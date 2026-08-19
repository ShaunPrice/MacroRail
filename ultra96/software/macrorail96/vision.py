"""Object detection on the Ultra96-V2, via the Vitis AI DPU or CPU.

Runs the MID loop of the robot architecture: camera frames in, detections
out, at 5-30 Hz. It never touches motors - it emits observations that
autonomy.py turns into *proposed* commands, which the fabric then clamps.

Honest positioning
------------------
A DPUCZDX8G in the ZU3EG's fabric gives roughly 0.5-1.2 TOPS depending on
the configuration you can fit (B1152 to B2304 on this device). A Jetson
Orin Nano Super is ~67 TOPS for similar money, runs stock PyTorch, and
exports to TensorRT in one line. If raw inference throughput is what you
need, buy the Jetson.

What the Ultra96 gives you instead is on the *same chip* as a
nanosecond-deterministic control and safety layer, at a few watts, with
the vision pipeline able to feed the control loop without crossing a USB
or UART boundary. Choose it for the integration and the determinism, not
for the TOPS.

The Vitis AI toolchain is also genuinely harder than TensorRT: models must
be quantised to INT8 and compiled to an .xmodel that matches your DPU's
arch.json fingerprint. Budget real time for that, and keep the CPU
fallback below working so the robot is testable meanwhile.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

from dataclasses import dataclass
from typing import List, Sequence, Tuple

import numpy as np

try:  # Vitis AI runtime, present only on a board with the DPU bitstream
    import vart
    import xir

    _HAVE_VART = True
except ImportError:  # pragma: no cover - depends on the board image
    _HAVE_VART = False


@dataclass
class Detection:
    """One detected object, in ORIGINAL image pixel coordinates."""

    x1: float
    y1: float
    x2: float
    y2: float
    score: float
    class_id: int

    @property
    def center(self) -> Tuple[float, float]:
        return ((self.x1 + self.x2) / 2.0, (self.y1 + self.y2) / 2.0)

    @property
    def area(self) -> float:
        return max(0.0, self.x2 - self.x1) * max(0.0, self.y2 - self.y1)


# ----------------------------------------------------------------------
# Geometry helpers
#
# These are where detection pipelines quietly go wrong: the model sees a
# letterboxed square, and every box it returns must be mapped back through
# that padding to the original frame. Get it subtly wrong and the robot
# steers at an object that is not where it thinks it is.
# ----------------------------------------------------------------------
def letterbox(image: np.ndarray, target: int) -> Tuple[np.ndarray, float, int, int]:
    """Resize preserving aspect ratio, pad to a square `target`.

    Returns (padded_image, scale, pad_x, pad_y) so detections can be
    mapped back with unletterbox_box().
    """
    import cv2

    h, w = image.shape[:2]
    if h == 0 or w == 0:
        raise ValueError("empty image")

    scale = min(target / w, target / h)
    new_w, new_h = int(round(w * scale)), int(round(h * scale))
    resized = cv2.resize(image, (new_w, new_h), interpolation=cv2.INTER_LINEAR)

    pad_x = (target - new_w) // 2
    pad_y = (target - new_h) // 2

    canvas = np.full((target, target, image.shape[2]), 114, dtype=image.dtype)
    canvas[pad_y:pad_y + new_h, pad_x:pad_x + new_w] = resized
    return canvas, scale, pad_x, pad_y


def unletterbox_box(box, scale: float, pad_x: int, pad_y: int,
                    orig_w: int, orig_h: int):
    """Map a box from letterboxed coordinates back to the original frame."""
    x1, y1, x2, y2 = box
    x1 = (x1 - pad_x) / scale
    y1 = (y1 - pad_y) / scale
    x2 = (x2 - pad_x) / scale
    y2 = (y2 - pad_y) / scale
    # Clip to the frame; a model can predict slightly outside it.
    x1 = min(max(x1, 0.0), orig_w)
    y1 = min(max(y1, 0.0), orig_h)
    x2 = min(max(x2, 0.0), orig_w)
    y2 = min(max(y2, 0.0), orig_h)
    return x1, y1, x2, y2


def iou(a: Sequence[float], b: Sequence[float]) -> float:
    """Intersection over union of two (x1, y1, x2, y2) boxes."""
    ix1, iy1 = max(a[0], b[0]), max(a[1], b[1])
    ix2, iy2 = min(a[2], b[2]), min(a[3], b[3])
    iw, ih = max(0.0, ix2 - ix1), max(0.0, iy2 - iy1)
    inter = iw * ih
    area_a = max(0.0, a[2] - a[0]) * max(0.0, a[3] - a[1])
    area_b = max(0.0, b[2] - b[0]) * max(0.0, b[3] - b[1])
    union = area_a + area_b - inter
    return inter / union if union > 0 else 0.0


def non_max_suppression(
    boxes: List[Sequence[float]],
    scores: List[float],
    class_ids: List[int],
    iou_threshold: float = 0.45,
) -> List[int]:
    """Greedy per-class NMS. Returns the indices to keep, best score first."""
    order = sorted(range(len(boxes)), key=lambda i: scores[i], reverse=True)
    keep: List[int] = []
    while order:
        best = order.pop(0)
        keep.append(best)
        order = [
            i
            for i in order
            if class_ids[i] != class_ids[best]
            or iou(boxes[best], boxes[i]) < iou_threshold
        ]
    return keep


# ----------------------------------------------------------------------
# Detector
# ----------------------------------------------------------------------
class Detector:
    """YOLO-style detector on the DPU when available, CPU otherwise.

    `model_path` is a compiled .xmodel for the DPU path, or an ONNX file
    for the OpenCV CPU fallback.
    """

    def __init__(
        self,
        model_path: str,
        input_size: int = 416,
        score_threshold: float = 0.35,
        iou_threshold: float = 0.45,
    ):
        self.model_path = model_path
        self.input_size = input_size
        self.score_threshold = score_threshold
        self.iou_threshold = iou_threshold
        self.backend = "none"
        self._runner = None
        self._net = None

        if _HAVE_VART and model_path.endswith(".xmodel"):
            self._init_dpu()
        else:
            self._init_cpu()

    # ------------------------------------------------------------------
    def _init_dpu(self) -> None:
        graph = xir.Graph.deserialize(self.model_path)
        subgraphs = [
            s
            for s in graph.get_root_subgraph().toposort_child_subgraph()
            if s.has_attr("device") and s.get_attr("device").upper() == "DPU"
        ]
        if not subgraphs:
            raise RuntimeError(
                f"{self.model_path} contains no DPU subgraph. It was "
                "probably compiled for a different DPU arch - recompile "
                "with the arch.json that matches your bitstream."
            )
        self._runner = vart.Runner.create_runner(subgraphs[0], "run")
        self.backend = "dpu"

    def _init_cpu(self) -> None:
        import cv2

        self._net = cv2.dnn.readNet(self.model_path)
        self.backend = "cpu"

    # ------------------------------------------------------------------
    def detect(self, frame: np.ndarray) -> List[Detection]:
        """Run one frame. Returns detections in original-frame pixels."""
        orig_h, orig_w = frame.shape[:2]
        padded, scale, pad_x, pad_y = letterbox(frame, self.input_size)

        if self.backend == "dpu":
            raw = self._infer_dpu(padded)
        else:
            raw = self._infer_cpu(padded)

        boxes, scores, class_ids = self._decode(raw)
        keep = non_max_suppression(
            boxes, scores, class_ids, self.iou_threshold
        )

        results = []
        for i in keep:
            x1, y1, x2, y2 = unletterbox_box(
                boxes[i], scale, pad_x, pad_y, orig_w, orig_h
            )
            results.append(
                Detection(x1, y1, x2, y2, scores[i], class_ids[i])
            )
        return results

    # ------------------------------------------------------------------
    def _infer_dpu(self, padded: np.ndarray) -> np.ndarray:
        in_tensors = self._runner.get_input_tensors()
        out_tensors = self._runner.get_output_tensors()

        # The DPU consumes INT8; the input fix-point scale comes from the
        # compiled model, not from a guess.
        in_fix = 2 ** in_tensors[0].get_attr("fix_point")
        data = (padded.astype(np.float32) / 255.0 * in_fix).astype(np.int8)
        input_data = [np.expand_dims(data, axis=0)]

        output_data = [
            np.empty(tuple(t.dims), dtype=np.int8, order="C")
            for t in out_tensors
        ]
        job = self._runner.execute_async(input_data, output_data)
        self._runner.wait(job)

        out_fix = 2 ** out_tensors[0].get_attr("fix_point")
        return output_data[0].astype(np.float32) / out_fix

    def _infer_cpu(self, padded: np.ndarray) -> np.ndarray:
        import cv2

        blob = cv2.dnn.blobFromImage(
            padded, 1 / 255.0, (self.input_size, self.input_size),
            swapRB=True, crop=False,
        )
        self._net.setInput(blob)
        return self._net.forward()

    # ------------------------------------------------------------------
    def _decode(self, raw: np.ndarray):
        """Decode a YOLO head of shape (1, N, 5 + num_classes).

        Boxes arrive as centre-x, centre-y, width, height in letterboxed
        pixels; converted here to corner form.
        """
        preds = np.squeeze(raw)
        if preds.ndim == 1:
            preds = preds[None, :]

        boxes, scores, class_ids = [], [], []
        for row in preds:
            objectness = float(row[4])
            if objectness < self.score_threshold:
                continue
            class_scores = row[5:]
            if class_scores.size == 0:
                continue
            class_id = int(np.argmax(class_scores))
            score = objectness * float(class_scores[class_id])
            if score < self.score_threshold:
                continue
            cx, cy, w, h = (float(v) for v in row[:4])
            boxes.append((cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2))
            scores.append(score)
            class_ids.append(class_id)
        return boxes, scores, class_ids

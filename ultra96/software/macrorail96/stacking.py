"""Focus stacking on the Ultra96-V2's quad Cortex-A53s.

Aligns the captured slices (ECC image alignment) and merges them with a
Laplacian-pyramid maximum-sharpness blend, producing the finished stack
on the board right after the shoot - no PC round trip.

Usage:
    python3 -m macrorail96.stacking shoot_dir/ -o stacked.jpg

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import argparse
import glob
import os

import cv2
import numpy as np

PYRAMID_LEVELS = 6


def _align(reference_gray: np.ndarray, image: np.ndarray) -> np.ndarray:
    """Align image to the reference using an affine ECC fit on a
    downscaled copy (slices from a rail only shift/scale slightly)."""
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    scale = 0.25
    ref_small = cv2.resize(reference_gray, None, fx=scale, fy=scale)
    img_small = cv2.resize(gray, None, fx=scale, fy=scale)
    warp = np.eye(2, 3, dtype=np.float32)
    try:
        _, warp = cv2.findTransformECC(
            ref_small,
            img_small,
            warp,
            cv2.MOTION_AFFINE,
            criteria=(cv2.TERM_CRITERIA_EPS | cv2.TERM_CRITERIA_COUNT, 100, 1e-5),
        )
        warp[:, 2] /= scale  # rescale translation to full resolution
    except cv2.error:
        return image  # alignment failed; use the frame as-is
    return cv2.warpAffine(
        image,
        warp,
        (image.shape[1], image.shape[0]),
        flags=cv2.INTER_LINEAR | cv2.WARP_INVERSE_MAP,
        borderMode=cv2.BORDER_REPLICATE,
    )


def _laplacian_pyramid(image: np.ndarray, levels: int):
    gaussian = [image.astype(np.float32)]
    for _ in range(levels):
        gaussian.append(cv2.pyrDown(gaussian[-1]))
    pyramid = []
    for i in range(levels):
        up = cv2.pyrUp(gaussian[i + 1], dstsize=gaussian[i].shape[1::-1])
        pyramid.append(gaussian[i] - up)
    pyramid.append(gaussian[levels])
    return pyramid


def stack_images(paths, align: bool = True, progress=None) -> np.ndarray:
    """Merge focus slices into one sharp image."""
    if len(paths) < 2:
        raise ValueError("need at least two images to stack")

    images = []
    reference_gray = None
    for i, path in enumerate(paths):
        img = cv2.imread(path, cv2.IMREAD_COLOR)
        if img is None:
            raise IOError(f"cannot read {path}")
        if align:
            if reference_gray is None:
                reference_gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
            else:
                img = _align(reference_gray, img)
        images.append(img)
        if progress:
            progress(f"aligned {i + 1}/{len(paths)}")

    # Per-image sharpness from the absolute Laplacian, lightly blurred so
    # the winner-take-all mask doesn't produce speckle.
    merged_pyramid = None
    best_sharpness = None
    best_index = None
    sharpness_maps = []
    for img in images:
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        sharp = np.abs(cv2.Laplacian(gray, cv2.CV_32F, ksize=3))
        sharp = cv2.GaussianBlur(sharp, (0, 0), 3)
        sharpness_maps.append(sharp)

    stack = np.stack(sharpness_maps)
    winner = np.argmax(stack, axis=0)

    # Blend pyramids: at each level, take coefficients from the image
    # whose (downscaled) sharpness wins at that location.
    pyramids = [_laplacian_pyramid(img, PYRAMID_LEVELS) for img in images]
    result_pyramid = []
    for level in range(PYRAMID_LEVELS + 1):
        shape = pyramids[0][level].shape[1::-1]
        winner_level = cv2.resize(
            winner.astype(np.float32), shape, interpolation=cv2.INTER_NEAREST
        ).astype(np.int32)
        level_result = np.zeros_like(pyramids[0][level])
        for idx in range(len(images)):
            mask = (winner_level == idx)[..., None]
            level_result += pyramids[idx][level] * mask
        result_pyramid.append(level_result)
        if progress:
            progress(f"blended level {level + 1}/{PYRAMID_LEVELS + 1}")

    # Collapse the pyramid.
    out = result_pyramid[-1]
    for level in range(PYRAMID_LEVELS - 1, -1, -1):
        out = cv2.pyrUp(out, dstsize=result_pyramid[level].shape[1::-1])
        out += result_pyramid[level]
    return np.clip(out, 0, 255).astype(np.uint8)


def main() -> None:
    parser = argparse.ArgumentParser(description="MacroRail focus stacker")
    parser.add_argument("shoot_dir", help="directory of focus slices (jpg/png/tif)")
    parser.add_argument("-o", "--output", default="stacked.jpg")
    parser.add_argument("--no-align", action="store_true")
    args = parser.parse_args()

    paths = sorted(
        p
        for ext in ("*.jpg", "*.jpeg", "*.png", "*.tif", "*.tiff", "*.JPG")
        for p in glob.glob(os.path.join(args.shoot_dir, ext))
    )
    print(f"stacking {len(paths)} slices from {args.shoot_dir}")
    result = stack_images(paths, align=not args.no_align, progress=print)
    cv2.imwrite(args.output, result)
    print(f"wrote {args.output}")


if __name__ == "__main__":
    main()

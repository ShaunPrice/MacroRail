package com.shaunprice.nswcommute;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.content.res.Configuration;
import android.graphics.Color;
import android.graphics.Insets;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.view.WindowInsets;
import android.view.WindowInsetsController;
import android.webkit.JavascriptInterface;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;

import org.json.JSONObject;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.Collections;
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;
import java.util.Set;
import java.util.HashSet;
import java.util.Arrays;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/**
 * Hosts the NSW Commute web app in a WebView.
 *
 * The app's files are served from the APK at https://appassets.androidplatform.net/
 * (a secure origin, so ES modules and localStorage work). Network requests to the
 * transport and routing APIs go through {@link NativeHttp}, which is restricted to
 * an allow-list of HTTPS hosts and is not subject to browser CORS rules.
 */
public class MainActivity extends Activity {

    private static final String APP_HOST = "appassets.androidplatform.net";
    private static final String APP_URL = "https://" + APP_HOST + "/index.html";

    private static final Set<String> ALLOWED_API_HOSTS = Collections.unmodifiableSet(new HashSet<>(Arrays.asList(
            "api.transport.nsw.gov.au",
            "router.project-osrm.org",
            "routes.googleapis.com")));

    private static final Map<String, String> MIME_TYPES = new HashMap<>();
    static {
        MIME_TYPES.put("html", "text/html");
        MIME_TYPES.put("js", "text/javascript");
        MIME_TYPES.put("mjs", "text/javascript");
        MIME_TYPES.put("css", "text/css");
        MIME_TYPES.put("json", "application/json");
        MIME_TYPES.put("svg", "image/svg+xml");
        MIME_TYPES.put("png", "image/png");
        MIME_TYPES.put("ico", "image/x-icon");
        MIME_TYPES.put("woff2", "font/woff2");
    }

    private final ExecutorService network = Executors.newFixedThreadPool(4);
    private FrameLayout root;
    private WebView webView;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        root = new FrameLayout(this);
        webView = new WebView(this);
        root.addView(webView, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT));
        setContentView(root);
        applySystemBars(isSystemDark());
        setUpInsets();

        boolean debuggable = (getApplicationInfo().flags & ApplicationInfo.FLAG_DEBUGGABLE) != 0;
        WebView.setWebContentsDebuggingEnabled(debuggable);

        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setAllowFileAccess(false);
        settings.setAllowContentAccess(false);
        settings.setSupportZoom(false);
        settings.setTextZoom(100);

        webView.setWebViewClient(new AppClient());
        webView.addJavascriptInterface(new NativeHttp(), "NativeHttp");
        webView.addJavascriptInterface(new NativeApp(), "NativeApp");

        if (savedInstanceState == null || webView.restoreState(savedInstanceState) == null) {
            webView.loadUrl(APP_URL);
        }
    }

    @Override
    protected void onSaveInstanceState(Bundle outState) {
        super.onSaveInstanceState(outState);
        webView.saveState(outState);
    }

    @Override
    protected void onResume() {
        super.onResume();
        webView.onResume();
    }

    @Override
    protected void onPause() {
        webView.onPause();
        super.onPause();
    }

    @Override
    protected void onDestroy() {
        network.shutdownNow();
        webView.destroy();
        super.onDestroy();
    }

    @Override
    @SuppressWarnings("deprecation")
    public void onBackPressed() {
        // Let the page close an open dialog first; otherwise leave the app.
        webView.evaluateJavascript("window.__onBack ? window.__onBack() : false", result -> {
            if (!"true".equals(result)) {
                super.onBackPressed();
            }
        });
    }

    private boolean isSystemDark() {
        int mode = getResources().getConfiguration().uiMode & Configuration.UI_MODE_NIGHT_MASK;
        return mode == Configuration.UI_MODE_NIGHT_YES;
    }

    /** Colours the area behind the status and navigation bars to match the page theme. */
    private void applySystemBars(boolean dark) {
        root.setBackgroundColor(dark ? Color.parseColor("#161B22") : Color.WHITE);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            WindowInsetsController controller = getWindow().getInsetsController();
            if (controller != null) {
                int light = WindowInsetsController.APPEARANCE_LIGHT_STATUS_BARS
                        | WindowInsetsController.APPEARANCE_LIGHT_NAVIGATION_BARS;
                controller.setSystemBarsAppearance(dark ? 0 : light, light);
            }
        }
    }

    /** Draws edge to edge and pads the WebView so content clears the system bars and keyboard. */
    private void setUpInsets() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.R) {
            root.setFitsSystemWindows(true);
            return;
        }
        getWindow().setDecorFitsSystemWindows(false);
        root.setOnApplyWindowInsetsListener((View v, WindowInsets insets) -> {
            Insets bars = insets.getInsets(WindowInsets.Type.systemBars() | WindowInsets.Type.displayCutout());
            Insets ime = insets.getInsets(WindowInsets.Type.ime());
            v.setPadding(bars.left, bars.top, bars.right, Math.max(bars.bottom, ime.bottom));
            return WindowInsets.CONSUMED;
        });
    }

    private final class AppClient extends WebViewClient {
        @Override
        public WebResourceResponse shouldInterceptRequest(WebView view, WebResourceRequest request) {
            Uri uri = request.getUrl();
            if (!"https".equals(uri.getScheme()) || !APP_HOST.equals(uri.getHost())) {
                return null;
            }
            String path = uri.getPath() == null || "/".equals(uri.getPath()) ? "/index.html" : uri.getPath();
            if (path.contains("..")) {
                return notFound();
            }
            try {
                InputStream in = getAssets().open("www" + path);
                String ext = path.substring(path.lastIndexOf('.') + 1).toLowerCase();
                String mime = MIME_TYPES.containsKey(ext) ? MIME_TYPES.get(ext) : "application/octet-stream";
                WebResourceResponse response = new WebResourceResponse(mime, "utf-8", in);
                Map<String, String> headers = new HashMap<>();
                headers.put("Cache-Control", "no-cache");
                response.setResponseHeaders(headers);
                return response;
            } catch (IOException e) {
                return notFound();
            }
        }

        private WebResourceResponse notFound() {
            WebResourceResponse r = new WebResourceResponse("text/plain", "utf-8",
                    new java.io.ByteArrayInputStream(new byte[0]));
            r.setStatusCodeAndReasonPhrase(404, "Not Found");
            return r;
        }

        @Override
        public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
            Uri uri = request.getUrl();
            if (APP_HOST.equals(uri.getHost())) {
                return false;
            }
            // External links open in the browser, never inside the app.
            try {
                startActivity(new Intent(Intent.ACTION_VIEW, uri));
            } catch (ActivityNotFoundException ignored) {
                // No browser available.
            }
            return true;
        }
    }

    /** Small helpers for the page: keeps the system bars in step with the in-app theme toggle. */
    public final class NativeApp {
        @JavascriptInterface
        public void setTheme(boolean dark) {
            runOnUiThread(() -> applySystemBars(dark));
        }
    }

    /** HTTPS requests on behalf of the page, limited to the transport and routing APIs. */
    public final class NativeHttp {
        @JavascriptInterface
        public void request(String id, String method, String url, String headersJson, String body) {
            network.execute(() -> {
                try {
                    URL target = new URL(url);
                    if (!"https".equals(target.getProtocol()) || !ALLOWED_API_HOSTS.contains(target.getHost())) {
                        reject(id, "Host not allowed: " + target.getHost());
                        return;
                    }
                    HttpURLConnection conn = (HttpURLConnection) target.openConnection();
                    conn.setConnectTimeout(20000);
                    conn.setReadTimeout(30000);
                    conn.setRequestMethod(method == null || method.isEmpty() ? "GET" : method.toUpperCase());
                    conn.setRequestProperty("User-Agent", "NSWCommute-Android/1.0");
                    if (headersJson != null && !headersJson.isEmpty()) {
                        JSONObject headers = new JSONObject(headersJson);
                        Iterator<String> keys = headers.keys();
                        while (keys.hasNext()) {
                            String key = keys.next();
                            conn.setRequestProperty(key, headers.getString(key));
                        }
                    }
                    if (body != null && !body.isEmpty() && !"GET".equals(conn.getRequestMethod())) {
                        conn.setDoOutput(true);
                        try (OutputStream out = conn.getOutputStream()) {
                            out.write(body.getBytes(StandardCharsets.UTF_8));
                        }
                    }
                    int status = conn.getResponseCode();
                    InputStream in = status >= 400 ? conn.getErrorStream() : conn.getInputStream();
                    String text = in == null ? "" : readAll(in);
                    String type = conn.getContentType();
                    conn.disconnect();
                    resolve(id, status, text, type == null ? "application/json" : type);
                } catch (Exception e) {
                    String message = e.getMessage() == null ? e.getClass().getSimpleName() : e.getMessage();
                    reject(id, "Network request failed: " + message);
                }
            });
        }
    }

    private static String readAll(InputStream in) throws IOException {
        try (InputStream stream = in; ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[16384];
            int n;
            while ((n = stream.read(buffer)) != -1) {
                out.write(buffer, 0, n);
            }
            return out.toString("UTF-8");
        }
    }

    private void resolve(String id, int status, String body, String contentType) {
        String js = "window.__nativeHttp && window.__nativeHttp.resolve(" + JSONObject.quote(id) + "," + status + ","
                + JSONObject.quote(body) + "," + JSONObject.quote(contentType) + ")";
        runOnUiThread(() -> webView.evaluateJavascript(js, null));
    }

    private void reject(String id, String message) {
        String js = "window.__nativeHttp && window.__nativeHttp.reject(" + JSONObject.quote(id) + ","
                + JSONObject.quote(message) + ")";
        runOnUiThread(() -> webView.evaluateJavascript(js, null));
    }
}

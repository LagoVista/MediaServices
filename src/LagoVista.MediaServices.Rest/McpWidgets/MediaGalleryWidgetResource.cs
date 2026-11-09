using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

namespace LagoVista.MediaServices.Rest.McpWidgets
{
    /// <summary>
    /// MediaServices owns its MCP App HTML and CSP declaration.
    /// The shared API host discovers this resource from the feature assembly.
    /// First-cut proof: native ChatGPT widget shell + external image rendering.
    /// </summary>
    [McpServerResourceType]
    public static class MediaGalleryWidgetResource
    {
        public const string ResourceUri = "ui://media/gallery/v4.html";
        public const string AppMimeType = "text/html;profile=mcp-app";

        [McpServerResource(UriTemplate = ResourceUri, Name = "APTIX Media Preview", MimeType = AppMimeType)]
        public static TextResourceContents ReadMediaGallery()
        {
            return new TextResourceContents
            {
                Uri = ResourceUri,
                MimeType = AppMimeType,
                Text = Html,
                Meta = new JsonObject
                {
                    ["ui"] = new JsonObject
                    {
                        ["prefersBorder"] = true,
                        ["csp"] = new JsonObject
                        {
                            ["resourceDomains"] = new JsonArray(
                                "https://apptrex.ai",
                                "https://storage-dev.nuviot.com",
                                "https://storage.nuviot.com",
                                "https://api-dev.nuviot.com",
                                "https://api.nuviot.com"),
                            ["connectDomains"] = new JsonArray()
                        }
                    }
                }
            };
        }

        // Reuse the verified v3 MCP Apps handshake, then display a short-lived
        // immutable revision URL supplied by the gallery preview tool.
        // The browser fetches pixels directly from the signed storage URL.
        public const string Html = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>APTIX Media Preview</title>
              <style>
                :root {color-scheme:light dark;font-family:system-ui,-apple-system,Segoe UI,sans-serif}
                body {margin:0;padding:20px;background:transparent}
                .card {border:1px solid rgba(128,128,128,.3);border-radius:12px;padding:20px}
                .label {font-size:11px;opacity:.7;text-transform:uppercase;letter-spacing:.07em}
                h2 {font-size:17px;margin:8px 0}
                #state {font-size:14px;margin:0}
                #details {font-size:12px;opacity:.7;margin:12px 0 0}
                .frame {display:flex;align-items:center;justify-content:center;min-height:180px;margin-top:14px;overflow:hidden;border:1px solid rgba(128,128,128,.25);border-radius:10px;background:rgba(128,128,128,.06)}
                img {display:block;width:auto;max-width:100%;max-height:520px;object-fit:contain}
                [hidden] {display:none!important}
                #imageMeta {font-size:12px;opacity:.76;overflow-wrap:anywhere;margin:12px 0 0}
              </style>
            </head>
            <body>
              <main class="card">
                <div class="label">APTIX / MediaServices / DEV / v4</div>
                <h2>Media Resource preview</h2>
                <p id="state">Widget HTML loaded. Waiting for JavaScript...</p>
                <p id="details">Initializing the MCP Apps bridge...</p>
                <div class="frame"><img id="preview" alt="Selected Media Resource" hidden></div>
                <p id="imageMeta">No image bytes have been sent to the model.</p>
              </main>
              <script>
                (() => {
                  const state = document.getElementById('state');
                  const details = document.getElementById('details');
                  const image = document.getElementById('preview');
                  const imageMeta = document.getElementById('imageMeta');
                  let initialized = false;
                  let lease = null;

                  // MCP Apps sends tool results through ui/notifications/tool-result.
                  // Handle both structured results and MCP text content containing JSON.
                  const findLease = input => {
                    const queue = [input];
                    let steps = 0;
                    while (queue.length && steps++ < 30) {
                      let value = queue.shift();
                      if (!value) continue;
                      if (typeof value === 'string') {
                        try { queue.push(JSON.parse(value)); } catch (_) {}
                      } else if (Array.isArray(value)) {
                        queue.push(...value);
                      } else if (typeof value === 'object') {
                        if (typeof value.url === 'string') return value;
                        for (const key of ['structuredContent', 'result', 'model', 'content', 'text']) {
                          if (value[key] != null) queue.push(value[key]);
                        }
                      }
                    }
                    return null;
                  };
                  const tryPreview = () => {
                    if (!initialized) return;
                    if (!lease) {
                      state.textContent = 'Connected. Waiting for MediaServices preview URL...';
                      return;
                    }
                    let target;
                    try { target = new URL(lease.url); } catch (_) {
                      state.textContent = 'Invalid image URL returned by MediaServices.';
                      return;
                    }
                    const permitted = ['storage-dev.nuviot.com', 'storage.nuviot.com', 'apptrex.ai', 'api-dev.nuviot.com', 'api.nuviot.com'];
                    if (target.protocol !== 'https:' || !permitted.includes(target.hostname)) {
                      state.textContent = 'MediaServices returned an unapproved image host.';
                      return;
                    }
                    state.textContent = 'Loading image directly from MediaServices storage...';
                    details.textContent = 'Source: ' + target.hostname + ' (signed HTTPS URL)';
                    imageMeta.textContent = (lease.fileName || 'Image') + ' · loading...';
                    image.hidden = false;
                    image.src = target.href;
                  };
                  image.addEventListener('load', () => {
                    state.textContent = 'Image loaded successfully';
                    imageMeta.textContent = (lease?.fileName || 'Media Resource') + ' · ' +
                      image.naturalWidth + ' × ' + image.naturalHeight +
                      ' · browser-rendered, not sent to model';
                  });
                  image.addEventListener('error', () => {
                    state.textContent = 'Image failed to load';
                    imageMeta.textContent = 'Inspect the Network and Console panels for CSP or HTTP errors.';
                  });
                  const receiveToolResult = value => {
                    const newLease = findLease(value);
                    if (!newLease) {
                      if (initialized && !lease) details.textContent = 'Tool result received, but no image URL was found.';
                      return;
                    }
                    lease = newLease;
                    tryPreview();
                  };

                  state.textContent = 'JavaScript running. Initializing host connection...';
                  window.addEventListener('message', event => {
                    if (event.source !== window.parent || !event.data || event.data.jsonrpc !== '2.0') return;
                    if (event.data.method === 'ui/notifications/tool-result') {
                      receiveToolResult(event.data.params);
                      return;
                    }
                    if (event.data.id !== 1) return;
                    if (event.data.error) {
                      state.textContent = 'Host rejected initialization';
                      details.textContent = String(event.data.error.message || 'Unspecified host error');
                      return;
                    }
                    if (event.data.result) {
                      const negotiated = event.data.result.protocolVersion || 'not specified';
                      window.parent.postMessage({
                        jsonrpc: '2.0',
                        method: 'ui/notifications/initialized',
                        params: {}
                      }, '*');
                      initialized = true;
                      state.textContent = 'MCP Apps handshake completed successfully';
                      details.textContent = 'Negotiated protocol: ' + negotiated;
                      if (window.openai && window.openai.toolOutput) receiveToolResult(window.openai.toolOutput);
                      tryPreview();
                    }
                  });
                  if (window.openai && window.openai.toolOutput) receiveToolResult(window.openai.toolOutput);
                  window.parent.postMessage({
                    jsonrpc: '2.0',
                    id: 1,
                    method: 'ui/initialize',
                    params: {
                      protocolVersion: '2026-01-26',
                      appInfo: { name: 'aptix-media-gallery', version: '4.0.0' },
                      appCapabilities: {}
                    }
                  }, '*');
                })();
              </script>
            </body>
            </html>
            """;
    }
}

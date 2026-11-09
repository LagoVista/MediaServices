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
        public const string ResourceUri = "ui://media/gallery/v3.html";
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
                                "https://api-dev.nuviot.com",
                                "https://api.nuviot.com"),
                            ["connectDomains"] = new JsonArray()
                        }
                    }
                }
            };
        }

        // Startup-only smoke test. No media downloads or image dependencies:
        // validate the MCP Apps iframe handshake independently.
        public const string Html = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>APTIX MCP Apps startup test</title>
              <style>
                :root {color-scheme:light dark;font-family:system-ui,-apple-system,Segoe UI,sans-serif}
                body {margin:0;padding:20px;background:transparent}
                .card {border:1px solid rgba(128,128,128,.3);border-radius:12px;padding:20px}
                .label {font-size:11px;opacity:.7;text-transform:uppercase;letter-spacing:.07em}
                h2 {font-size:17px;margin:8px 0}
                #state {font-size:14px;margin:0}
                #details {font-size:12px;opacity:.7;margin:12px 0 0}
              </style>
            </head>
            <body>
              <main class="card">
                <div class="label">APTIX / MediaServices / DEV / v3</div>
                <h2>MCP Apps connection test</h2>
                <p id="state">Widget HTML loaded. Waiting for JavaScript...</p>
                <p id="details">No image or network request is needed for this test.</p>
              </main>
              <script>
                (() => {
                  const state = document.getElementById('state');
                  const details = document.getElementById('details');
                  state.textContent = 'JavaScript running. Initializing host connection...';
                  window.addEventListener('message', event => {
                    if (event.source !== window.parent || !event.data ||
                        event.data.jsonrpc !== '2.0' || event.data.id !== 1) return;
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
                      state.textContent = 'MCP Apps handshake completed successfully';
                      details.textContent = 'Negotiated protocol: ' + negotiated;
                    }
                  });
                  window.parent.postMessage({
                    jsonrpc: '2.0',
                    id: 1,
                    method: 'ui/initialize',
                    params: {
                      protocolVersion: '2026-01-26',
                      appInfo: { name: 'aptix-media-gallery', version: '3.0.0' },
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

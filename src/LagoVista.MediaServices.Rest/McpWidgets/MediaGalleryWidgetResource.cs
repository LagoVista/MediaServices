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
        public const string ResourceUri = "ui://media/gallery/v2.html";
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

        // No external JS assets or runtime app service required.
        // The UI is sandboxed inside ChatGPT; it never receives API credentials.
        public const string Html = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>APTIX Media Preview</title>
              <style>
                :root { color-scheme: light dark; font-family: system-ui, -apple-system, Segoe UI, sans-serif; }
                * { box-sizing: border-box; }
                body { margin: 0; padding: 14px; background: transparent; color: inherit; }
                .shell { max-width: 800px; margin: 0 auto; }
                .header { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 12px; }
                .title { font-weight: 700; font-size: 15px; letter-spacing: .01em; }
                .eyebrow { font-size: 11px; opacity: .6; letter-spacing: .06em; text-transform: uppercase; }
                .status { font-size: 12px; opacity: .8; margin: 8px 0; word-break: break-word; }
                .frame { overflow: hidden; background: rgba(128,128,128,.07); border: 1px solid rgba(128,128,128,.22); border-radius: 12px; min-height: 170px; display: flex; align-items: center; justify-content: center; }
                img { display: block; width: 100%; height: auto; max-height: 490px; object-fit: contain; }
                .footer { display: flex; gap: 10px; align-items: center; justify-content: space-between; margin-top: 10px; flex-wrap: wrap; font-size: 11px; opacity: .72; }
                #filename { font-weight: 600; }
                .good { color: #159669; opacity: 1; }
                .bad { color: #c34e42; opacity: 1; }
              </style>
            </head>
            <body>
              <main class="shell">
                <header class="header">
                  <div><div class="eyebrow">MCP Apps / MediaServices</div><div class="title">APTIX Media Preview</div></div>
                  <div class="eyebrow">DEV · v2</div>
                </header>
                <div class="frame">
                  <img id="preview" alt="Selected Media Resource preview" hidden>
                </div>
                <div id="status" class="status">Connecting to ChatGPT MCP Apps host…</div>
                <footer class="footer">
                  <span id="filename">No image selected</span>
                  <span id="dimensions">Image bytes stay in your browser</span>
                </footer>
              </main>
              <script>
                (() => {
                  const img = document.getElementById('preview');
                  const status = document.getElementById('status');
                  const filename = document.getElementById('filename');
                  const dimensions = document.getElementById('dimensions');
                  let loaded = false;
                  const setStatus = (msg, ok) => {
                    status.textContent = msg;
                    status.className = 'status ' + (ok === true ? 'good' : ok === false ? 'bad' : '');
                  };
                  img.addEventListener('load', () => {
                    img.hidden = false;
                    loaded = true;
                    dimensions.textContent = img.naturalWidth + ' × ' + img.naturalHeight + ' · direct browser image';
                    setStatus('Image loaded. The widget CSP permits this image host.', true);
                  });
                  img.addEventListener('error', () => {
                    setStatus('Image could not be loaded. Check the browser Network and Console tabs.', false);
                  });
                  const parseResult = (input) => {
                    if (!input) return null;
                    let result = input.structuredContent || input;
                    if (typeof result === 'string') { try { result = JSON.parse(result); } catch (_) { return null; } }
                    if (!result || !result.result) {
                      const block = input.content && input.content.find(item => item.type === 'text');
                      if (block) { try { result = JSON.parse(block.text); } catch (_) {} }
                    }
                    const lease = result && (result.result || result.model || result);
                    return lease && (lease.url || lease.Url) ? lease : null;
                  };
                  const receive = (input) => {
                    const lease = parseResult(input);
                    if (!lease) return;
                    const url = lease.url || lease.Url;
                    try {
                      const target = new URL(url);
                      if (target.protocol !== 'https:') return;
                      filename.textContent = lease.fileName || lease.FileName || 'Media Resource';
                      loaded = false;
                      img.hidden = false;
                      setStatus('Loading signed revision directly in your browser…');
                      img.src = target.href;
                    } catch (_) { setStatus('Invalid preview URL.', false); }
                  };
                  window.addEventListener('message', (event) => {
                    if (event.source !== window.parent || !event.data) return;
                    const data = event.data;
                    if (data.method === 'ui/notifications/tool-result') receive(data.params);
                    if (data.id === 1 && data.result) {
                      setStatus('Connected. Waiting for MediaServices revision URL…');
                      window.parent.postMessage({ jsonrpc: '2.0', method: 'ui/notifications/initialized' }, '*');
                    } else if (data.id === 1 && data.error) {
                      setStatus('MCP Apps initialization rejected: ' + (data.error.message || 'unknown error'), false);
                    }
                  });
                  if (window.openai && window.openai.toolOutput) receive(window.openai.toolOutput);
                  window.parent.postMessage({
                    jsonrpc: '2.0', id: 1, method: 'ui/initialize',
                    params: { protocolVersion: '2026-01-26',
                      capabilities: {},
                      clientInfo: { name: 'aptix-media-preview', version: '1.0.1' },
                      appCapabilities: {} }
                  }, '*');
                })();
              </script>
            </body>
            </html>
            """;
    }
}

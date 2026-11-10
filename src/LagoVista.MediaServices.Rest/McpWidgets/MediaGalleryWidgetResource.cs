using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

namespace LagoVista.MediaServices.Rest.McpWidgets
{
    /// <summary>
    /// MediaServices owns its MCP App HTML and CSP declaration.
    /// The shared API host discovers this resource from the feature assembly.
    /// Exact-revision image preview for manual clipboard-to-composer editing.
    /// Resource identity travels in widget model context; image pixels do not.
    /// </summary>
    [McpServerResourceType]
    public static class MediaGalleryWidgetResource
    {
        public const string ResourceUri = "ui://media/gallery/v6.html";
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
                            ["connectDomains"] = new JsonArray(
                                "https://storage-dev.nuviot.com",
                                "https://storage.nuviot.com")
                        }
                    }
                }
            };
        }

        // Render the exact original image for native browser Copy image -> paste
        // into the ChatGPT composer. Widget state retains resource/revision IDs
        // without attempting image upload or sending a model follow-up.
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
                .instructions {margin-top:16px;border:1px solid rgba(128,128,128,.3);border-radius:10px;padding:14px 16px}
                .instructions h3 {font-size:14px;margin:0 0 8px}
                .instructions ol {margin:0 0 10px;padding-left:22px;font-size:13px;line-height:1.7}
                .instructions p {font-size:12px;margin:7px 0 0;overflow-wrap:anywhere}
                .instructions code {font-size:11px;overflow-wrap:anywhere}
                a {color:#0969b6;text-decoration:underline}
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
                <div class="label">APTIX / MediaServices / exact-revision preview / v6</div>
                <h2>Media Resource preview</h2>
                <p id="state">Widget HTML loaded. Waiting for JavaScript...</p>
                <p id="details">Initializing the MCP Apps bridge...</p>
                <div class="frame"><img id="preview" alt="Selected Media Resource" hidden></div>
                <p id="imageMeta">The exact revision is displayed here; copy its image pixels into your ChatGPT message.</p>
                <section class="instructions" aria-label="How to edit this image">
                  <h3>To make changes</h3>
                  <ol>
                    <li>Right-click the image above and choose <strong>Copy image</strong> (not Copy image address).</li>
                    <li>Click the ChatGPT message box and paste the image (Ctrl+V on Windows or Cmd+V on Mac).</li>
                    <li>Describe what you want changed and send your message.</li>
                    <li>Review the result ChatGPT shows you.</li>
                    <li>Ask for further changes until you're happy.</li>
                    <li>Ask ChatGPT to <strong>save the approved image as a new revision</strong> of this resource.</li>
                  </ol>
                  <p>Media Resource ID: <code id="resourceId">Loading...</code></p>
                  <p>Source revision: <code id="revisionId">Loading...</code></p>
                  <p><a id="openOriginal" hidden target="_blank" rel="noopener noreferrer">Open full-size original</a> if the browser does not offer Copy image here.</p>
                  <p>No changes are saved until you explicitly ask ChatGPT to save.</p>
                </section>
              </main>
              <script>
                (() => {
                  const state = document.getElementById('state');
                  const details = document.getElementById('details');
                  const image = document.getElementById('preview');
                  const imageMeta = document.getElementById('imageMeta');
                  const resourceId = document.getElementById('resourceId');
                  const revisionId = document.getElementById('revisionId');
                  const openOriginal = document.getElementById('openOriginal');
                  let initialized = false;
                  let lease = null;
                  let lastContextKey = null;
                  const permittedHosts = new Set(['storage-dev.nuviot.com',
                    'storage.nuviot.com', 'apptrex.ai', 'api-dev.nuviot.com', 'api.nuviot.com']);

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
                    if (target.protocol !== 'https:' || !permittedHosts.has(target.hostname)) {
                      state.textContent = 'MediaServices returned an unapproved image host.';
                      return;
                    }
                    resourceId.textContent = lease.mediaResourceId || 'Unknown';
                    revisionId.textContent = lease.revisionId || 'Unknown';
                    openOriginal.href = target.href;
                    openOriginal.hidden = false;
                    const key = (lease.mediaResourceId || '') + ':' + (lease.revisionId || '');
                    if (key !== lastContextKey && lease.mediaResourceId && lease.revisionId) {
                      lastContextKey = key;
                      if (window.openai?.setWidgetState) {
                        Promise.resolve(window.openai.setWidgetState({
                          modelContent:{
                            action:'aptix_media_image_selected_for_manual_edit',
                            mediaResourceId:lease.mediaResourceId,
                            revisionId:lease.revisionId,
                            sourceTransport:'user-copies-full-resolution-image-and-pastes-into-chat-composer',
                            instruction:'The user is copying the ACTUAL image pixels from this exact Media Resource revision and pasting them into the chat composer to request edits. Use the image attached to the user message as the editing source; do not regenerate from text alone. When the user asks to save, use media-revise-chatgpt-image on THIS mediaResourceId after approval. A widget preview alone is not a source-image attachment. Do not save until explicitly requested.'
                          },
                          privateContent:{
                            selectedResourceId:lease.mediaResourceId,
                            selectedRevisionId:lease.revisionId
                          }
                        })).catch(() => {
                          // Identifiers remain visible for manual save instructions if
                          // the host does not retain widget state.
                        });
                      }
                    }
                    state.textContent = 'Loading exact image revision directly from MediaServices storage...';
                    details.textContent = 'Source: ' + target.hostname + ' (signed HTTPS URL)';
                    imageMeta.textContent = (lease.fileName || 'Image') + ' · loading...';
                    image.hidden = false;
                    image.src = target.href;
                  };
                  image.addEventListener('load', () => {
                    state.textContent = 'Exact image revision loaded successfully';
                    imageMeta.textContent = (lease?.fileName || 'Media Resource') + ' · ' +
                      image.naturalWidth + ' × ' + image.naturalHeight +
                      ' · full-resolution original, ready for browser Copy image';
                  });
                  image.addEventListener('error', () => {
                    state.textContent = 'Image failed to load';
                    imageMeta.textContent = 'Open full-size original above, or inspect the browser Network panel.';
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
                      appInfo: { name: 'aptix-media-gallery', version: '6.0.0' },
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

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

namespace LagoVista.MediaServices.Rest.McpWidgets
{
    /// <summary>
    /// MediaServices owns its MCP App HTML and CSP declaration.
    /// The shared API host discovers this resource from the feature assembly.
    /// Exact-revision preview and native ChatGPT source-image handoff. The model\n    /// does not retrieve image bytes via an MCP download tool.
    /// </summary>
    [McpServerResourceType]
    public static class MediaGalleryWidgetResource
    {
        public const string ResourceUri = "ui://media/gallery/v5.html";
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

        // Reuse the verified MCP Apps handshake to display the exact immutable\n        // revision; transfer source pixels via window.openai.uploadFile only\n        // after a user explicitly clicks the native handoff button.
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
                button {font:inherit}
                .actions {display:flex;flex-wrap:wrap;gap:12px;align-items:center;margin-top:15px}
                #handoff {border:1px solid #0969b6;border-radius:9px;padding:9px 14px;color:#fff;background:#0969b6;font-weight:600;cursor:pointer}
                #handoff:disabled {opacity:.5;cursor:default}
                #handoffStatus {font-size:12px;opacity:.8;overflow-wrap:anywhere;flex:1;min-width:220px}
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
                <div class="label">APTIX / MediaServices / exact-revision preview / v5</div>
                <h2>Media Resource preview</h2>
                <p id="state">Widget HTML loaded. Waiting for JavaScript...</p>
                <p id="details">Initializing the MCP Apps bridge...</p>
                <div class="frame"><img id="preview" alt="Selected Media Resource" hidden></div>
                <p id="imageMeta">Image is browser-rendered; no image bytes have been sent to the model.</p>
                <div class="actions">
                  <button id="handoff" type="button" disabled>Work with this exact image</button>
                  <span id="handoffStatus" role="status">Wait for the original revision to load, then select this action to attach it to ChatGPT.</span>
                </div>
              </main>
              <script>
                (() => {
                  const state = document.getElementById('state');
                  const details = document.getElementById('details');
                  const image = document.getElementById('preview');
                  const imageMeta = document.getElementById('imageMeta');
                  const handoffButton = document.getElementById('handoff');
                  const handoffStatus = document.getElementById('handoffStatus');
                  let initialized = false;
                  let lease = null;
                  let imageReady = false;
                  let transferring = false;
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
                    imageReady = false;
                    handoffButton.disabled = true;
                    state.textContent = 'Loading exact image revision directly from MediaServices storage...';
                    details.textContent = 'Source: ' + target.hostname + ' (signed HTTPS URL)';
                    imageMeta.textContent = (lease.fileName || 'Image') + ' · loading...';
                    image.hidden = false;
                    image.src = target.href;
                  };
                  image.addEventListener('load', () => {
                    imageReady = !!(lease?.mediaResourceId && lease?.revisionId);
                    handoffButton.disabled = !imageReady;
                    state.textContent = 'Exact image revision loaded successfully';
                    imageMeta.textContent = (lease?.fileName || 'Media Resource') + ' · ' +
                      image.naturalWidth + ' × ' + image.naturalHeight +
                      ' · resource ' + (lease?.mediaResourceId || 'unknown') +
                      ' · revision ' + (lease?.revisionId || 'unknown');
                    handoffStatus.textContent = imageReady
                      ? 'Click to attach these exact original pixels to ChatGPT before editing.'
                      : 'Image revision identity is missing; native handoff is disabled.';
                  });
                  image.addEventListener('error', () => {
                    imageReady = false;
                    handoffButton.disabled = true;
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

                  // Old media revisions may have neither ContentType nor FileName.
                  // Detect the MIME from actual source bytes rather than trusting
                  // signed storage response headers or legacy metadata.
                  const detectImageMime = async blob => {
                    const b = new Uint8Array(await blob.slice(0,16).arrayBuffer());
                    if (b.length >= 8 && b[0]===137 && b[1]===80 && b[2]===78 &&
                        b[3]===71 && b[4]===13 && b[5]===10 && b[6]===26 && b[7]===10)
                      return 'image/png';
                    if (b.length >= 3 && b[0]===255 && b[1]===216 && b[2]===255)
                      return 'image/jpeg';
                    if (b.length >= 12 && b[0]===82 && b[1]===73 && b[2]===70 &&
                        b[3]===70 && b[8]===87 && b[9]===69 && b[10]===66 && b[11]===80)
                      return 'image/webp';
                    if (b.length >= 6 && b[0]===71 && b[1]===73 && b[2]===70 &&
                        b[3]===56 && (b[4]===55 || b[4]===57) && b[5]===97)
                      return 'image/gif';
                    return null;
                  };

                  handoffButton.addEventListener('click', async () => {
                    if (transferring || !initialized || !imageReady ||
                        !lease?.mediaResourceId || !lease?.revisionId) return;
                    const openai = window.openai;
                    if (!openai?.uploadFile || !openai?.setWidgetState) {
                      handoffStatus.textContent =
                        'ChatGPT native uploadFile/setWidgetState is not available in this host.';
                      return;
                    }
                    const original = lease;
                    transferring = true;
                    handoffButton.disabled = true;
                    try {
                      const target = new URL(original.url);
                      if (target.protocol !== 'https:' || !permittedHosts.has(target.hostname))
                        throw Error('Original revision URL is not from an approved HTTPS host');

                      handoffStatus.textContent = 'Downloading the exact original revision in the widget...';
                      // This fetch occurs in the widget browser, NOT through the
                      // model-side media-resource-image-download MCP tool.
                      const response = await fetch(target.href, {method:'GET',credentials:'omit'});
                      if (!response.ok) throw Error('Image storage returned HTTP ' + response.status);
                      const maxBytes = 20 * 1024 * 1024;
                      const length = Number(response.headers.get('content-length') || 0);
                      if (length > maxBytes) throw Error('Source image exceeds 20 MiB');
                      const blob = await response.blob();
                      if (!blob.size || blob.size > maxBytes) throw Error('Invalid source-image size');
                      const mime = await detectImageMime(blob);
                      if (!mime) throw Error('Downloaded source is not PNG, JPEG, WebP or GIF');

                      const ext = ({'image/png':'.png','image/jpeg':'.jpg',
                        'image/webp':'.webp','image/gif':'.gif'})[mime];
                      const name = (original.fileName && String(original.fileName).trim()) ||
                        ('media-source-' + original.mediaResourceId + '-' + original.revisionId + ext);
                      const file = new File([blob], name, {type:mime});

                      handoffStatus.textContent = 'Attaching the original image via ChatGPT uploadFile...';
                      const uploaded = await openai.uploadFile(file);
                      if (!uploaded?.fileId) throw Error('ChatGPT did not return a native file ID');

                      // This exact OpenAI-specific imageIds handoff was verified
                      // to make source pixels available for model vision.
                      // Image-editor source-input capability remains a separate test.
                      await Promise.resolve(openai.setWidgetState({
                        modelContent:{
                          action:'aptix_exact_revision_source_image_handoff',
                          mediaResourceId:original.mediaResourceId,
                          revisionId:original.revisionId,
                          sourceImageFileId:uploaded.fileId,
                          imageMimeType:mime,
                          sourceTransport:'openai.uploadFile + setWidgetState.imageIds',
                          instruction:'This attached image is the EXACT original revision to use as image-editor source. When the user asks for edits, pass this actual attached file to the image editing tool. Never regenerate solely from text or silently use a different revision. Do not use media-resource-image-download for this native handoff. If image editing cannot receive the source file, report the limitation rather than fabricate an edit. Do not save revisions without explicit approval.'
                        },
                        privateContent:{
                          selectedResourceId:original.mediaResourceId,
                          selectedRevisionId:original.revisionId,
                          handoff:'exact-original-revision'
                        },
                        imageIds:[uploaded.fileId]
                      }));

                      handoffStatus.textContent = 'Original image attached to ChatGPT.';
                      if (typeof openai.sendFollowUpMessage === 'function') {
                        await openai.sendFollowUpMessage({
                          prompt:'APTIX NATIVE ORIGINAL-IMAGE HANDOFF: I selected Media Resource ' +
                            original.mediaResourceId + ', exact revision ' + original.revisionId +
                            '. Its original pixels have been attached via ChatGPT imageIds. Please first confirm you can inspect the actual image and wait for my edit instruction. When editing, use THIS ATTACHED IMAGE AS THE IMAGE EDITOR SOURCE, not a text-only reconstruction; do not call media-resource-image-download. If the image editor cannot receive the native image attachment as input, tell me and stop instead of regenerating. Do not save until I approve.'
                        });
                        handoffStatus.textContent =
                          'Exact revision attached; requested source-image verification in ChatGPT.';
                      } else {
                        handoffStatus.textContent =
                          'Exact revision attached; ask ChatGPT to verify the selected original.';
                      }
                    } catch (error) {
                      handoffStatus.textContent = 'Native source-image handoff failed: ' +
                        String(error?.message || error) +
                        ' (if the preview loads but fetch fails, check storage CORS/CSP).';
                    } finally {
                      transferring = false;
                      handoffButton.disabled = !imageReady;
                    }
                  });

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
                      appInfo: { name: 'aptix-media-gallery', version: '5.0.0' },
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

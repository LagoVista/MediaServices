using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

namespace LagoVista.MediaServices.Rest.McpWidgets
{
    /// <summary>
    /// APTIX creation brief for new ChatGPT images. This widget collects metadata
    /// and a starting description; it does NOT generate, upload or save media.
    /// Submitting the brief requests image generation through the conversation.
    /// </summary>
    [McpServerResourceType]
    public static class MediaImageCreationWidgetResource
    {
        public const string ResourceUri = "ui://media/image-create/v3.html";
        public const string AppMimeType = "text/html;profile=mcp-app";

        [McpServerResource(UriTemplate = ResourceUri, Name = "APTIX Create New Image", MimeType = AppMimeType)]
        public static TextResourceContents ReadImageCreator()
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
                            ["resourceDomains"] = new JsonArray(),
                            ["connectDomains"] = new JsonArray()
                        }
                    }
                }
            };
        }

        public const string Html = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>APTIX Create New Image</title>
              <style>
                :root {color-scheme:light dark;font-family:system-ui,-apple-system,Segoe UI,sans-serif}
                * {box-sizing:border-box}
                body {margin:0;padding:16px;color:inherit;background:transparent}
                .card {max-width:780px;margin:0 auto;border:1px solid rgba(128,128,128,.3);border-radius:12px;padding:20px}
                .eyebrow {font-size:11px;letter-spacing:.07em;text-transform:uppercase;opacity:.65}
                h2 {font-size:19px;margin:7px 0}
                .intro {font-size:13px;line-height:1.5;opacity:.82;margin:0 0 16px}
                form {display:flex;flex-direction:column;gap:14px}
                .field {display:flex;flex-direction:column;gap:6px}
                label {font-size:13px;font-weight:600}
                input,select,textarea,button {font:inherit}
                input[type="text"],input[type="number"],select,textarea {
                  width:100%;padding:10px 11px;border:1px solid rgba(128,128,128,.5);
                  border-radius:9px;background:transparent;color:inherit;font-size:14px
                }
                select option {color:#202020;background:#fff}
                textarea {resize:vertical;min-height:110px;line-height:1.5}
                .hint {font-size:11px;line-height:1.4;opacity:.74}
                .layout {display:grid;grid-template-columns:1fr 1fr;gap:14px}
                .dimensions {display:grid;grid-template-columns:1fr 1fr;gap:10px}
                .checkline {display:flex;align-items:flex-start;gap:10px;padding:10px 12px;border:1px solid rgba(128,128,128,.3);border-radius:9px}
                .checkline input {margin-top:4px;accent-color:#0969b6}
                .checkline label {font-weight:600}
                .actions {display:flex;gap:12px;align-items:center;flex-wrap:wrap}
                button.primary {background:#0969b6;border:1px solid #0969b6;border-radius:9px;color:#fff;padding:11px 17px;font-weight:700;cursor:pointer}
                button.primary:disabled {opacity:.5;cursor:default}
                #status {font-size:12px;line-height:1.5;overflow-wrap:anywhere;flex:1}
                #fallback {margin-top:8px;min-height:90px}
                .notice {margin:0;font-size:12px;line-height:1.5;opacity:.8}
                [hidden] {display:none!important}
                @media(max-width:540px) {.card{padding:14px}.layout{grid-template-columns:1fr}}
              </style>
            </head>
            <body>
              <main class="card">
                <div class="eyebrow">APTIX / MediaServices / Create New Image / v3</div>
                <h2>Create a new image</h2>
                <p class="intro">Describe what you need or refine the starting idea ChatGPT provided. Develop the image with ChatGPT, then render a private draft in APTIX using the Image Family's actual reference-image bytes.</p>
                <form id="imageForm">
                  <div class="field">
                    <label for="imageName">Image name</label>
                    <input id="imageName" type="text" maxlength="160" placeholder="e.g., AppTrex Insiders — Virtual Team Hero">
                    <div class="hint">A working name is fine. You can change it before saving.</div>
                  </div>
                  <div class="field">
                    <label for="description">Describe the image</label>
                    <textarea id="description" maxlength="4000" placeholder="Describe the subjects, composition, visual style, and what this image is for..."></textarea>
                    <div class="hint">Start from scratch, or edit a description ChatGPT has prefilled. If you leave this empty, ChatGPT will help develop your idea.</div>
                  </div>
                  <div class="field">
                    <label for="library">Media library</label>
                    <select id="library"><option value="">Loading available libraries...</option></select>
                    <div class="hint">New image drafts are stored privately in this library; approval is required before public access.</div>
                  </div>
                  <div class="field">
                    <label for="family">Image family (optional)</label>
                    <select id="family"><option value="">Loading available image families...</option></select>
                    <div class="hint">APTIX uses the family's saved guidance and real reference image pixels during generation.</div>
                  </div>
                  <div class="layout">
                    <div class="field">
                      <label for="sizePreset">Target image size</label>
                      <select id="sizePreset">
                        <option value="1536x1024">Landscape · 1536 × 1024</option>
                        <option value="1920x1080">Website hero · 1920 × 1080</option>
                        <option value="1200x630">Social preview · 1200 × 630</option>
                        <option value="1024x1024">Square · 1024 × 1024</option>
                        <option value="1080x1350">Portrait · 1080 × 1350</option>
                        <option value="custom">Custom dimensions</option>
                      </select>
                    </div>
                    <div class="field">
                      <label>Requested dimensions (pixels)</label>
                      <div class="dimensions">
                        <input type="number" id="width" aria-label="Target width in pixels" min="256" max="4096" step="1" value="1536" required>
                        <input type="number" id="height" aria-label="Target height in pixels" min="256" max="4096" step="1" value="1024" required>
                      </div>
                    </div>
                  </div>
                  <div class="hint">These are target dimensions. APTIX supports landscape, portrait, and square image sizes; generated dimensions may differ.</div>
                  <div class="checkline">
                    <input type="checkbox" id="isPublic" checked>
                    <div>
                      <label for="isPublic">Make image public after approval</label>
                      <div class="hint">Enabled by default for website and marketing content. Public images can be accessed via a public link after saving.</div>
                    </div>
                  </div>
                  <div class="actions">
                    <button id="start" class="primary" type="submit">Start creating image</button>
                    <span id="status" role="status">Ready to develop your image in ChatGPT.</span>
                  </div>
                  <div id="manual" hidden>
                    <label for="fallback">If ChatGPT could not receive the brief, copy this text into the chat:</label>
                    <textarea id="fallback" readonly></textarea>
                  </div>
                  <p class="notice"><strong>Generating creates a private MediaResource draft.</strong> Review it before explicitly approving public access.</p>
                </form>
              </main>
              <script>
                (() => {
                  const el = id => document.getElementById(id);
                  const form = el('imageForm');
                  const name = el('imageName');
                  const description = el('description');
                  const library = el('library');
                  const family = el('family');
                  const sizePreset = el('sizePreset');
                  const width = el('width');
                  const height = el('height');
                  const isPublic = el('isPublic');
                  const status = el('status');
                  const start = el('start');
                  const manual = el('manual');
                  const fallback = el('fallback');
                  let prefilled = false;
                  let submitting = false;
                  let libraries = [];
                  let families = [];

                  const unwrap = input => {
                    const queue = [input];
                    let checks = 0;
                    while (queue.length && checks++ < 50) {
                      const v = queue.shift();
                      if (v == null) continue;
                      if (typeof v === 'string') {
                        try { queue.push(JSON.parse(v)); } catch (_) {}
                      } else if (Array.isArray(v)) {
                        queue.push(...v);
                      } else if (typeof v === 'object') {
                        if (Array.isArray(v.libraries) || Array.isArray(v.Libraries)) return v;
                        for (const key of ['structuredContent','result','model','content','text']) {
                          if (v[key] != null) queue.push(v[key]);
                        }
                      }
                    }
                    return null;
                  };

                  const syncPreset = () => {
                    const value = String(width.value) + 'x' + String(height.value);
                    const values = Array.from(sizePreset.options).map(option => option.value);
                    sizePreset.value = values.includes(value) ? value : 'custom';
                  };
                  sizePreset.addEventListener('change', () => {
                    if (sizePreset.value !== 'custom') {
                      const parts = sizePreset.value.split('x');
                      width.value = parts[0];
                      height.value = parts[1];
                    }
                  });
                  width.addEventListener('input', syncPreset);
                  height.addEventListener('input', syncPreset);

                  const receive = raw => {
                    const data = unwrap(raw);
                    if (!data || prefilled) return;
                    prefilled = true;
                    libraries = (data.libraries || data.Libraries || [])
                      .map(x => ({id:x.id || x.Id, name:x.name || x.Name}))
                      .filter(x => x.id && x.name);
                    library.replaceChildren();
                    if (!libraries.length) {
                      const o = document.createElement('option');
                      o.value = '';
                      o.textContent = 'No library selected';
                      library.appendChild(o);
                    } else {
                      for (const item of libraries) {
                        const o = document.createElement('option');
                        o.value = item.id;
                        o.textContent = item.name;
                        library.appendChild(o);
                      }
                    }
                    const initial = data.initialLibraryId || data.InitialLibraryId;
                    if (libraries.some(x => x.id === initial)) library.value = initial;
                    else if (libraries.length) library.value = libraries[0].id;
                    families = (data.families || data.Families || [])
                      .map(x => ({id:x.id || x.Id, name:x.name || x.Name, key:x.key || x.Key || ''}))
                      .filter(x => x.id && x.name);
                    family.replaceChildren();
                    const none = document.createElement('option');
                    none.value = '';
                    none.textContent = 'No image family';
                    family.appendChild(none);
                    for (const item of families) {
                      const option = document.createElement('option');
                      option.value = item.id;
                      option.textContent = item.name;
                      family.appendChild(option);
                    }
                    const initialFamily = data.initialFamilyId || data.InitialFamilyId;
                    if (families.some(x => x.id === initialFamily)) family.value = initialFamily;
                    name.value = data.name || data.Name || '';
                    description.value = data.description || data.Description || '';
                    width.value = data.width || data.Width || 1536;
                    height.value = data.height || data.Height || 1024;
                    isPublic.checked = (data.isPublic ?? data.IsPublic ?? true) === true;
                    syncPreset();
                    status.textContent = 'Brief ready. Review the details and choose Start creating image.';
                  };

                  const buildBrief = () => {
                    const w = Number(width.value);
                    const h = Number(height.value);
                    if (!Number.isInteger(w) || !Number.isInteger(h) ||
                        w < 256 || w > 4096 || h < 256 || h > 4096)
                      throw Error('Width and height must be whole numbers from 256 to 4096 pixels.');
                    const id = library.value || '';
                    const chosen = libraries.find(x => x.id === id);
                    const familyId = family.value || '';
                    const chosenFamily = families.find(x => x.id === familyId);
                    return {
                      imageName:name.value.trim(),
                      imageDescription:description.value.trim(),
                      mediaLibraryId:id,
                      mediaLibraryName:chosen ? chosen.name : '',
                      imageFamilyId:familyId,
                      imageFamilyName:chosenFamily ? chosenFamily.name : '',
                      imageFamilyKey:chosenFamily ? chosenFamily.key : '',
                      targetWidth:w,
                      targetHeight:h,
                      isPublic:isPublic.checked
                    };
                  };

                  const messageFor = brief => {
                    return [
                      'APTIX CREATE NEW IMAGE — user-submitted brief.',
                      'Use media-image-generate MCP (the existing APTIX image-generation service), NOT native ChatGPT image generation.',
                      'Image name / ResourceName: '+(brief.imageName || '(help me choose a name first)'),
                      'Image description / UserPrompt: '+(brief.imageDescription || '(help me develop the image idea before generating)'),
                      'Target: '+brief.targetWidth+' x '+brief.targetHeight+' pixels; choose the closest supported ImageSize preset.',
                      'MediaLibrary EntityHeader: '+(brief.mediaLibraryId ? JSON.stringify({id:brief.mediaLibraryId,text:brief.mediaLibraryName}) : 'null'),
                      'ImageFamily EntityHeader: '+(brief.imageFamilyId ? JSON.stringify({id:brief.imageFamilyId,text:brief.imageFamilyName,key:brief.imageFamilyKey}) : 'null'),
                      'Desired PUBLIC state only AFTER APPROVAL: '+String(brief.isPublic),
                      'Once the name and description are ready, call media-image-generate with the ImageGenerationRequest:',
                      '- Intent=CreateNew, MediaResource=null, ResourceName as above, UserPrompt as above, NumberGenerated=1.',
                      '- ImageFamily and MediaLibrary as the EntityHeaders above.',
                      '- ReferenceMediaResources=[] so the APTIX service loads the Image Family and passes its real reference-image bytes to the provider.',
                      '- IsPublic=false for this private draft, regardless of desired public state after approval.',
                      '- Use default EditorialPhotography style and Standard quality unless the conversation specifies otherwise.',
                      'The backend saves a private draft MediaResource. Report its ID and revision, and open media-gallery-preview for review.',
                      'Allow follow-up discussions and manual image editing via copying the preview image into ChatGPT. Publish only after explicit approval; do not import duplicate media.'
                    ].join('\n');
                  };

                  form.addEventListener('submit', async event => {
                    event.preventDefault();
                    if (submitting) return;
                    let brief;
                    try { brief = buildBrief(); }
                    catch (error) { status.textContent = String(error.message || error); return; }
                    const message = messageFor(brief);
                    if (!window.openai?.sendFollowUpMessage) {
                      fallback.value = message;
                      manual.hidden = false;
                      status.textContent = 'This ChatGPT host cannot submit the brief automatically. Copy the brief below into the chat message box.';
                      return;
                    }
                    submitting = true;
                    start.disabled = true;
                    manual.hidden = true;
                    status.textContent = 'Sending your image brief to ChatGPT...';
                    try {
                      if (typeof window.openai.setWidgetState === 'function') {
                        try {
                          await Promise.resolve(window.openai.setWidgetState({
                            modelContent:{
                              action:'aptix_create_new_image_brief',
                              ...brief,
                              instruction:'Use media-image-generate MCP to create a new PRIVATE APTIX MediaResource with EntityHeader ImageFamily and MediaLibrary, resolving family references in the backend. Publish only after approval.'
                            },
                            privateContent:{creationBrief:brief}
                          }));
                        } catch (_) {
                          // The full brief is also in the follow-up prompt.
                        }
                      }
                      await window.openai.sendFollowUpMessage({prompt:message});
                      status.textContent = 'Brief sent. Continue reviewing and refining the image in the ChatGPT conversation.';
                    } catch (error) {
                      fallback.value = message;
                      manual.hidden = false;
                      status.textContent = 'Could not send the brief: ' +
                        String(error?.message || error) + '. Copy the brief below into ChatGPT instead.';
                    } finally {
                      submitting = false;
                      start.disabled = false;
                    }
                  });

                  window.addEventListener('message', event => {
                    if (event.source !== window.parent || !event.data || event.data.jsonrpc !== '2.0') return;
                    const msg = event.data;
                    if (msg.method === 'ui/notifications/tool-result') {receive(msg.params);return;}
                    if (msg.id === 1 && msg.result) {
                      window.parent.postMessage({
                        jsonrpc:'2.0',method:'ui/notifications/initialized',params:{}
                      },'*');
                      if (window.openai?.toolOutput) receive(window.openai.toolOutput);
                    }
                    if (msg.id === 1 && msg.error)
                      status.textContent = 'MCP Apps host rejected initialization.';
                  });
                  if (window.openai?.toolOutput) receive(window.openai.toolOutput);
                  window.parent.postMessage({
                    jsonrpc:'2.0',id:1,method:'ui/initialize',
                    params:{
                      protocolVersion:'2026-01-26',
                      appInfo:{name:'aptix-image-create',version:'3.0.0'},
                      appCapabilities:{}
                    }
                  },'*');
                })();
              </script>
            </body>
            </html>
            """;
    }
}

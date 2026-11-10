using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

namespace LagoVista.MediaServices.Rest.McpWidgets
{
    /// <summary>
    /// APTIX image creation workspace. It saves generated MediaResources immediately
    /// through the existing MCP generator, and previews exact saved revisions.
    /// ChatGPT editing still uses user copy/paste into the conversation.
    /// </summary>
    [McpServerResourceType]
    public static class MediaImageCreationWidgetResource
    {
        public const string ResourceUri = "ui://media/image-create/v4.html";
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
                            ["resourceDomains"] = new JsonArray(
                                "https://storage-dev.nuviot.com",
                                "https://storage.nuviot.com"),
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
                .saved-result {margin-top:18px;padding-top:16px;border-top:1px solid rgba(128,128,128,.35)}
                .saved-result h3 {font-size:17px;margin:7px 0 12px}
                .image-frame {display:flex;align-items:center;justify-content:center;min-height:160px;border-radius:9px;border:1px solid rgba(128,128,128,.3);background:rgba(128,128,128,.06);overflow:hidden}
                .image-frame img {display:block;max-width:100%;max-height:520px;object-fit:contain}
                .saved-result a {font-size:13px;color:inherit;text-decoration:underline}
                .saved-result p {font-size:12px;line-height:1.5}
                .image-details {font-size:11px;opacity:.75;overflow-wrap:anywhere}
                .saved-result .hint {margin:8px 0}
                [hidden] {display:none!important}
                @media(max-width:540px) {.card{padding:14px}.layout{grid-template-columns:1fr}}
              </style>
            </head>
            <body>
              <main class="card">
                <div class="eyebrow">APTIX / MediaServices / Create New Image / v4</div>
                <h2>Create a new image</h2>
                <p class="intro">Describe an image, choose a Media Library, and generate it. APTIX saves the image automatically and shows it here. Copy the image into ChatGPT whenever you want to edit it.</p>
                <form id="imageForm">
                  <div class="field">
                    <label for="imageName">Image name</label>
                    <input id="imageName" type="text" maxlength="160" placeholder="e.g., AppTrex Insiders — Virtual Team Hero">
                    <div class="hint">Optional — we can name the image from your description. Rename it in the library anytime.</div>
                  </div>
                  <div class="field">
                    <label for="description">Describe the image</label>
                    <textarea id="description" maxlength="4000" placeholder="Describe the subjects, composition, visual style, and what this image is for..."></textarea>
                    <div class="hint">A short description is enough. You can ask ChatGPT to help write it first.</div>
                  </div>
                  <div class="field">
                    <label for="library">Media library</label>
                    <select id="library"><option value="">Loading available libraries...</option></select>
                    <div class="hint">Every generated image is saved to this Media Library immediately; delete unwanted images from the library.</div>
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
                      <label for="isPublic">Make this image public when generated</label>
                      <div class="hint">Public images can be accessed by link immediately. Uncheck for private library images.</div>
                    </div>
                  </div>
                  <div class="actions">
                    <button id="start" class="primary" type="submit">Generate and save image</button>
                    <span id="status" role="status">Connecting to APTIX...</span>
                  </div>
                  <p class="notice">Each generation creates a saved MediaResource. There is no separate Save step; remove unwanted images from your library.</p>
                </form>
                <section id="savedResult" class="saved-result" hidden aria-live="polite">
                  <div class="eyebrow">Saved in your Media Library</div>
                  <h3 id="savedName"></h3>
                  <div class="image-frame"><img id="savedImage" alt="Saved, full-resolution generated image. Right-click and choose Copy image to edit it in ChatGPT." hidden></div>
                  <p id="previewStatus" role="status">Loading saved image...</p>
                  <a id="openOriginal" href="#" target="_blank" rel="noopener noreferrer" hidden>Open full-size original</a>
                  <p class="hint">To edit: right-click the image and choose <strong>Copy image</strong> (not Copy image address), then paste it into ChatGPT and describe your changes. The MediaResource ID and revision are passed to ChatGPT for saving edits back to this image.</p>
                  <div id="savedIdentity" class="image-details"></div>
                </section>
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
                  const savedResult = el('savedResult');
                  const savedName = el('savedName');
                  const savedImage = el('savedImage');
                  const savedIdentity = el('savedIdentity');
                  const previewStatus = el('previewStatus');
                  const openOriginal = el('openOriginal');
                  const allowedHosts = new Set(['storage-dev.nuviot.com','storage.nuviot.com']);
                  let connected = false;
                  let requestCounter = 20;
                  const pending = new Map();
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
                    status.textContent = connected ? 'Ready. Generate and save directly to your Media Library.' : 'Brief ready. Connecting to APTIX...';
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

                  const errorText = value => {
                    if (typeof value === 'string') return value;
                    return value?.errorMessage || value?.message ||
                      (Array.isArray(value?.errors) && value.errors[0]?.message) ||
                      'APTIX image operation failed.';
                  };
                  const findPayload = (raw, predicate) => {
                    const queue = [raw];
                    let checks = 0;
                    while (queue.length && checks++ < 75) {
                      const v = queue.shift();
                      if (v == null) continue;
                      if (typeof v === 'string') {
                        try {queue.push(JSON.parse(v))} catch (_) {}
                        continue;
                      }
                      if (Array.isArray(v)) {queue.push(...v); continue;}
                      if (typeof v !== 'object') continue;
                      if (v.isError === true || v.successful === false) throw Error(errorText(v));
                      if (predicate(v)) return v;
                      for (const key of ['structuredContent','result','model','content','text'])
                        if (v[key] != null) queue.push(v[key]);
                    }
                    throw Error('APTIX returned an unexpected response.');
                  };
                  const callTool = (toolName,args) => new Promise((resolve,reject) => {
                    if (!connected) {reject(Error('MCP App is not connected. Reopen the image creator.'));return;}
                    const id = ++requestCounter;
                    pending.set(id,{resolve,reject});
                    window.parent.postMessage({jsonrpc:'2.0',id,method:'tools/call',
                      params:{name:toolName,arguments:args}},'*');
                  });
                  const safeUrl = value => {
                    if (typeof value !== 'string') return null;
                    try {
                      const url = new URL(value);
                      return url.protocol === 'https:' && allowedHosts.has(url.hostname) ? url.href : null;
                    } catch (_) {return null;}
                  };
                  const publishSelection = async (media,brief) => {
                    if (typeof window.openai?.setWidgetState !== 'function') return;
                    try {
                      await Promise.resolve(window.openai.setWidgetState({
                        modelContent:{
                          action:'aptix_media_image_selected_for_manual_edit',
                          mediaResourceId:media.id,
                          revisionId:media.currentRevision,
                          imageName:media.name,
                          mediaLibraryId:brief.mediaLibraryId,
                          isPublic:media.isPublic === true,
                          sourceTransport:'user-copies-exact-original-and-pastes-into-chat-composer',
                          instruction:'This MediaResource was generated and SAVED in the APTIX library. The widget image is a preview only, not a source attachment. For edits, use the image the user pastes into ChatGPT; when approved, use media-revise-chatgpt-image with the selected mediaResourceId and current revision guard. Never create a duplicate resource for the edited image.'
                        },
                        privateContent:{
                          selectedResourceId:media.id,
                          selectedRevisionId:media.currentRevision,
                          libraryId:brief.mediaLibraryId
                        }
                      }));
                    } catch (_) {
                      // The exact resource and revision remain visible for manual reference.
                    }
                  };
                  const sizeSelection = (w,h) => {
                    if (w >= h * 1.12) return {key:'landscape1536x1024',text:'Landscape'};
                    if (h >= w * 1.12) return {key:'portrait1024x1536',text:'Portrait'};
                    return {key:'square1024x1024',text:'Square'};
                  };
                  const requestFor = (model,brief) => {
                    const request = JSON.parse(JSON.stringify(model));
                    const selectedSize = sizeSelection(brief.targetWidth,brief.targetHeight);
                    request.imageSize = {
                      ...(request.imageSize || {}),
                      id:selectedSize.key,key:selectedSize.key,text:selectedSize.text
                    };
                    request.mediaLibrary = brief.mediaLibraryId
                      ? {id:brief.mediaLibraryId,text:brief.mediaLibraryName,entityType:'MediaLibrary'} : null;
                    request.imageFamily = brief.imageFamilyId
                      ? {id:brief.imageFamilyId,key:brief.imageFamilyKey,
                         text:brief.imageFamilyName,entityType:'ImageFamily'} : null;
                    request.mediaResource = null;
                    request.mediaResourceId = null;
                    request.previousResponseId = null;
                    request.referenceMediaResourceId = null;
                    request.referenceMediaResources = [];
                    request.resourceName = brief.imageName || brief.imageDescription.replace(/\s+/g,' ').slice(0,80).trim();
                    request.userPrompt = brief.imageDescription;
                    request.numberGenerated = 1;
                    request.isPublic = brief.isPublic;
                    return request;
                  };
                  const showSavedIdentity = media => {
                    savedResult.hidden = false;
                    savedName.textContent = media.name || 'Generated image';
                    savedIdentity.textContent = 'MediaResource ID: '+media.id+
                      ' | Revision ID: '+(media.currentRevision || '(not provided)')+
                      ' | '+(media.isPublic ? 'Public' : 'Private');
                    savedImage.hidden = true;
                    savedImage.removeAttribute('src');
                    openOriginal.hidden = true;
                    openOriginal.removeAttribute('href');
                  };
                  const showOriginal = async media => {
                    if (!media.currentRevision) {
                      previewStatus.textContent = 'The image was saved, but its revision ID is missing. Open it from the Media Library.';
                      return;
                    }
                    previewStatus.textContent = 'Loading saved, full-resolution image...';
                    const raw = await callTool('media-gallery-image-lease',
                      {id:media.id,revisionid:media.currentRevision});
                    const lease = findPayload(raw, v => typeof v.url === 'string' && !!v.mediaResourceId);
                    if (lease.mediaResourceId !== media.id || lease.revisionId !== media.currentRevision)
                      throw Error('MediaServices returned a different image revision.');
                    const originalUrl = safeUrl(lease.url);
                    if (!originalUrl) throw Error('The image URL did not come from approved storage.');
                    openOriginal.href = originalUrl;
                    openOriginal.hidden = false;
                    savedImage.src = originalUrl;
                    savedImage.hidden = false;
                    previewStatus.textContent = 'Image saved. Loading full-resolution preview...';
                  };
                  savedImage.addEventListener('load', () => {
                    previewStatus.textContent = 'Saved and ready. Right-click to Copy image, then paste into ChatGPT to edit.';
                  });
                  savedImage.addEventListener('error', () => {
                    savedImage.hidden = true;
                    previewStatus.textContent = 'The image was saved, but its preview could not load. Open the full-size original or find it in the Media Library.';
                  });
                  form.addEventListener('submit', async event => {
                    event.preventDefault();
                    if (submitting) return;
                    let brief;
                    try {
                      brief = buildBrief();
                      if (!brief.mediaLibraryId) throw Error('Choose a Media Library before generating.');
                      if (!brief.imageDescription) throw Error('Describe the image first. ChatGPT can help with the wording.');
                      if (!connected) throw Error('APTIX MCP is not connected yet. Reopen the widget or retry.');
                    } catch (error) {
                      status.textContent = errorText(error);
                      return;
                    }
                    submitting = true;
                    start.disabled = true;
                    status.textContent = 'Generating image and saving it directly to your library...';
                    let generated = null;
                    try {
                      const factory = findPayload(
                        await callTool('media-image-generation-request-factory', {}),
                        v => v.model && v.model.imageStyle && v.model.imageSize && v.model.imageQuality);
                      const request = requestFor(factory.model,brief);
                      const raw = await callTool('media-image-generate',{request});
                      const response = findPayload(raw,v =>
                        (Array.isArray(v.result) && v.result.length > 0 && !!v.result[0]?.id) ||
                        (Array.isArray(v.model) && v.model.length > 0 && !!v.model[0]?.id));
                      const resources = Array.isArray(response.result) ? response.result : response.model;
                      generated = resources[0];
                      if (!generated?.id) throw Error('APTIX did not return a MediaResource ID.');
                      showSavedIdentity(generated);
                      await publishSelection(generated,brief);
                      status.textContent = 'Image created in your Media Library. Verifying its saved revision...';
                      const verified = findPayload(
                        await callTool('media-resources-get',{id:generated.id}),
                        v => v.model && v.model.id === generated.id);
                      const media = verified.model;
                      if (media.mediaLibrary?.id !== brief.mediaLibraryId)
                        throw Error('The saved image was not linked to the selected Media Library.');
                      if (!media.currentRevision)
                        throw Error('The saved MediaResource has no current revision ID.');
                      generated = media;
                      showSavedIdentity(media);
                      await publishSelection(media,brief);
                      status.textContent = 'Saved in the selected Media Library. You can generate another or edit this image.';
                      try {await showOriginal(media)}
                      catch (error) {
                        previewStatus.textContent = 'Image saved, but preview unavailable: '+errorText(error)+
                          ' Open it from the Media Library.';
                      }
                    } catch (error) {
                      if (generated?.id) {
                        showSavedIdentity(generated);
                        await publishSelection(generated,brief);
                        status.textContent = 'Image '+generated.id+' may already be saved. Check the Media Library before generating again.';
                        previewStatus.textContent = 'Could not finish verifying the image: '+errorText(error);
                      } else {
                        status.textContent = 'Could not complete generation: '+errorText(error)+
                          ' If generation was interrupted, check your Media Library before retrying.';
                      }
                    } finally {
                      submitting = false;
                      start.disabled = false;
                    }
                  });

                  window.addEventListener('message', event => {
                    if (event.source !== window.parent || !event.data || event.data.jsonrpc !== '2.0') return;
                    const msg = event.data;
                    if (msg.id === 1) {
                      if (msg.error) {
                        status.textContent = 'MCP Apps host rejected initialization: '+errorText(msg.error);
                        return;
                      }
                      if (msg.result) {
                        window.parent.postMessage({jsonrpc:'2.0',
                          method:'ui/notifications/initialized',params:{}},'*');
                        connected = true;
                        if (window.openai?.toolOutput) receive(window.openai.toolOutput);
                        if (prefilled) status.textContent = 'Connected. Generate and save directly to your Media Library.';
                      }
                      return;
                    }
                    if (pending.has(msg.id)) {
                      const request = pending.get(msg.id);
                      pending.delete(msg.id);
                      if (msg.error) request.reject(Error(errorText(msg.error)));
                      else request.resolve(msg.result);
                      return;
                    }
                    if (msg.method === 'ui/notifications/tool-result') receive(msg.params);
                  });
                  if (window.openai?.toolOutput) receive(window.openai.toolOutput);
                  window.parent.postMessage({
                    jsonrpc:'2.0',id:1,method:'ui/initialize',
                    params:{
                      protocolVersion:'2026-01-26',
                      appInfo:{name:'aptix-image-create',version:'4.0.0'},
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

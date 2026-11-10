using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

namespace LagoVista.MediaServices.Rest.McpWidgets
{
    /// <summary>
    /// APTIX mixed-media library browser. The MediaServices assembly owns its HTML;
    /// the API host discovers and serves the registered MCP Apps resource.
    /// </summary>
    [McpServerResourceType]
    public static class MediaGalleryBrowserWidgetResource
    {
        public const string ResourceUri = "ui://media/gallery/v6.html";
        public const string AppMimeType = "text/html;profile=mcp-app";

        [McpServerResource(UriTemplate = ResourceUri, Name = "APTIX Media Library Browser", MimeType = AppMimeType)]
        public static TextResourceContents ReadMediaGalleryBrowser()
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
                            ["connectDomains"] = new JsonArray(
                                "https://storage-dev.nuviot.com",
                                "https://storage.nuviot.com")
                        }
                    }
                }
            };
        }

        // All business data is obtained from MediaServices MCP tools. The page only
        // receives fifteen items per tools/call. Only explicit user image handoff
        // downloads the selected original into the browser for ChatGPT upload.
        public const string Html = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>APTIX Media Libraries</title>
              <style>
                :root {color-scheme:light dark;font-family:system-ui,-apple-system,Segoe UI,sans-serif}
                * {box-sizing:border-box}
                body {margin:0;padding:14px;color:inherit;background:transparent}
                button,select {font:inherit}
                .shell {max-width:840px;margin:0 auto}
                .heading {display:flex;align-items:baseline;justify-content:space-between;gap:12px;flex-wrap:wrap}
                .eyebrow {font-size:11px;letter-spacing:.07em;text-transform:uppercase;opacity:.65}
                h2 {font-size:17px;margin:5px 0 12px}
                .controls {display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-bottom:10px}
                .controls label {font-size:12px;font-weight:600}
                select {min-width:200px;max-width:100%;border-radius:8px;padding:7px 9px;color:inherit;background:transparent;border:1px solid rgba(128,128,128,.45)}
                select option {color:#202020;background:#fff}
                .quiet {font-size:12px;opacity:.7}
                #status {font-size:12px;min-height:20px;margin:8px 0;overflow-wrap:anywhere}
                .grid {display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px}
                .tile {appearance:none;display:flex;flex-direction:column;min-width:0;min-height:170px;padding:0;overflow:hidden;text-align:left;border:1px solid rgba(128,128,128,.3);border-radius:10px;background:rgba(128,128,128,.05);color:inherit;cursor:pointer}
                .tile:hover {border-color:rgba(0,146,218,.8)}
                .tile:focus-visible {outline:2px solid #0094e7;outline-offset:2px}
                .tile.selected {border:2px solid #0094e7}
                .visual {display:flex;align-items:center;justify-content:center;width:100%;height:130px;overflow:hidden;background:rgba(128,128,128,.11)}
                .visual img {width:100%;height:100%;object-fit:cover;display:block}
                .glyph {font-size:42px}
                .label {padding:9px 10px 11px;min-width:0;width:100%}
                .name {font-weight:600;font-size:12px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
                .kind {font-size:11px;opacity:.7;margin-top:3px;text-transform:capitalize;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
                .pager {display:flex;align-items:center;justify-content:space-between;gap:10px;margin-top:12px}
                button.nav {border:1px solid rgba(128,128,128,.45);border-radius:8px;padding:7px 12px;color:inherit;background:transparent;cursor:pointer}
                button.nav:disabled {opacity:.38;cursor:default}
                .selected-info {margin-top:12px;border:1px solid rgba(128,128,128,.3);border-radius:9px;padding:10px 12px;font-size:12px}
                .selected-title {font-weight:700;font-size:13px;margin-bottom:4px}
                .selected-actions {display:flex;gap:10px;align-items:center;flex-wrap:wrap;margin-top:11px}
                #handoffStatus {font-size:12px;overflow-wrap:anywhere;flex:1;min-width:180px}
                .nav.primary {background:#0969b6;color:#fff;border-color:#0969b6;font-weight:600}
                .nav.primary:disabled {opacity:.45}
                .details {opacity:.72;overflow-wrap:anywhere}
                .empty {grid-column:1/-1;text-align:center;padding:30px 10px;opacity:.75;font-size:13px}
                @media(max-width:480px) {.grid{grid-template-columns:repeat(2,minmax(0,1fr))}.controls{align-items:stretch;flex-direction:column}.controls select{width:100%}}
              </style>
            </head>
            <body>
              <main class="shell">
                <div class="heading">
                  <div><div class="eyebrow">APTIX · MediaServices · v6</div><h2>Media Library Browser</h2></div>
                  <div class="quiet">15 items / page</div>
                </div>
                <div class="controls">
                  <label for="libraries">Library</label>
                  <select id="libraries" disabled><option>Loading libraries...</option></select>
                  <span id="libraryCount" class="quiet"></span>
                </div>
                <div id="status" role="status">Connecting to ChatGPT MCP Apps...</div>
                <div class="grid" id="gallery" aria-live="polite"></div>
                <div class="pager">
                  <button id="previous" class="nav" disabled>Previous</button>
                  <span id="pageLabel" class="quiet">Page 1</span>
                  <button id="next" class="nav" disabled>Next</button>
                </div>
                <section id="selected" class="selected-info" hidden>
                  <div id="selectedTitle" class="selected-title"></div>
                  <div id="selectedDetails" class="details"></div>
                  <div class="selected-actions">
                    <button id="handoff" class="nav primary" type="button" disabled>Work with this image</button>
                    <span id="handoffStatus" role="status" class="quiet">Select an image to hand off to ChatGPT.</span>
                  </div>
                </section>
              </main>
              <script>
                (() => {
                  const el = id => document.getElementById(id);
                  const libraries = el('libraries');
                  const grid = el('gallery');
                  const status = el('status');
                  const previous = el('previous');
                  const next = el('next');
                  const pageLabel = el('pageLabel');
                  let connected = false;
                  let openData = null;
                  let libraryId = null;
                  let pageIndex = 1;
                  let pageData = null;
                  let loading = false;
                  let transferring = false;
                  let selectedItem = null;
                  const handoff = el('handoff');
                  const handoffStatus = el('handoffStatus');
                  let requestCounter = 20;
                  let loadGeneration = 0;
                  const pending = new Map();
                  const permittedHosts = new Set(['storage-dev.nuviot.com', 'storage.nuviot.com']);

                  const errorText = value => {
                    if (!value) return 'Unknown MCP error';
                    if (typeof value === 'string') return value;
                    return value.errorMessage || value.message ||
                      (Array.isArray(value.errors) && value.errors[0]?.message) ||
                      'MediaServices request failed';
                  };
                  const unwrap = value => {
                    const queue = [value];
                    let checks = 0;
                    while (queue.length && checks++ < 40) {
                      const v = queue.shift();
                      if (v == null) continue;
                      if (typeof v === 'string') {
                        try { queue.push(JSON.parse(v)); } catch (_) {}
                      } else if (Array.isArray(v)) {
                        queue.push(...v);
                      } else if (typeof v === 'object') {
                        if (v.successful === false || v.isError === true)
                          return {error: errorText(v)};
                        if (Array.isArray(v.libraries) || Array.isArray(v.items) ||
                            (typeof v.url === 'string' && v.mediaResourceId)) return v;
                        for (const key of ['structuredContent','result','model','content','text']) {
                          if (v[key] != null) queue.push(v[key]);
                        }
                      }
                    }
                    return null;
                  };
                  const safeUrl = value => {
                    if (typeof value !== 'string') return null;
                    try {
                      const uri = new URL(value);
                      return uri.protocol === 'https:' && permittedHosts.has(uri.hostname) ? uri.href : null;
                    } catch (_) {return null;}
                  };
                  const typeIcon = kind => {
                    return ({image:'🖼️',video:'🎬',audio:'🎵',document:'📄',archive:'🗜️',link:'🔗',file:'📁'})[kind] || '📁';
                  };
                  const make = (tag, className, textValue) => {
                    const node = document.createElement(tag);
                    if (className) node.className = className;
                    if (textValue !== undefined) node.textContent = String(textValue);
                    return node;
                  };
                  const setStatus = msg => {status.textContent = msg};
                  const resetSelection = () => {
                    selectedItem = null;
                    handoff.disabled = true;
                    handoffStatus.textContent = 'Select an image to hand off to ChatGPT.';
                    el('selected').hidden = true;
                  };
                  const selectItem = (item, tile) => {
                    selectedItem = item;
                    for (const child of grid.children) child.classList.remove('selected');
                    tile.classList.add('selected');
                    el('selectedTitle').textContent = item.name || 'Untitled media resource';
                    const size = typeof item.contentSize === 'number'
                      ? ' · ' + (item.contentSize / 1048576).toFixed(2) + ' MB' : '';
                    el('selectedDetails').textContent =
                      (item.kind || 'file') + (item.mimeType ? ' · ' + item.mimeType : '') +
                      size + ' · Media Resource: ' + item.id +
                      (item.revisionId ? ' · Revision: ' + item.revisionId : '');
                    handoff.disabled = transferring || item.kind !== 'image' || !item.revisionId;
                    handoffStatus.textContent = item.kind !== 'image'
                      ? 'Image handoff is available for images only.'
                      : !item.revisionId
                        ? 'This item has no persisted image revision to transfer.'
                        : 'Only this selected image will be uploaded into ChatGPT.';
                    el('selected').hidden = false;
                  };
                  const render = () => {
                    grid.replaceChildren();
                    resetSelection();
                    const items = Array.isArray(pageData?.items) ? pageData.items.slice(0,15) : [];
                    if (!items.length) {
                      grid.appendChild(make('div','empty','No Media Resources on this page.'));
                    }
                    for (const item of items) {
                      const tile = make('button','tile');
                      tile.type = 'button';
                      tile.title = item.name || 'Media Resource';
                      const visual = make('div','visual');
                      const url = safeUrl(item.previewUrl);
                      if (url && (item.kind === 'image' || item.kind === 'video')) {
                        const img = make('img');
                        img.src = url;
                        img.alt = item.name || 'Media thumbnail';
                        img.loading = 'lazy';
                        img.decoding = 'async';
                        img.addEventListener('error', () => {
                          visual.replaceChildren(make('div','glyph',typeIcon(item.kind)));
                        });
                        visual.appendChild(img);
                      } else {
                        visual.appendChild(make('div','glyph',typeIcon(item.kind)));
                      }
                      const label = make('div','label');
                      label.appendChild(make('div','name',item.name || item.fileName || 'Untitled'));
                      label.appendChild(make('div','kind',
                        (item.kind || 'file') + (item.usesOriginalAsPreview ? ' · original image' : '')));
                      tile.append(visual,label);
                      tile.addEventListener('click', () => selectItem(item,tile));
                      grid.appendChild(tile);
                    }
                    previous.disabled = loading || pageIndex <= 1;
                    next.disabled = loading || !pageData?.hasMore;
                    pageLabel.textContent = 'Page ' + pageIndex + ' · ' + items.length + ' item' + (items.length===1?'':'s');
                  };
                  const callTool = (name, args) => new Promise((resolve,reject) => {
                    const id = ++requestCounter;
                    pending.set(id,{resolve,reject});
                    window.parent.postMessage({
                      jsonrpc:'2.0',id,method:'tools/call',
                      params:{name,arguments:args}
                    },'*');
                  });
                  // This is a user-initiated bridge: the gallery continues to browse
                  // without model image bytes until this button is explicitly clicked.
                  handoff.addEventListener('click', async () => {
                    if (transferring || !selectedItem || selectedItem.kind !== 'image' ||
                        !selectedItem.id || !selectedItem.revisionId) return;

                    const openai = window.openai;
                    if (!openai?.uploadFile || !openai?.setWidgetState) {
                      handoffStatus.textContent = 'This host does not expose ChatGPT native file upload and image state.';
                      return;
                    }
                    const item = selectedItem;
                    transferring = true;
                    handoff.disabled = true;
                    try {
                      handoffStatus.textContent = 'Requesting a signed URL for the selected original...';
                      const lease = unwrap(await callTool('media-gallery-image-lease',
                        {id:item.id,revisionid:item.revisionId}));
                      if (!lease || lease.error) throw Error(lease?.error || 'No original image lease returned');
                      if (lease.mediaResourceId !== item.id || lease.revisionId !== item.revisionId)
                        throw Error('MediaServices returned a different image revision');
                      const url = safeUrl(lease.url);
                      if (!url) throw Error('Signed original URL was not from an approved HTTPS storage host');

                      handoffStatus.textContent = 'Retrieving selected image directly from storage...';
                      // Fetch requires BOTH widget connectDomains and cross-origin CORS
                      // on the storage host. Rendering <img> alone does not prove CORS.
                      const response = await fetch(url, {method:'GET', credentials:'omit'});
                      if (!response.ok) throw Error('Image download returned HTTP ' + response.status);
                      const limit = 25 * 1024 * 1024;
                      const headerSize = Number(response.headers.get('content-length') || 0);
                      if (headerSize > limit) throw Error('Selected original exceeds 25 MB handoff limit');
                      const blob = await response.blob();
                      if (!blob.size || blob.size > limit) throw Error('Selected image is empty or exceeds 25 MB');

                      const mime = (lease.contentType || item.mimeType || blob.type || '').toLowerCase();
                      const extension = ({'image/png':'.png','image/jpeg':'.jpg',
                        'image/webp':'.webp','image/gif':'.gif'})[mime];
                      if (!extension) throw Error('Selected resource is not a supported image format');
                      const fileName = lease.fileName || item.fileName || ('media-' + item.id + extension);
                      const file = new File([blob], fileName, {type:mime});

                      handoffStatus.textContent = 'Uploading selected image through ChatGPT native file API...';
                      const uploaded = await openai.uploadFile(file);
                      if (!uploaded?.fileId) throw Error('ChatGPT did not return a file ID');

                      // imageIds is an OpenAI widget extension. It makes only this
                      // uploaded file visible to the model in subsequent turns.
                      openai.setWidgetState({
                        modelContent:{
                          action:'review_selected_media_image',
                          instruction:'Describe the selected APTIX MediaServices image before editing it.',
                          mediaResourceId:item.id,
                          revisionId:item.revisionId,
                          imageName:item.name
                        },
                        privateContent:{
                          libraryId,
                          selectedResourceId:item.id,
                          selectedRevisionId:item.revisionId
                        },
                        imageIds:[uploaded.fileId]
                      });

                      handoffStatus.textContent = 'Image uploaded and attached to model context.';
                      if (typeof openai.sendFollowUpMessage === 'function') {
                        await openai.sendFollowUpMessage({
                          prompt:'I selected the APTIX MediaServices image "' +
                            (item.name || 'Untitled') + '" (resource ' + item.id +
                            ', revision ' + item.revisionId +
                            '). Please describe what you actually see in the image so we can verify the handoff. Do not edit or save it yet.'
                        });
                        handoffStatus.textContent = 'Image attached to ChatGPT. Requested visual verification.';
                      } else {
                        handoffStatus.textContent =
                          'Image attached. Ask ChatGPT to describe the selected image to verify handoff.';
                      }
                    } catch (error) {
                      handoffStatus.textContent = 'Handoff failed: ' + errorText(error) +
                        '. If previews load but fetch fails, check storage CORS and connectDomains.';
                    } finally {
                      transferring = false;
                      handoff.disabled = !selectedItem || selectedItem.kind !== 'image' ||
                        !selectedItem.revisionId;
                    }
                  });

                  const loadPage = async requested => {
                    if (!connected || !libraryId || loading) return;
                    const generation = ++loadGeneration;
                    const requestedLibrary = libraryId;
                    loading = true;
                    previous.disabled = true;
                    next.disabled = true;
                    setStatus('Loading up to fifteen Media Resources...');
                    try {
                      const result = unwrap(await callTool('media-gallery-page',
                        {libraryid:requestedLibrary,page:requested}));
                      if (generation !== loadGeneration || requestedLibrary !== libraryId) return;
                      if (!result || result.error) throw Error(result?.error || 'Invalid gallery page response');
                      if (!Array.isArray(result.items) || result.items.length > 15)
                        throw Error('MediaServices did not return a bounded fifteen-item page.');
                      pageIndex = requested;
                      pageData = result;
                      setStatus(result.items.length
                        ? 'Showing page ' + pageIndex + ' · images load directly from signed storage URLs'
                        : 'This page has no Media Resources.');
                    } catch (error) {
                      if (generation === loadGeneration) setStatus('Could not load library page: '+errorText(error));
                    } finally {
                      if (generation === loadGeneration) {loading=false;render();}
                    }
                  };
                  const chooseLibrary = id => {
                    if (!id) return;
                    libraryId=id;
                    pageIndex=1;
                    pageData={items:[],hasMore:false};
                    resetSelection();
                    render();
                    loadPage(1);
                  };
                  const showLibraries = data => {
                    if (!data || !Array.isArray(data.libraries)) return;
                    openData=data;
                    libraries.replaceChildren();
                    const options=data.libraries;
                    for(const library of options) {
                      const option=make('option',null,library.name||library.id);
                      option.value=library.id;
                      libraries.appendChild(option);
                    }
                    libraries.disabled=!options.length;
                    el('libraryCount').textContent=options.length+' librar'+(options.length===1?'y':'ies');
                    if (!options.length) {setStatus('No accessible Media Libraries found.');return;}
                    const selected=options.some(x=>x.id===data.initialLibraryId)
                      ? data.initialLibraryId : options[0].id;
                    libraries.value=selected;
                    if (connected && !libraryId) chooseLibrary(selected);
                  };
                  libraries.addEventListener('change',()=>{
                    // A library switch invalidates previously requested paging data.
                    ++loadGeneration;
                    loading=false;
                    chooseLibrary(libraries.value);
                  });
                  previous.addEventListener('click',()=>loadPage(Math.max(1,pageIndex-1)));
                  next.addEventListener('click',()=>loadPage(pageIndex+1));
                  const receiveOpen = data => {
                    const parsed=unwrap(data);
                    if (parsed?.libraries) showLibraries(parsed);
                  };
                  window.addEventListener('message',event=>{
                    if(event.source!==window.parent||!event.data||event.data.jsonrpc!=='2.0')return;
                    const msg=event.data;
                    if(msg.id===1){
                      if(msg.error){setStatus('MCP Apps initialization rejected: '+errorText(msg.error));return;}
                      if(msg.result) {
                        window.parent.postMessage({
                          jsonrpc:'2.0',method:'ui/notifications/initialized',params:{}
                        },'*');
                        connected=true;
                        setStatus('Connected to ChatGPT MCP Apps.');
                        if(window.openai?.toolOutput) receiveOpen(window.openai.toolOutput);
                        if(openData && !libraryId) chooseLibrary(openData.initialLibraryId || openData.libraries[0]?.id);
                      }
                      return;
                    }
                    if(pending.has(msg.id)){
                      const req=pending.get(msg.id);
                      pending.delete(msg.id);
                      if(msg.error)req.reject(Error(errorText(msg.error)));
                      else req.resolve(msg.result);
                      return;
                    }
                    if(msg.method==='ui/notifications/tool-result') receiveOpen(msg.params);
                  });
                  if(window.openai?.toolOutput) receiveOpen(window.openai.toolOutput);
                  window.parent.postMessage({
                    jsonrpc:'2.0',id:1,method:'ui/initialize',
                    params:{
                      protocolVersion:'2026-01-26',
                      appInfo:{name:'aptix-media-library',version:'6.0.0'},
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

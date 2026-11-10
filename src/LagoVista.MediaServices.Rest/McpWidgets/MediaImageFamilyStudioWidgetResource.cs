using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

namespace LagoVista.MediaServices.Rest.McpWidgets
{
    /// <summary>
    /// Persistent Image Family creative workbench. The ImageFamily record, not
    /// widget state, is authoritative. Family create/get/update and the existing
    /// authorized Media Gallery tools provide all data operations.
    /// </summary>
    [McpServerResourceType]
    public static class MediaImageFamilyStudioWidgetResource
    {
        public const string ResourceUri = "ui://media/image-family-studio/v1.html";
        public const string AppMimeType = "text/html;profile=mcp-app";

        [McpServerResource(UriTemplate = ResourceUri, Name = "APTIX Image Family Studio", MimeType = AppMimeType)]
        public static TextResourceContents ReadStudio()
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
              <title>APTIX Image Family Studio</title>
              <style>
                :root{color-scheme:light dark;font-family:system-ui,-apple-system,Segoe UI,sans-serif}
                *{box-sizing:border-box} body{margin:0;padding:15px;background:transparent;color:inherit}
                .shell{max-width:870px;margin:0 auto} h2{margin:5px 0;font-size:20px}
                h3{font-size:15px;margin:0 0 10px} p{line-height:1.5}
                .eyebrow{font-size:11px;text-transform:uppercase;letter-spacing:.075em;opacity:.65}
                .muted{font-size:12px;opacity:.75;line-height:1.5}
                .panel{border:1px solid rgba(128,128,128,.35);border-radius:12px;padding:15px;margin:12px 0}
                label{display:block;font-size:12px;font-weight:700;margin:0 0 6px}
                input,textarea,select,button{font:inherit}
                input[type=text],textarea,select{width:100%;min-width:0;padding:9px 10px;border:1px solid rgba(128,128,128,.45);border-radius:8px;background:transparent;color:inherit;font-size:13px}
                select option{color:#202020;background:#fff} textarea{min-height:70px;resize:vertical;line-height:1.45}
                textarea.long{min-height:95px}
                .field{margin:0 0 12px}.layout{display:grid;grid-template-columns:1fr 1fr;gap:12px}
                .row{display:flex;gap:10px;align-items:center;flex-wrap:wrap}
                .row > .stretch{flex:1;min-width:180px}
                button{border-radius:8px;border:1px solid rgba(128,128,128,.45);background:transparent;color:inherit;padding:9px 12px;cursor:pointer;font-size:12px;font-weight:600}
                button.primary{background:#0969b6;color:#fff;border-color:#0969b6}
                button:disabled{opacity:.45;cursor:default}
                button:focus-visible,input:focus-visible,textarea:focus-visible,select:focus-visible{outline:2px solid #0094e7;outline-offset:2px}
                .switch{display:flex;align-items:center;gap:8px;font-size:12px;margin:0 0 10px}
                .switch label{margin:0}
                .grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px}
                .tile{display:flex;flex-direction:column;align-items:stretch;text-align:left;min-width:0;padding:0;overflow:hidden}
                .preview{height:100px;background:rgba(128,128,128,.13);display:flex;align-items:center;justify-content:center}
                .preview img{width:100%;height:100%;object-fit:cover}
                .tile .caption{padding:8px;font-size:11px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
                .selected-images{display:flex;flex-direction:column;gap:7px;padding:0;margin:7px 0 16px;list-style:none}
                .reference{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:8px 10px;border:1px solid rgba(128,128,128,.25);border-radius:8px}
                .reference span{font-size:12px;overflow-wrap:anywhere}
                .reference button{padding:5px 9px}
                .message{font-size:12px;line-height:1.45;min-height:21px;overflow-wrap:anywhere}
                .guide{margin:0 0 12px} .foot{margin-top:15px;border-top:1px solid rgba(128,128,128,.25);padding-top:12px}
                #fallback{min-height:105px}
                [hidden]{display:none!important}
                @media(max-width:560px){.layout{grid-template-columns:1fr}.grid{grid-template-columns:repeat(2,minmax(0,1fr))}}
              </style>
            </head>
            <body>
            <main class="shell">
              <div class="eyebrow">APTIX · MediaServices · Studio v1</div>
              <h2>Image Family Studio</h2>
              <p class="muted">Create a reusable visual family or reopen one to improve its guidance and approved references. Changes are saved to the Image Family record, not a separate session.</p>
            
              <section class="panel" aria-label="Choose image family">
                <div class="row">
                  <div class="stretch">
                    <label for="familyChooser">Existing Image Families</label>
                    <select id="familyChooser" disabled><option value="">Loading...</option></select>
                  </div>
                  <button id="newFamily" type="button" disabled>+ New family</button>
                </div>
                <p class="muted" id="mode">Loading Studio...</p>
              </section>
            
              <section class="panel" aria-label="Family details">
                <h3>Family definition</h3>
                <div class="layout">
                  <div class="field"><label for="name">Name</label><input id="name" type="text" maxlength="160" placeholder="AppTrex Marketing"></div>
                  <div class="field"><label for="key">Key</label><input id="key" type="text" maxlength="100" placeholder="apptrex-marketing"></div>
                </div>
                <div class="field"><label for="description">Purpose and description</label><textarea id="description" placeholder="What this family is for, and when to use it"></textarea></div>
                <div class="switch"><input id="isActive" type="checkbox" checked><label for="isActive">Active · available for image creation</label></div>
                <div class="field"><label for="styleGuidance">Base visual style (required)</label><textarea id="styleGuidance" class="long" placeholder="Art direction, consistent visual language, and subject treatment"></textarea></div>
                <div class="layout">
                  <div class="field"><label for="paletteGuidance">Palette</label><textarea id="paletteGuidance"></textarea></div>
                  <div class="field"><label for="renderingGuidance">Rendering</label><textarea id="renderingGuidance"></textarea></div>
                  <div class="field"><label for="moodGuidance">Mood and lighting</label><textarea id="moodGuidance"></textarea></div>
                  <div class="field"><label for="compositionGuidance">Composition</label><textarea id="compositionGuidance"></textarea></div>
                </div>
                <div class="layout">
                  <div class="field"><label for="usageGuidance">Use cases</label><textarea id="usageGuidance"></textarea></div>
                  <div class="field"><label for="avoidGuidance">Avoid</label><textarea id="avoidGuidance"></textarea></div>
                </div>
                <div class="row">
                  <button id="save" class="primary" type="button" disabled>Save Image Family</button>
                  <span id="saveStatus" class="message" role="status"></span>
                </div>
              </section>
            
              <section class="panel" aria-label="Reference images">
                <h3>Reference images <span id="referenceCount" class="muted"></span></h3>
                <p class="muted">Select existing, authorized Media Resources as examples. Add or remove them, then Save Image Family. This does not attach image pixels to ChatGPT automatically.</p>
                <ul id="selectedImages" class="selected-images"></ul>
                <div class="row">
                  <div class="stretch"><label for="library">Browse Media Library</label><select id="library" disabled><option value="">Loading...</option></select></div>
                </div>
                <p class="muted">Click an image thumbnail to add it as a reference.</p>
                <div id="gallery" class="grid" aria-live="polite"></div>
                <div class="row" style="margin-top:10px">
                  <button id="previous" type="button" disabled>Previous</button>
                  <span id="page" class="muted">Page 1</span>
                  <button id="next" type="button" disabled>Next</button>
                  <span id="galleryStatus" class="message" role="status"></span>
                </div>
              </section>
            
              <section class="panel" aria-label="Creative assistance">
                <h3>Work with ChatGPT</h3>
                <p class="muted">Once the family is saved, you can request a new candidate image from its guidance. ChatGPT generates and reviews images in the conversation; only approved images should be imported into MediaServices and then added as references here.</p>
                <div class="row">
                  <button id="askGuidance" type="button" disabled>Help draft guidance</button>
                  <button id="generate" type="button" disabled>Generate an example</button>
                </div>
                <div id="manual" hidden>
                  <label for="fallback">Copy this request into ChatGPT:</label>
                  <textarea id="fallback" readonly></textarea>
                </div>
              </section>
            
              <div id="status" class="message" role="status">Connecting to ChatGPT MCP Apps...</div>
            </main>
            <script>
            (() => {
              const el = id => document.getElementById(id);
              const editable = ['name','key','description','styleGuidance','paletteGuidance',
                'renderingGuidance','moodGuidance','compositionGuidance','usageGuidance','avoidGuidance'];
              const guidance = editable.filter(x => x !== 'name' && x !== 'key' && x !== 'description');
              let initialized = false, connected = false, busy = false, dirty = false, isNew = true;
              let familyModel = null, snapshot = null, references = [], families = [], libraries = [];
              let libraryId = null, galleryPage = 1, galleryLoading = false, galleryGeneration = 0;
              let prefill = null, currentId = null, pageData = null, requestCounter = 30;
              const pending = new Map();
              const permittedHosts = new Set(['storage-dev.nuviot.com','storage.nuviot.com']);
              const clone = v => JSON.parse(JSON.stringify(v));
              const make = (tag, className, label) => {
                const n = document.createElement(tag);
                if(className) n.className = className;
                if(label !== undefined) n.textContent = String(label);
                return n;
              };
              const errorText = v => typeof v === 'string' ? v : (v?.errorMessage || v?.message ||
                (Array.isArray(v?.errors) && v.errors[0]?.message) || 'MediaServices operation failed');
              const unpack = input => {
                const queue = [input];
                let attempts = 0;
                while(queue.length && attempts++ < 60) {
                  const v = queue.shift();
                  if(v == null) continue;
                  if(typeof v === 'string') {try{queue.push(JSON.parse(v))}catch(_){ } continue;}
                  if(Array.isArray(v)) {queue.push(...v); continue;}
                  if(typeof v !== 'object') continue;
                  if(v.successful === false || v.isError === true) return {error:errorText(v)};
                  if(v.family && Array.isArray(v.families) && Array.isArray(v.libraries)) return v;
                  if(v.model && (Array.isArray(v.model) || typeof v.model === 'object')) return v;
                  if(Array.isArray(v.items) || Array.isArray(v.libraries)) return v;
                  if(v.result && typeof v.result === 'object') queue.unshift(v.result);
                  if(v.successful === true && v.result == null && v.model == null) return v;
                  for(const k of ['structuredContent','content','text']) if(v[k] != null) queue.push(v[k]);
                }
                return null;
              };
              const request = (name, args) => new Promise((resolve,reject) => {
                if(!connected) {reject(Error('MCP App is not connected yet'));return;}
                const id = ++requestCounter;
                pending.set(id,{resolve,reject});
                window.parent.postMessage({jsonrpc:'2.0',id,method:'tools/call',
                  params:{name,arguments:args}},'*');
              });
              const tool = async (name,args) => {
                const data = unpack(await request(name,args));
                if(!data || data.error) throw Error(data?.error || 'Unexpected '+name+' response');
                return data;
              };
              const setStatus = msg => {el('status').textContent=msg;};
              const showSaveStatus = msg => {el('saveStatus').textContent=msg;};
              const token = m => JSON.stringify([m?._etag ?? null,m?.revision ?? null,m?.lastUpdatedDate ?? null]);
              const safeUrl = value => {
                if(typeof value !== 'string') return null;
                try {
                  const u = new URL(value);
                  return u.protocol === 'https:' && permittedHosts.has(u.hostname) ? u.href : null;
                }catch(_){return null;}
              };
              const publishState = () => {
                if(!window.openai?.setWidgetState || !familyModel) return;
                Promise.resolve(window.openai.setWidgetState({
                  modelContent:{
                    action:'aptix_image_family_studio',
                    imageFamilyId:isNew ? null : currentId,
                    imageFamilyName:el('name').value.trim(),
                    isSaved:!isNew,
                    referenceImageIds:references.map(r=>r.id || r.Id),
                    instruction:'The Studio stores visual guidance and Media Resource references. A widget preview or Resource ID does not attach image pixels for generation. Do not save candidate generated images without user approval.'
                  },
                  privateContent:{imageFamilyId:isNew ? null : currentId}
                })).catch(()=>{});
              };
              const setBusy = value => {
                busy=value;
                el('save').disabled=value || !familyModel;
                el('newFamily').disabled=value || !connected;
                el('familyChooser').disabled=value || !connected;
                el('generate').disabled=value || isNew || !connected;
                el('askGuidance').disabled=value || !familyModel || !connected;
              };
              const renderChooser = () => {
                const select=el('familyChooser');
                select.replaceChildren();
                const option=make('option',null,'+ New Image Family');
                option.value='';select.appendChild(option);
                for(const family of families) {
                  const item=make('option',null,family.name || family.id);
                  item.value=family.id;select.appendChild(item);
                }
                select.value=isNew ? '' : currentId;
              };
              const renderReferences = () => {
                const box=el('selectedImages');box.replaceChildren();
                el('referenceCount').textContent='('+references.length+')';
                if(!references.length)box.appendChild(make('li','muted','No reference images selected yet.'));
                references.forEach((ref,i)=>{
                  const item=make('li','reference');
                  item.appendChild(make('span',null,ref.text || ref.Text || ref.id || ref.Id || 'Media Resource'));
                  const remove=make('button',null,'Remove');
                  remove.type='button';remove.disabled=busy;
                  remove.addEventListener('click',()=>{
                    references.splice(i,1);dirty=true;renderReferences();renderGallery();
                    showSaveStatus('Unsaved reference changes. Choose Save Image Family.');
                    publishState();
                  });
                  item.appendChild(remove);box.appendChild(item);
                });
              };
              const populate = (model, saved) => {
                if(!model || !model.id) throw Error('Image Family model or identifier was missing.');
                familyModel=clone(model);snapshot=clone(model);
                isNew=!saved;currentId=model.id;
                references=clone(Array.isArray(model.referenceImages)?model.referenceImages:[]);
                for(const f of editable) el(f).value=typeof model[f]==='string' ? model[f] : '';
                el('isActive').checked=model.isActive!==false;
                dirty=false;showSaveStatus('');
                el('mode').textContent=(isNew?'New unsaved family':'Editing saved family')+
                  ' · ID: '+currentId;
                el('save').textContent=isNew?'Create Image Family':'Save Changes';
                renderChooser();renderReferences();renderGallery();setBusy(false);publishState();
              };
              const chooseFamily = async id => {
                if(busy)return;
                if(dirty && !window.confirm('Discard unsaved Image Family edits?')) {
                  renderChooser();return;
                }
                setBusy(true);
                try{
                  if(!id) {
                    const response=await tool('media-image-families-factory',{});
                    if(!response.model)throw Error('Factory did not return a complete Image Family');
                    populate(response.model,false);
                  }else{
                    const response=await tool('media-image-families-get',{id});
                    if(!response.model)throw Error('GET did not return a complete Image Family');
                    populate(response.model,true);
                  }
                  setStatus('Image Family ready for editing.');
                }catch(e){setStatus('Could not open Image Family: '+errorText(e));renderChooser();}
                finally{setBusy(false);}
              };
              const readValues = () => {
                const values={};
                for(const f of editable)values[f]=el(f).value.trim();
                values.isActive=el('isActive').checked;
                values.referenceImages=clone(references);
                if(!values.name)throw Error('Enter an Image Family name.');
                if(!values.key)throw Error('Enter a unique Image Family key.');
                if(!values.styleGuidance)throw Error('Base visual style is required.');
                return values;
              };
              const updateChooser = model => {
                const idx=families.findIndex(x=>x.id===model.id);
                const item={id:model.id,name:model.name,key:model.key};
                if(idx<0)families.unshift(item);else families[idx]=item;
                renderChooser();
              };
              const save = async () => {
                if(busy || !familyModel)return;
                let edits;
                try{edits=readValues()}catch(e){showSaveStatus(errorText(e));return;}
                setBusy(true);showSaveStatus('Saving and verifying Image Family...');
                try{
                  let source=familyModel;
                  if(!isNew){
                    const response=await tool('media-image-families-get',{id:currentId});
                    if(!response.model || response.model.id!==currentId)
                      throw Error('Could not retrieve the complete existing Image Family.');
                    // Refuse to overwrite another editor's changes with a stale snapshot.
                    if(token(response.model)!==token(snapshot))
                      throw Error('Image Family was changed elsewhere. Reopen it before saving.');
                    source=response.model;
                  }
                  const record=clone(source);
                  for(const field of Object.keys(edits))record[field]=edits[field];
                  const operation=isNew?'media-image-families-create':'media-image-families-update';
                  const response=await tool(operation,{imageFamily:record});
                  if(response.successful===false)throw Error(errorText(response));
                  // A successful mutation is not proof until the complete record is readable.
                  const verified=await tool('media-image-families-get',{id:record.id});
                  if(!verified.model || verified.model.id!==record.id)
                    throw Error('Save was accepted, but the saved Image Family could not be verified. Reopen to check.');
                  const refs=verified.model.referenceImages || [];
                  if(refs.length!==record.referenceImages.length)
                    throw Error('The saved Image Family did not retain every reference image.');
                  populate(verified.model,true);
                  updateChooser(verified.model);
                  showSaveStatus('Saved and verified, including '+refs.length+' reference image(s).');
                  setStatus('Image Family saved. You can reopen it in another session.');
                }catch(e){
                  showSaveStatus('Save not confirmed: '+errorText(e));
                  setStatus('Review the message beside Save Image Family.');
                }finally{setBusy(false);}
              };
              const addReference = item => {
                if(busy || !familyModel || item.kind!=='image')return;
                if(!item.revisionId){el('galleryStatus').textContent='This image has no saved revision.';return;}
                if(references.some(r=>(r.id||r.Id)===item.id)){
                  el('galleryStatus').textContent='This image is already a reference.';return;
                }
                references.push({id:item.id,text:item.name||item.fileName||'Media Resource'});
                dirty=true;renderReferences();renderGallery();
                el('galleryStatus').textContent='Added '+(item.name||'image')+'. Save the family to persist it.';
                showSaveStatus('Unsaved reference changes.');
                publishState();
              };
              const renderGallery = () => {
                const gallery=el('gallery');gallery.replaceChildren();
                const items=Array.isArray(pageData?.items)
                  ?pageData.items.filter(x=>x.kind==='image'):[];
                if(!items.length)gallery.appendChild(make('p','muted',
                  pageData?'No images on this page. Try another library or page.':'Choose a Media Library to browse images.'));
                for(const item of items){
                  const already=references.some(x=>(x.id||x.Id)===item.id);
                  const tile=make('button','tile');
                  tile.type='button';tile.disabled=busy || already;
                  tile.title=already?'Already a reference':'Add '+(item.name||'image');
                  const preview=make('div','preview');
                  const url=safeUrl(item.previewUrl);
                  if(url) {
                    const img=make('img');img.src=url;img.loading='lazy';img.alt=item.name||'Media Resource';
                    img.addEventListener('error',()=>preview.replaceChildren(make('span',null,'Image')));
                    preview.appendChild(img);
                  }else preview.appendChild(make('span',null,'Image'));
                  tile.append(preview,make('div','caption',(already?'✓ ':'+ ')+(item.name||item.fileName||'Untitled image')));
                  tile.addEventListener('click',()=>addReference(item));
                  gallery.appendChild(tile);
                }
                el('previous').disabled=galleryLoading || galleryPage<=1;
                el('next').disabled=galleryLoading || !pageData?.hasMore;
                el('page').textContent='Page '+galleryPage;
              };
              const loadGallery = async page => {
                if(!libraryId || galleryLoading || !connected)return;
                galleryLoading=true;
                const generation=++galleryGeneration;
                const lib=libraryId;
                el('previous').disabled=el('next').disabled=true;
                el('galleryStatus').textContent='Loading authorized Media Resources...';
                try{
                  const data=await tool('media-gallery-page',{libraryid:lib,page});
                  if(generation!==galleryGeneration || lib!==libraryId)return;
                  if(!Array.isArray(data.items) || data.items.length>15)
                    throw Error('Invalid gallery page returned by MediaServices.');
                  galleryPage=page;pageData=data;
                  el('galleryStatus').textContent=data.items.length+' item(s); click images to select.';
                }catch(e){if(generation===galleryGeneration)el('galleryStatus').textContent=errorText(e);}
                finally{if(generation===galleryGeneration){galleryLoading=false;renderGallery();}}
              };
              const showLibraries = () => {
                const select=el('library');select.replaceChildren();
                for(const library of libraries){
                  const option=make('option',null,library.name||library.id);option.value=library.id;
                  select.appendChild(option);
                }
                select.disabled=!libraries.length;
                if(libraries.length){
                  const initial=libraries.find(x=>(x.name||'').toLowerCase().includes('marketing'))||libraries[0];
                  libraryId=initial.id;select.value=libraryId;
                  if(connected)loadGallery(1);
                }else{
                  libraryId=null;el('galleryStatus').textContent='No accessible Media Libraries.';
                }
              };
              const receiveOpen = raw => {
                const data=unpack(raw);
                if(initialized || !data || !data.family || !Array.isArray(data.families)
                   || !Array.isArray(data.libraries))return;
                initialized=true;prefill=data;
                families=data.families.filter(x=>x?.id && x?.name);
                libraries=data.libraries.filter(x=>x?.id && x?.name);
                if(data.initialFamilyId && !families.some(x=>x.id===data.initialFamilyId))
                  families.unshift({id:data.initialFamilyId,name:data.family.name||data.initialFamilyId,key:data.family.key});
                populate(data.family,!!data.initialFamilyId);
                showLibraries();
                setStatus('Studio ready. Edit guidance, choose reference images, and save.');
              };
              const send = async prompt => {
                if(!window.openai?.sendFollowUpMessage){
                  el('fallback').value=prompt;el('manual').hidden=false;
                  setStatus('Copy the request from the box into your ChatGPT message.');return;
                }
                el('manual').hidden=true;
                publishState();
                try{
                  await window.openai.sendFollowUpMessage({prompt});
                  setStatus('Request sent to ChatGPT. Continue in the conversation.');
                }catch(e){
                  el('fallback').value=prompt;el('manual').hidden=false;
                  setStatus('Could not send request: '+errorText(e)+'. Copy it into chat.');
                }
              };
              el('newFamily').addEventListener('click',()=>chooseFamily(''));
              el('familyChooser').addEventListener('change',()=>chooseFamily(el('familyChooser').value));
              el('save').addEventListener('click',save);
              el('library').addEventListener('change',()=>{
                ++galleryGeneration;galleryLoading=false;
                libraryId=el('library').value;galleryPage=1;pageData=null;
                renderGallery();loadGallery(1);
              });
              el('previous').addEventListener('click',()=>loadGallery(Math.max(1,galleryPage-1)));
              el('next').addEventListener('click',()=>loadGallery(galleryPage+1));
              for(const id of editable)el(id).addEventListener('input',()=>{dirty=true;showSaveStatus('Unsaved edits.');});
              el('isActive').addEventListener('change',()=>{dirty=true;showSaveStatus('Unsaved edits.');});
              el('name').addEventListener('input',()=>{
                if(isNew && !el('key').value.trim())el('key').value=el('name').value.trim()
                  .toLowerCase().replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'');
              });
              el('askGuidance').addEventListener('click',()=>{
                const lines=['Help me improve the visual guidance for an APTIX Image Family.',
                  'Family name: '+(el('name').value||'(not named)'),
                  'Purpose: '+el('description').value];
                for(const f of guidance)lines.push(f+': '+el(f).value);
                lines.push('Suggest complete, distinct field values with a consistent brand direction.',
                  'Do not save or update any ImageFamily record without explicit approval. Reference images are not attached as pixels.');
                send(lines.join('\n'));
              });
              el('generate').addEventListener('click',()=>{
                if(isNew || dirty){setStatus('Save your Image Family changes before generating an example.');return;}
                send([
                  'Create ONE new draft example image for the saved APTIX Image Family '+(el('name').value||currentId)+
                    ' (id '+currentId+').',
                  'First use media-image-families-get with this exact ID to retrieve the latest visual guidance.',
                  'Use the family style, palette, rendering, mood, composition, use-case and avoid guidance.',
                  'Ask me which subject or use case to depict if needed, then generate a draft image using ChatGPT image generation.',
                  'Existing referenceImage headers are identifiers, not actual image pixels. Do not claim to have seen their images.',
                  'Show the draft in chat for review. DO NOT SAVE until I explicitly approve.',
                  'After approval, use media-import-chatgpt-image to create a NEW Media Resource, verify it, and then ask me to reopen this Studio to attach the saved resource as a reference.'
                ].join('\n'));
              });
              window.addEventListener('message',event=>{
                if(event.source!==window.parent || !event.data || event.data.jsonrpc!=='2.0')return;
                const msg=event.data;
                if(msg.id===1){
                  if(msg.error){setStatus('MCP App initialization rejected: '+errorText(msg.error));return;}
                  if(msg.result){
                    window.parent.postMessage({jsonrpc:'2.0',method:'ui/notifications/initialized',params:{}},'*');
                    connected=true;setStatus('Connected to APTIX MediaServices.');
                    if(window.openai?.toolOutput)receiveOpen(window.openai.toolOutput);
                    if(prefill){showLibraries();setBusy(false);}
                  }
                  return;
                }
                if(pending.has(msg.id)){
                  const p=pending.get(msg.id);pending.delete(msg.id);
                  if(msg.error)p.reject(Error(errorText(msg.error)));
                  else p.resolve(msg.result);
                  return;
                }
                if(msg.method==='ui/notifications/tool-result')receiveOpen(msg.params);
              });
              if(window.openai?.toolOutput)receiveOpen(window.openai.toolOutput);
              window.parent.postMessage({
                jsonrpc:'2.0',id:1,method:'ui/initialize',
                params:{protocolVersion:'2026-01-26',appInfo:{name:'aptix-image-family-studio',version:'1.0.0'},appCapabilities:{}}
              },'*');
            })();
            </script>
            </body>
            </html>
            """;
    }
}

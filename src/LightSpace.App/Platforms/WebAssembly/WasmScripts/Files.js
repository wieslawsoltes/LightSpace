(() => {
  'use strict';
  let dirty = false;
  let diagnosticSequence = 0;
  addEventListener('beforeunload', event => { if (dirty) { event.preventDefault(); event.returnValue = ''; } });
  const diagnostics = new URLSearchParams(location.search).has('diagnostics');
  const database = () => globalThis.lightSpaceRecovery.open();
  async function pick(accept, multiple, limit) {
    return await new Promise((resolve, reject) => {
      const input = document.createElement('input'); input.type = 'file'; input.accept = accept; input.multiple = multiple;
      input.style.cssText = 'position:fixed;left:-10000px;top:0'; document.body.append(input);
      let finished = false;
      const finish = (value, error) => { if (finished) return; finished = true; input.remove(); error ? reject(error) : resolve(value); };
      input.addEventListener('cancel', () => finish('[]'), { once: true });
      input.addEventListener('change', async () => {
        try {
          const files = Array.from(input.files || []); const total = files.reduce((n, f) => n + f.size, 0);
          if (files.some(f => f.size > limit) || total > (multiple ? 256 : 360) * 1024 * 1024) throw new Error('Selected files exceed the import size limit.');
          const result = [];
          for (const file of files) {
            const base64 = await new Promise((ok, bad) => { const reader = new FileReader(); reader.onload = () => ok(String(reader.result).split(',')[1]); reader.onerror = () => bad(reader.error); reader.readAsDataURL(file); });
            result.push({ name: file.name, base64 });
          }
          finish(JSON.stringify(result));
        } catch (error) { finish('', error); }
      }, { once: true });
      input.click();
    });
  }
  globalThis.lightSpaceFiles = {
    setDirty: value => { dirty = value; },
    openImages: () => pick('.jpg,.jpeg,.png,.webp,.bmp,.gif', true, 64 * 1024 * 1024),
    openCatalog: () => pick('.lightspace,.json', false, 360 * 1024 * 1024),
    openSidecar: () => pick('.xmp', false, 16 * 1024 * 1024),
    download: async (name, base64, type) => {
      const raw = atob(base64); const bytes = new Uint8Array(raw.length);
      for (let i = 0; i < raw.length; i++) bytes[i] = raw.charCodeAt(i);
      const url = URL.createObjectURL(new Blob([bytes], { type })); const link = document.createElement('a'); link.href = url; link.download = name; document.body.append(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 30000); return 'ok';
    },
    // Portable hydration is for legacy consumers and diagnostics. Normal startup reads the manifest and source blobs separately.
    load: () => globalThis.lightSpaceRecovery.loadCompatible(),
    save: async json => {
      const db = await database();
      return await new Promise((resolve, reject) => { const tx = db.transaction('workspace', 'readwrite'); tx.objectStore('workspace').put(json, 'catalog'); tx.objectStore('workspace').delete('manifest-v1'); tx.oncomplete = () => { db.close(); resolve('ok'); }; tx.onerror = tx.onabort = () => { db.close(); reject(tx.error || new Error('Recovery write failed.')); }; });
    },
    publishDiagnostics: json => {
      globalThis.lightSpaceReady = true;
      if (diagnostics) {
        // A repeated read of one snapshot is not evidence that layout is stable.
        const snapshot = JSON.parse(json);
        snapshot.diagnosticSequence = ++diagnosticSequence;
        globalThis.lightSpaceDiagnostics = snapshot;
      }
    },
    startupError: error => { globalThis.lightSpaceStartupError = error; },
    focusCanvasUnlessEditing: () => { const element = document.activeElement; if (element && (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA' || element.isContentEditable)) return; const canvas = document.querySelector('canvas'); if (canvas) { canvas.tabIndex = 0; canvas.focus({ preventScroll: true }); } }
  };
  addEventListener('keydown', event => {
    if (!(event.ctrlKey || event.metaKey) || event.altKey) return;
    const element = document.activeElement;
    if (element && (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA' || element.isContentEditable)) return;
    if (['s', 'i', 'e', 'z', 'y', 'a'].includes(event.key.toLowerCase())) event.preventDefault();
  });
})();

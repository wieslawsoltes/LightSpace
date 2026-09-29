(() => {
  'use strict';
  const MAX_SOURCE = 64 * 1024 * 1024, MAX_TOTAL = 256 * 1024 * 1024;
  const MAX_CATALOG_SCHEMA = 5;
  const validKey = key => typeof key === 'string' && /^[a-f0-9]{64}$/.test(key);
  const validId = id => typeof id === 'string' && /^[a-f0-9]{8}(-[a-f0-9]{4}){3}-[a-f0-9]{12}$/i.test(id);
  const stats = { commits: 0, blobWrites: 0, blobBytes: 0, blobReads: 0, referencesChecked: 0, manifestBytes: 0, durability: 'unrequested' };
  function open() {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open('LightSpace-v1', 2); let rejected = false;
      const fail = error => { rejected = true; reject(error); };
      request.onupgradeneeded = () => {
        for (const name of ['workspace', 'originals'])
          if (!request.result.objectStoreNames.contains(name)) request.result.createObjectStore(name);
      };
      request.onsuccess = () => {
        const db = request.result;
        if (rejected) { db.close(); return; }
        db.onversionchange = () => db.close(); resolve(db);
      };
      request.onerror = () => fail(request.error || new Error('Browser storage is unavailable.'));
      request.onblocked = () => fail(new Error('Close other LightSpace tabs to update recovery storage.'));
    });
  }
  async function read(store, key) {
    const db = await open();
    return new Promise((resolve, reject) => {
      try {
        const tx = db.transaction(store, 'readonly'); let value;
        tx.oncomplete = () => { db.close(); resolve(value); };
        tx.onabort = () => { db.close(); reject(tx.error || new Error('Recovery read failed.')); };
        tx.objectStore(store).get(key).onsuccess = event => { value = event.target.result; };
      } catch (error) { db.close(); reject(error); }
    });
  }
  function parseManifest(json) {
    if (typeof json !== 'string' || json.length > MAX_SOURCE) throw new Error('Recovery manifest exceeds the safety limit.');
    const manifestBytes = new TextEncoder().encode(json).byteLength;
    if (manifestBytes > MAX_SOURCE) throw new Error('Recovery manifest exceeds the safety limit.');
    const value = JSON.parse(json);
    if (!value || value.Format !== 'LightSpace.Recovery' || value.Version !== 1 || !Number.isSafeInteger(value.Revision) || value.Revision < 0
        || !value.Catalog || !Number.isInteger(value.Catalog.SchemaVersion) || value.Catalog.SchemaVersion < 1 || value.Catalog.SchemaVersion > MAX_CATALOG_SCHEMA
        || !value.Sources || typeof value.Sources !== 'object' || Array.isArray(value.Sources)
        || !Array.isArray(value.Catalog.Photos) || value.Catalog.Photos.length > 5000)
      throw new Error('Unsupported recovery manifest.');
    const sources = Object.values(value.Sources); let total = 0;
    const keys = new Set(), ids = new Set(), lengths = new Map();
    if (sources.length !== value.Catalog.Photos.length) throw new Error('Invalid recovery source mapping.');
    for (const photo of value.Catalog.Photos) {
      if (!photo || !validId(photo.Id) || ids.has(photo.Id.toLowerCase()) || !Object.hasOwn(value.Sources, photo.Id))
        throw new Error('Missing or duplicate recovery photo identity.');
      ids.add(photo.Id.toLowerCase());
      const source = value.Sources[photo.Id];
      if (!source || !validKey(source.Key) || !Number.isInteger(source.Length) || source.Length < 1 || source.Length > MAX_SOURCE || photo.Original !== ''
          || !Number.isInteger(photo.Width) || !Number.isInteger(photo.Height) || photo.Width < 1 || photo.Height < 1 || photo.Width * photo.Height > 100000000)
        throw new Error('Invalid recovery source reference.');
      if (lengths.has(source.Key) && lengths.get(source.Key) !== source.Length) throw new Error('Conflicting recovery source lengths.');
      lengths.set(source.Key, source.Length);
      total += source.Length; if (total > MAX_TOTAL) throw new Error('Recovery originals exceed 256 MiB.');
      keys.add(source.Key);
    }
    return { value, keys, lengths, manifestBytes };
  }
  function fromBase64(text) {
    if (typeof text !== 'string' || text.length > Math.ceil(MAX_SOURCE / 3) * 4) throw new Error('Recovery source is too large.');
    const decoded = atob(text); const bytes = new Uint8Array(decoded.length);
    for (let i = 0; i < decoded.length; i++) bytes[i] = decoded.charCodeAt(i);
    return bytes;
  }
  function toBase64(bytes) {
    const chunks = [];
    for (let i = 0; i < bytes.length; i += 32768) chunks.push(String.fromCharCode(...bytes.subarray(i, i + 32768)));
    return btoa(chunks.join(''));
  }
  async function readBlob(key) {
    if (!validKey(key)) throw new Error('Invalid recovery key.');
    const blob = await read('originals', key);
    if (blob === undefined) return '';
    if (!(blob instanceof Blob) || blob.size < 1 || blob.size > MAX_SOURCE) throw new Error('Invalid recovery source.');
    stats.blobReads++;
    return toBase64(new Uint8Array(await blob.arrayBuffer()));
  }
  async function commit(manifest, referenceText, encodedSources) {
    const { keys, lengths, manifestBytes } = parseManifest(manifest);
    if (typeof referenceText !== 'string' || referenceText.length > 65 * 5000) throw new Error('Invalid recovery reference list.');
    const required = new Set(referenceText === '' ? [] : referenceText.split(','));
    if (required.size !== keys.size || [...required].some(key => !validKey(key) || !keys.has(key))) throw new Error('Recovery references do not match the manifest.');
    if (typeof encodedSources !== 'string' || encodedSources.length > MAX_TOTAL * 1.4) throw new Error('Recovery staging exceeds its safety limit.');
    const sources = JSON.parse(encodedSources);
    if (!Array.isArray(sources) || sources.length > 5000) throw new Error('Invalid recovery staging.');
    const staged = new Map(); let stagedBytes = 0;
    for (const source of sources) {
      if (!source || !validKey(source.key) || !required.has(source.key) || staged.has(source.key)) throw new Error('Invalid staged recovery key.');
      const bytes = fromBase64(source.data); stagedBytes += bytes.length;
      if (bytes.length !== lengths.get(source.key) || bytes.length < 1 || bytes.length > MAX_SOURCE || stagedBytes > MAX_TOTAL)
        throw new Error('Recovery source length does not match its manifest.');
      const digest = new Uint8Array(await crypto.subtle.digest('SHA-256', bytes));
      const key = [...digest].map(value => value.toString(16).padStart(2, '0')).join('');
      if (key !== source.key) throw new Error('Recovery source hash mismatch.');
      staged.set(key, new Blob([bytes]));
    }
    const db = await open();
    return new Promise((resolve, reject) => {
      let tx, failure;
      const abort = error => {
        failure ??= error instanceof Error ? error : new Error(String(error));
        try { tx.abort(); } catch { db.close(); reject(failure); }
      };
      try {
        tx = db.transaction(['workspace', 'originals'], 'readwrite', { durability: 'strict' });
        tx.oncomplete = () => {
          db.close(); stats.commits++; stats.blobWrites += staged.size; stats.blobBytes += stagedBytes;
          stats.referencesChecked += required.size; stats.manifestBytes += manifestBytes;
          stats.durability = tx.durability || 'default'; resolve('ok');
        };
        tx.onabort = () => { db.close(); reject(failure || tx.error || new Error('Recovery commit failed.')); };
        const originals = tx.objectStore('originals');
        for (const [key, blob] of staged) originals.put(blob, key);
        for (const key of required) originals.getKey(key).onsuccess = event => {
          if (event.target.result === undefined) abort(new Error('Recovery source is missing. Retry saving or export a catalog backup.'));
        };
        const workspace = tx.objectStore('workspace');
        workspace.put(manifest, 'manifest-v1'); workspace.delete('catalog');
      } catch (error) {
        // Synchronous failures must roll back any requests already enqueued.
        if (tx) abort(error); else { db.close(); reject(error); }
      }
    });
  }
  async function loadCompatible() {
    const manifest = await read('workspace', 'manifest-v1');
    if (manifest === undefined) return (await read('workspace', 'catalog')) || '';
    const { value } = parseManifest(manifest); const cache = new Map();
    for (const photo of value.Catalog.Photos) {
      const source = value.Sources[photo.Id];
      if (!cache.has(source.Key)) cache.set(source.Key, await readBlob(source.Key));
      photo.Original = cache.get(source.Key); if (!photo.Original) throw new Error('Recovery source is missing.');
    }
    return JSON.stringify(value.Catalog);
  }
  globalThis.lightSpaceRecovery = Object.freeze({
    open, readManifest: async () => (await read('workspace', 'manifest-v1')) ?? '', readBlob, commit, loadCompatible,
    statistics: () => ({ ...stats })
  });
})();

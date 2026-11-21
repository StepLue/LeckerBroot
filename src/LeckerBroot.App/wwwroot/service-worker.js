// Während der Entwicklung immer aus dem Netz laden – kein Offline-Cache.
// Die Offline-Version steht in service-worker.published.js und wird beim Veröffentlichen verwendet.
self.addEventListener('fetch', () => { });

// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', (event) => event.waitUntil(onInstall(event)));
self.addEventListener('activate', (event) => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', (event) => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

async function onInstall(event) {
    console.info('Service worker: install');
    self.skipWaiting();
    const assetsRequests = self.assetsManifest.assets
        .filter((asset) => offlineAssetsInclude.some((pattern) => pattern.test(asset.url)))
        .filter((asset) => !offlineAssetsExclude.some((pattern) => pattern.test(asset.url)))
        .map((asset) => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then((cache) => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: activate');
    self.clients.claim();
    const cacheKeys = await caches.keys();
    await Promise.all(
        cacheKeys
            .filter((key) => key.startsWith(cacheNamePrefix) && key !== cacheName)
            .map((key) => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        const cache = await caches.open(cacheName);
        // Garantir que vamos buscar a requisição precisamente
        // para evitar que assets estáticos (ex.: js) sejam servisidos por index.html
        cachedResponse = await cache.match(event.request);
        if (!cachedResponse) {
            // Se não encontrado pelo request direto, tentar normalizar o URL
            // (ex.: normalizar o path para o manifest)
            const normalizedURL = normalizeUrl(event.request.url);
            if (normalizedURL !== event.request.url) {
                cachedResponse = await cache.match(normalizedURL);
            }
        }
    }
    if (cachedResponse) {
        return cachedResponse;
    }
    try {
        return await fetch(event.request);
    } catch (error) {
        // Sem rede: devolve a casca do app para navegacoes, para a PWA abrir offline.
        if (event.request.mode === 'navigate') {
            const cache = await caches.open(cacheName);
            const fallback = await cache.match('index.html');
            if (fallback) {
                return fallback;
            }
        }
        throw error;
    }
}

function normalizeUrl(url) {
    // Tentar mapear o request.url para o asset do manifest se estiver em /_content/FitTrack.UI/
    // para corresponder com as entradas do manifest (ex.: /_content/FitTrack.UI/js/auto-finish-alarm.js)
    // Se o request.url está em /js/auto-finish-alarm.js mas manifest tem /_content/FitTrack.UI/js/auto-finish-alarm.js,
    // adicionar /_content/FitTrack.UI/
    const pattern = /\/js\/(auto-finish-alarm|avatar|combobox|rest-alarm)\.js$/;
    const match = url.match(pattern);
    if (match) {
        return `/_content/FitTrack.UI/js/${match[1]}.js`;
    }
    // Se for um caminho CSS /css/app.css, o manifest tem /_content/FitTrack.UI/css/app.css
    const cssPattern = /\/css\/app\.css$/;
    if (cssPattern.test(url)) {
        return `/_content/FitTrack.UI/css/app.css`;
    }
    // Outros assets estáticos que podem estar no manifest serão tratados pelo service worker-assets.js
    return url;
}
// Service worker de desenvolvimento. Em producao o SDK substitui este arquivo
// pela versao publicada (service-worker.published.js), que carrega o manifesto
// de assets e faz cache offline. Veja https://aka.ms/blazor-offline-considerations
const cacheName = 'fittrack-dev';

self.addEventListener('install', (event) => {
    console.log('Service worker (dev): install');
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    console.log('Service worker (dev): activate');
    self.clients.claim();
});

self.addEventListener('fetch', (event) => {
    // Em desenvolvimento deixa o navegador tratar todas as requisicoes.
});
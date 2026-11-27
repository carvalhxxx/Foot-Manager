// Service worker do Foot Manager BR: deixa o jogo funcionar offline depois do primeiro acesso.
// Estratégia "rede primeiro": online, sempre busca a versão nova (e guarda uma cópia);
// sem conexão, usa a cópia guardada. Os saves ficam no IndexedDB e não passam por aqui.

const CACHE = 'foot-manager-v1'

self.addEventListener('install', () => self.skipWaiting())

self.addEventListener('activate', (evento) => {
  evento.waitUntil(
    caches
      .keys()
      .then((nomes) => Promise.all(nomes.filter((n) => n !== CACHE).map((n) => caches.delete(n))))
      .then(() => self.clients.claim()),
  )
})

self.addEventListener('fetch', (evento) => {
  const pedido = evento.request
  if (pedido.method !== 'GET' || new URL(pedido.url).origin !== self.location.origin) {
    return
  }

  evento.respondWith(
    fetch(pedido)
      .then((resposta) => {
        if (resposta.ok) {
          const copia = resposta.clone()
          evento.waitUntil(caches.open(CACHE).then((cache) => cache.put(pedido, copia)))
        }
        return resposta
      })
      .catch(() => caches.match(pedido).then((guardada) => guardada ?? Response.error())),
  )
})

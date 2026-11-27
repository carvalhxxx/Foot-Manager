import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

// PWA: instalável e jogável offline depois do primeiro acesso. Só na versão publicada,
// para o cache não atrapalhar o desenvolvimento.
if (import.meta.env.PROD && 'serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    // Sem service worker o jogo funciona normalmente (só não abre offline); por isso o erro é ignorado.
    navigator.serviceWorker.register(`${import.meta.env.BASE_URL}sw.js`).catch(() => {})
  })
}

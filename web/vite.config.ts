import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  // No GitHub Pages o site fica em /<repositório>/; o CI define BASE_PATH.
  base: process.env.BASE_PATH ?? '/',
  plugins: [react()],
  build: {
    // O pedaço com as definições dos 40 clubes passa de 500 kB sem compressão (~80 kB com gzip).
    chunkSizeWarningLimit: 1000,
  },
  server: {
    // As definições do jogo ficam em ../dados, fora da pasta web.
    fs: { allow: ['..'] },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/testes/preparar.ts'],
  },
})

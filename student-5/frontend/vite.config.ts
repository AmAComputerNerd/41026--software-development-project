import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  base: '/grades/',
  plugins: [vue()],
  server: {
    proxy: {
      '/api/grades': {
        target: 'http://localhost:5105',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/grades/, ''),
      },
    },
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})

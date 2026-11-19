import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// base './' so the built files work from WebView2's virtual host and from the dev server alike.
export default defineConfig({
  base: './',
  plugins: [react()],
  server: {
    port: 5173,
    proxy: { '/api': 'http://localhost:5178' },
  },
  build: { outDir: 'dist', emptyOutDir: true, chunkSizeWarningLimit: 900 },
})

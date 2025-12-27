import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig(({ mode }) => ({
  plugins: [vue()],
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        entryFileNames: 'assets/[name]-[hash].js',
        chunkFileNames: 'assets/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash].[ext]'
      }
    },
    sourcemap: mode === 'development',
  },
  server: {
    port: 5174,
    strictPort: true, // 既に使用中なら起動失敗
    proxy: {
      '/api': {
        target: 'http://localhost:5197',
        changeOrigin: true,
        secure: false,
      }
    }
  }
}))
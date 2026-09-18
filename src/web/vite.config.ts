import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: { proxy: { '/api': 'http://localhost:5078', '/health': 'http://localhost:5078' } },
  build: { outDir: '../backend/Variable.App/wwwroot', emptyOutDir: true },
});

import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// The ASP.NET Core app serves whatever lands in wwwroot, so that is the build
// output directory. wwwroot is generated — it is gitignored and safe to wipe.
export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      // The hub needs ws:true or the websocket upgrade never reaches the server
      // and the client silently falls back to long polling.
      '/hubs': { target: 'http://localhost:5179', ws: true },
      '/debug': 'http://localhost:5179',
      '/test': 'http://localhost:5179',
    },
  },
});

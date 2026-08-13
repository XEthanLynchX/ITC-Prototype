import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// The ASP.NET Core app serves whatever lands in wwwroot, so that is the build output
// directory. wwwroot is generated
export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      // ws:true is required, or the websocket upgrade never reaches the server and
      // SignalR silently falls back to long polling.
      '/hubs': { target: 'http://localhost:5179', ws: true },
      '/debug': 'http://localhost:5179',
      '/test': 'http://localhost:5179',
    },
  },
});

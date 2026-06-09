// Vite dev server proxy to forward API requests to the backend.
const backend = API_BASE;

/** @type {import('vite').UserConfig} */
module.exports = {
  server: {
    proxy: {
      // Proxy any request starting with /flights/status to the backend
      '/flights/status': {
        target: backend,
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path,
      },
    },
  },
};

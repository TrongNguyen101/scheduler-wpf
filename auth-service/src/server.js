const app = require('./app');
const { ensureSeedData, loadConfig } = require('./setup');

async function start() {
  const config = loadConfig();
  await ensureSeedData();

  app.listen(config.port, () => {
    console.log(`[auth-service] Listening on http://localhost:${config.port}`);
  });
}

start().catch((err) => {
  console.error('[auth-service] Failed to start:', err);
  process.exit(1);
});



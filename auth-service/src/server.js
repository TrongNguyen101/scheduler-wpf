const app = require("./app");
const {
  loadConfig,
  migrateExistingJsonToMongo,
  ensureSeedDataMongo,
} = require("./setup");
const { connectMongo } = require("./db");

async function start() {
  const config = loadConfig();

  console.log("[auth-service] Connecting to MongoDB...");
  await connectMongo(config.mongoUri);
  console.log("[auth-service] MongoDB connected successfully");

  console.log("[auth-service] Migrating existing data...");
  await migrateExistingJsonToMongo();

  console.log("[auth-service] Ensuring seed data...");
  await ensureSeedDataMongo();

  console.log("[auth-service] Database initialization complete");

  app.listen(config.port, () => {
    console.log(
      `[auth-service] Server listening on http://localhost:${config.port}`
    );
    console.log(
      `[auth-service] Health check: http://localhost:${config.port}/health`
    );
    console.log(
      `[auth-service] Environment: ${process.env.NODE_ENV || "development"}`
    );
  });
}

start().catch((err) => {
  console.error("[auth-service] Failed to start:", err);
  process.exit(1);
});

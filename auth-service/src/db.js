const mongoose = require("mongoose");

async function connectMongo(uri) {
  if (!uri) {
    throw new Error("Missing MongoDB connection string (MONGO_URI)");
  }

  // Set Mongoose configuration
  mongoose.set("strictQuery", true);

  // Simplified connection options for better compatibility
  const options = {
    serverSelectionTimeoutMS: 10000, // 10 seconds
    connectTimeoutMS: 10000,
  };

  try {
    await mongoose.connect(uri, options);

    // Set up connection event handlers
    mongoose.connection.on("connected", () => {
      console.log("[Database] MongoDB connected successfully");
    });

    mongoose.connection.on("error", (err) => {
      console.error("[Database] MongoDB connection error:", err);
    });

    mongoose.connection.on("disconnected", () => {
      console.warn("[Database] MongoDB disconnected");
    });

    // Graceful shutdown
    process.on("SIGINT", async () => {
      try {
        await mongoose.connection.close();
        console.log(
          "[Database] MongoDB connection closed through app termination"
        );
        process.exit(0);
      } catch (error) {
        console.error(
          "[Database] Error during MongoDB connection cleanup:",
          error
        );
        process.exit(1);
      }
    });

    return mongoose.connection;
  } catch (error) {
    console.error("[Database] Failed to connect to MongoDB:", error);
    throw error;
  }
}

module.exports = { connectMongo };

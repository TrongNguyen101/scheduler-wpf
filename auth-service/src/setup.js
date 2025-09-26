const fs = require("fs");
const path = require("path");
const bcrypt = require("bcryptjs");
const { Role, User, UserRole, RefreshToken, Schedule } = require("./models");

function loadConfig() {
  // Enhanced configuration with better defaults and validation
  const cfg = {
    // Server configuration
    port: parseInt(process.env.PORT || "4000", 10),
    environment: process.env.NODE_ENV || "development",

    // Database configuration
    mongoUri:
      process.env.MONGO_URI ||
      "mongodb+srv://khiemlc:khiem123456@khiemlc.1nrng.mongodb.net/?retryWrites=true&w=majority&appName=Khiemlc",

    // JWT configuration
    accessSecret:
      process.env.JWT_ACCESS_SECRET || "change-me-access-secret-for-production",
    refreshSecret:
      process.env.JWT_REFRESH_SECRET ||
      "change-me-refresh-secret-for-production",
    accessExpires: parseInt(process.env.JWT_ACCESS_EXPIRES || "900", 10), // 15 minutes
    refreshExpires: parseInt(process.env.JWT_REFRESH_EXPIRES || "1209600", 10), // 14 days

    // CORS configuration
    corsOrigin: process.env.CORS_ORIGIN || "http://localhost:5173",

    // Application paths
    dataDir: path.join(process.cwd(), "data"),

    // Security configuration
    bcryptRounds: parseInt(process.env.BCRYPT_ROUNDS || "12", 10),
    maxLoginAttempts: parseInt(process.env.MAX_LOGIN_ATTEMPTS || "5", 10),

    // Performance configuration
    requestTimeout: parseInt(process.env.REQUEST_TIMEOUT || "30000", 10), // 30 seconds
    maxRequestSize: process.env.MAX_REQUEST_SIZE || "50mb",
  };

  // Validate critical configuration
  if (cfg.environment === "production") {
    const warnings = [];
    if (cfg.accessSecret.includes("change-me")) {
      warnings.push("JWT_ACCESS_SECRET should be changed for production");
    }
    if (cfg.refreshSecret.includes("change-me")) {
      warnings.push("JWT_REFRESH_SECRET should be changed for production");
    }
    if (cfg.mongoUri.includes("khiemlc:khiem123456")) {
      warnings.push("MONGO_URI should use production credentials");
    }

    if (warnings.length > 0) {
      console.warn("[Security Warning] Production configuration issues:");
      warnings.forEach((warning) => console.warn(`  - ${warning}`));
    }
  }

  return cfg;
}

function ensureDataDir() {
  const { dataDir } = loadConfig();
  if (!fs.existsSync(dataDir)) fs.mkdirSync(dataDir, { recursive: true });
}

function readJson(filePath, fallback) {
  if (!fs.existsSync(filePath)) return fallback;
  const raw = fs.readFileSync(filePath, "utf8");
  try {
    return JSON.parse(raw);
  } catch {
    return fallback;
  }
}

function writeJson(filePath, data) {
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2), "utf8");
}

async function migrateExistingJsonToMongo() {
  // Đưa dữ liệu từ các file JSON cũ vào Mongo nếu DB còn trống
  const { dataDir } = loadConfig();
  const usersPath = path.join(dataDir, "users.json");
  const rolesPath = path.join(dataDir, "roles.json");
  const userRolesPath = path.join(dataDir, "userRoles.json");
  const tokensPath = path.join(dataDir, "refreshTokens.json");

  // Roles
  try {
    const count = await Role.estimatedDocumentCount();
    const roles = readJson(rolesPath, []);
    if (count === 0 && Array.isArray(roles) && roles.length > 0) {
      await Role.insertMany(roles);
    }
  } catch {}

  // Users
  try {
    const count = await User.estimatedDocumentCount();
    const users = readJson(usersPath, []);
    if (count === 0 && Array.isArray(users) && users.length > 0) {
      await User.insertMany(users);
    }
  } catch {}

  // UserRoles
  try {
    const count = await UserRole.estimatedDocumentCount();
    const urs = readJson(userRolesPath, []);
    if (count === 0 && Array.isArray(urs) && urs.length > 0) {
      // Loại bỏ bản ghi trùng userId-roleId nếu có
      const uniqueKey = new Set();
      const filtered = [];
      for (const it of urs) {
        const key = `${it.userId}::${it.roleId}`;
        if (!uniqueKey.has(key)) {
          uniqueKey.add(key);
          filtered.push(it);
        }
      }
      if (filtered.length > 0) await UserRole.insertMany(filtered);
    }
  } catch {}

  // RefreshTokens
  try {
    const count = await RefreshToken.estimatedDocumentCount();
    const tokens = readJson(tokensPath, []);
    if (count === 0 && Array.isArray(tokens) && tokens.length > 0) {
      await RefreshToken.insertMany(tokens);
    }
  } catch {}
}

async function ensureSeedDataMongo() {
  // Seed roles
  const roleCount = await Role.estimatedDocumentCount();
  if (roleCount === 0) {
    await Role.insertMany([
      { id: "admin", name: "Admin" },
      // { id: 'training_staff', name: 'TrainingStaff' },
      { id: "academic_of_department", name: "AcademicOfDepartment" },
      { id: "head_of_department", name: "HeadOfDepartment" },
      // { id: 'viewer', name: 'Viewer' },
    ]);
  }

  // Seed admin user
  const admin = await User.findOne({ username: "admin" }).lean();
  if (!admin) {
    const passwordHash = await bcrypt.hash("admin123", 10);
    await User.create({
      id: "u_admin",
      username: "admin",
      passwordHash,
      isActive: true,
    });
  }

  // Seed user role mapping for admin
  const existingUR = await UserRole.findOne({
    userId: "u_admin",
    roleId: "admin",
  }).lean();
  if (!existingUR) {
    await UserRole.create({ userId: "u_admin", roleId: "admin" });
  }

  // Ensure tokens collection exists by touching it
  await RefreshToken.init();

  // Ensure schedule collection exists and indexes are created
  await Schedule.init();
  console.log("[setup] Schedule collection initialized with indexes");
}

module.exports = {
  loadConfig,
  migrateExistingJsonToMongo,
  ensureSeedDataMongo,
  ensureDataDir,
  readJson,
  writeJson,
};

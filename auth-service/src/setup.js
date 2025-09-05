const fs = require('fs');
const path = require('path');
const bcrypt = require('bcryptjs');

function loadConfig() {
  // no dotenv to avoid writing files; allow env vars override
  const cfg = {
    port: parseInt(process.env.PORT || '4000', 10),
    accessSecret: process.env.JWT_ACCESS_SECRET || 'change-me-access',
    refreshSecret: process.env.JWT_REFRESH_SECRET || 'change-me-refresh',
    accessExpires: parseInt(process.env.JWT_ACCESS_EXPIRES || '900', 10),
    refreshExpires: parseInt(process.env.JWT_REFRESH_EXPIRES || '1209600', 10),
    corsOrigin: process.env.CORS_ORIGIN || 'http://localhost:5173',
    dataDir: path.join(process.cwd(), 'data'),
  };
  return cfg;
}

function ensureDataDir() {
  const { dataDir } = loadConfig();
  if (!fs.existsSync(dataDir)) fs.mkdirSync(dataDir, { recursive: true });
}

function readJson(filePath, fallback) {
  if (!fs.existsSync(filePath)) return fallback;
  const raw = fs.readFileSync(filePath, 'utf8');
  try { return JSON.parse(raw); } catch { return fallback; }
}

function writeJson(filePath, data) {
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
}

async function ensureSeedData() {
  ensureDataDir();
  const { dataDir } = loadConfig();
  const usersPath = path.join(dataDir, 'users.json');
  const rolesPath = path.join(dataDir, 'roles.json');
  const userRolesPath = path.join(dataDir, 'userRoles.json');
  const tokensPath = path.join(dataDir, 'refreshTokens.json');

  const roles = readJson(rolesPath, []);
  if (roles.length === 0) {
    writeJson(rolesPath, [
      { id: 'admin', name: 'Admin' },
      { id: 'training_staff', name: 'TrainingStaff' },
      { id: 'head_of_training', name: 'HeadOfTraining' },
      { id: 'head_of_department', name: 'HeadOfDepartment' },
      { id: 'viewer', name: 'Viewer' },
    ]);
  }

  const users = readJson(usersPath, []);
  if (!users.find(u => u.username === 'admin')) {
    const passwordHash = await bcrypt.hash('admin123', 10);
    users.push({ id: 'u_admin', username: 'admin', passwordHash, isActive: true });
    writeJson(usersPath, users);
  }

  const userRoles = readJson(userRolesPath, []);
  if (!userRoles.find(ur => ur.userId === 'u_admin' && ur.roleId === 'admin')) {
    userRoles.push({ userId: 'u_admin', roleId: 'admin' });
    writeJson(userRolesPath, userRoles);
  }

  if (!fs.existsSync(tokensPath)) writeJson(tokensPath, []);
}

module.exports = {
  loadConfig,
  ensureSeedData,
  ensureDataDir,
  readJson,
  writeJson,
};



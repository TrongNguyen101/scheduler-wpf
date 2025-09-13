const express = require('express');
const jwt = require('jsonwebtoken');
const bcrypt = require('bcryptjs');
const path = require('path');
const { loadConfig, readJson, writeJson } = require('../setup');

const router = express.Router();
const config = loadConfig();

function dataPaths() {
  const base = path.join(process.cwd(), 'data');
  return {
    users: path.join(base, 'users.json'),
    roles: path.join(base, 'roles.json'),
    userRoles: path.join(base, 'userRoles.json'),
    refreshTokens: path.join(base, 'refreshTokens.json'),
  };
}

function signAccessToken(user, roles) {
  const payload = { sub: user.id, username: user.username, roles };
  return jwt.sign(payload, config.accessSecret, { expiresIn: config.accessExpires });
}

function signRefreshToken(user) {
  const payload = { sub: user.id, type: 'refresh' };
  return jwt.sign(payload, config.refreshSecret, { expiresIn: config.refreshExpires });
}

function getRolesForUser(userId) {
  const p = dataPaths();
  const roles = readJson(p.roles, []);
  const userRoles = readJson(p.userRoles, []);
  const set = new Set(userRoles.filter(ur => ur.userId === userId).map(ur => ur.roleId));
  return roles.filter(r => set.has(r.id)).map(r => r.name);
}

router.post('/login', async (req, res) => {
  const { username, password } = req.body || {};
  if (!username || !password) return res.status(400).json({ message: 'Missing username/password' });
  const p = dataPaths();
  const users = readJson(p.users, []);
  const user = users.find(u => u.username === username);
  if (!user || !user.isActive) return res.status(401).json({ message: 'Invalid credentials' });
  const ok = await bcrypt.compare(password, user.passwordHash);
  if (!ok) return res.status(401).json({ message: 'Invalid credentials' });

  const roles = getRolesForUser(user.id);
  const accessToken = signAccessToken(user, roles);
  const refreshToken = signRefreshToken(user);

  const tokens = readJson(p.refreshTokens, []);
  tokens.push({ token: refreshToken, userId: user.id, exp: Math.floor(Date.now() / 1000) + config.refreshExpires, revoked: false });
  writeJson(p.refreshTokens, tokens);

  res.json({ accessToken, refreshToken, user: { id: user.id, username: user.username, roles } });
});

router.post('/refresh', (req, res) => {
  const { refreshToken } = req.body || {};
  if (!refreshToken) return res.status(400).json({ message: 'Missing refreshToken' });
  const p = dataPaths();
  try {
    const decoded = jwt.verify(refreshToken, config.refreshSecret);
    const tokens = readJson(p.refreshTokens, []);
    const stored = tokens.find(t => t.token === refreshToken && !t.revoked);
    if (!stored) return res.status(401).json({ message: 'Invalid refresh token' });

    const users = readJson(p.users, []);
    const user = users.find(u => u.id === decoded.sub);
    if (!user || !user.isActive) return res.status(401).json({ message: 'User not active' });
    const roles = getRolesForUser(user.id);
    const accessToken = signAccessToken(user, roles);
    return res.json({ accessToken });
  } catch (e) {
    return res.status(401).json({ message: 'Invalid refresh token' });
  }
});

router.get('/me', (req, res) => {
  const auth = req.headers.authorization || '';
  const token = auth.startsWith('Bearer ') ? auth.slice(7) : null;
  if (!token) return res.status(401).json({ message: 'Missing token' });
  try {
    const decoded = jwt.verify(token, config.accessSecret);
    return res.json({ sub: decoded.sub, username: decoded.username, roles: decoded.roles });
  } catch (e) {
    return res.status(401).json({ message: 'Invalid token' });
  }
});

router.post('/logout', (req, res) => {
  const { refreshToken } = req.body || {};
  if (!refreshToken) return res.json({ ok: true });
  const p = dataPaths();
  const tokens = readJson(p.refreshTokens, []);
  const idx = tokens.findIndex(t => t.token === refreshToken);
  if (idx >= 0) {
    tokens[idx].revoked = true;
    writeJson(p.refreshTokens, tokens);
  }
  return res.json({ ok: true });
});

module.exports = router;



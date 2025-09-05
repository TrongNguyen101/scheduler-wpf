const express = require('express');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const path = require('path');
const { v4: uuidv4 } = require('uuid');
const { loadConfig, readJson, writeJson } = require('../setup');

const router = express.Router();
const config = loadConfig();

function dataPaths() {
  const base = path.join(process.cwd(), 'data');
  return {
    users: path.join(base, 'users.json'),
    roles: path.join(base, 'roles.json'),
    userRoles: path.join(base, 'userRoles.json'),
  };
}

function requireAuth(req, res, next) {
  const auth = req.headers.authorization || '';
  const token = auth.startsWith('Bearer ') ? auth.slice(7) : null;
  if (!token) return res.status(401).json({ message: 'Unauthorized' });
  try {
    req.user = jwt.verify(token, config.accessSecret);
    next();
  } catch (e) {
    return res.status(401).json({ message: 'Unauthorized' });
  }
}

function requireAdmin(req, res, next) {
  if (!req.user || !Array.isArray(req.user.roles) || !req.user.roles.includes('Admin')) {
    return res.status(403).json({ message: 'Forbidden' });
  }
  return next();
}

router.use(requireAuth);

router.get('/', requireAdmin, (req, res) => {
  const p = dataPaths();
  const users = readJson(p.users, []);
  const roles = readJson(p.roles, []);
  const userRoles = readJson(p.userRoles, []);
  const roleMap = new Map(roles.map(r => [r.id, r.name]));
  const result = users.map(u => ({
    id: u.id,
    username: u.username,
    isActive: u.isActive,
    roles: userRoles.filter(ur => ur.userId === u.id).map(ur => roleMap.get(ur.roleId)),
  }));
  res.json(result);
});

router.post('/', requireAdmin, async (req, res) => {
  const { username, password, roles = [], isActive = true } = req.body || {};
  if (!username || !password) return res.status(400).json({ message: 'Missing username/password' });
  const p = dataPaths();
  const users = readJson(p.users, []);
  if (users.find(u => u.username === username)) return res.status(409).json({ message: 'Username exists' });
  const passwordHash = await bcrypt.hash(password, 10);
  const user = { id: uuidv4(), username, passwordHash, isActive };
  users.push(user);
  writeJson(p.users, users);

  const rolesList = readJson(p.roles, []);
  const userRoles = readJson(p.userRoles, []);
  const roleIdSet = new Set(rolesList.filter(r => roles.includes(r.name)).map(r => r.id));
  for (const roleId of roleIdSet) userRoles.push({ userId: user.id, roleId });
  writeJson(p.userRoles, userRoles);

  res.status(201).json({ id: user.id, username: user.username, isActive: user.isActive, roles: roles });
});

router.put('/:id', requireAdmin, async (req, res) => {
  const { id } = req.params;
  const { password, roles, isActive } = req.body || {};
  const p = dataPaths();
  const users = readJson(p.users, []);
  const user = users.find(u => u.id === id);
  if (!user) return res.status(404).json({ message: 'Not found' });
  if (typeof isActive === 'boolean') user.isActive = isActive;
  if (password) user.passwordHash = await bcrypt.hash(password, 10);
  writeJson(p.users, users);

  if (Array.isArray(roles)) {
    const rolesList = readJson(p.roles, []);
    const userRoles = readJson(p.userRoles, []);
    const filtered = userRoles.filter(ur => ur.userId !== id);
    const roleIdSet = new Set(rolesList.filter(r => roles.includes(r.name)).map(r => r.id));
    for (const roleId of roleIdSet) filtered.push({ userId: id, roleId });
    writeJson(p.userRoles, filtered);
  }

  res.json({ ok: true });
});

router.delete('/:id', requireAdmin, (req, res) => {
  const { id } = req.params;
  const p = dataPaths();
  const users = readJson(p.users, []);
  const idx = users.findIndex(u => u.id === id);
  if (idx < 0) return res.status(404).json({ message: 'Not found' });
  users.splice(idx, 1);
  writeJson(p.users, users);

  const userRoles = readJson(p.userRoles, []);
  writeJson(p.userRoles, userRoles.filter(ur => ur.userId !== id));
  res.json({ ok: true });
});

module.exports = router;



const express = require("express");
const bcrypt = require("bcryptjs");
const jwt = require("jsonwebtoken");
const { v4: uuidv4 } = require("uuid");
const multer = require("multer");
const xlsx = require("xlsx");
const path = require("path");
const { loadConfig } = require("../setup");
const { User, Role, UserRole } = require("../models");

const router = express.Router();
const config = loadConfig();
const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: 5 * 1024 * 1024 },
});

function requireAuth(req, res, next) {
  const auth = req.headers.authorization || "";
  const token = auth.startsWith("Bearer ") ? auth.slice(7) : null;
  if (!token) return res.status(401).json({ message: "Unauthorized" });
  try {
    req.user = jwt.verify(token, config.accessSecret);
    next();
  } catch (e) {
    return res.status(401).json({ message: "Unauthorized" });
  }
}

function requireAdmin(req, res, next) {
  if (
    !req.user ||
    !Array.isArray(req.user.roles) ||
    !req.user.roles.includes("Admin")
  ) {
    return res.status(403).json({ message: "Forbidden" });
  }
  return next();
}

router.use(requireAuth);

router.get("/", requireAdmin, async (req, res) => {
  const { q } = req.query || {};
  const filter = {};
  if (q && typeof q === "string" && q.trim()) {
    filter.username = { $regex: q.trim(), $options: "i" };
  }
  const users = await User.find(filter).lean();
  const allUserIds = users.map((u) => u.id);
  const userRoles = await UserRole.find({ userId: { $in: allUserIds } }).lean();
  const roleIds = [...new Set(userRoles.map((ur) => ur.roleId))];
  const roles = await Role.find({ id: { $in: roleIds } }).lean();
  const roleMap = new Map(roles.map((r) => [r.id, r.name]));
  const rolesByUser = new Map();
  for (const ur of userRoles) {
    if (!rolesByUser.has(ur.userId)) rolesByUser.set(ur.userId, []);
    rolesByUser.get(ur.userId).push(roleMap.get(ur.roleId));
  }
  const result = users.map((u) => ({
    id: u.id,
    username: u.username,
    isActive: u.isActive,
    roles: rolesByUser.get(u.id) || [],
  }));
  res.json(result);
});

router.post("/", requireAdmin, async (req, res) => {
  const {
    username,
    password,
    roles = [],
    isActive = true,
    department,
  } = req.body || {};
  if (!username || !password)
    return res.status(400).json({ message: "Missing username/password" });
  const exists = await User.findOne({ username }).lean();
  if (exists) return res.status(409).json({ message: "Username exists" });
  const passwordHash = await bcrypt.hash(password, 10);
  const user = await User.create({
    id: uuidv4(),
    username,
    passwordHash,
    isActive,
    department,
  });

  const rolesList = await Role.find({ name: { $in: roles } }).lean();
  const roleIdSet = new Set(rolesList.map((r) => r.id));
  const toCreate = Array.from(roleIdSet).map((roleId) => ({
    userId: user.id,
    roleId,
  }));
  if (toCreate.length > 0) await UserRole.insertMany(toCreate);

  res
    .status(201)
    .json({
      id: user.id,
      username: user.username,
      isActive: user.isActive,
      roles: roles,
    });
});

router.put("/:id", requireAdmin, async (req, res) => {
  const { id } = req.params;
  const { password, roles, isActive } = req.body || {};
  const user = await User.findOne({ id });
  if (!user) return res.status(404).json({ message: "Not found" });
  if (typeof isActive === "boolean") user.isActive = isActive;
  if (password) user.passwordHash = await bcrypt.hash(password, 10);
  await user.save();

  if (Array.isArray(roles)) {
    await UserRole.deleteMany({ userId: id });
    const rolesList = await Role.find({ name: { $in: roles } }).lean();
    const roleIdSet = new Set(rolesList.map((r) => r.id));
    const toCreate = Array.from(roleIdSet).map((roleId) => ({
      userId: id,
      roleId,
    }));
    if (toCreate.length > 0) await UserRole.insertMany(toCreate);
  }

  res.json({ ok: true });
});

router.delete("/:id", requireAdmin, async (req, res) => {
  const { id } = req.params;
  const del = await User.deleteOne({ id });
  if (del.deletedCount === 0)
    return res.status(404).json({ message: "Not found" });
  await UserRole.deleteMany({ userId: id });
  res.json({ ok: true });
});

// Import users from Excel
router.post(
  "/import",
  requireAdmin,
  upload.single("file"),
  async (req, res) => {
    if (!req.file) return res.status(400).json({ message: "Missing file" });
    try {
      const workbook = xlsx.read(req.file.buffer, { type: "buffer" });
      const sheetName = workbook.SheetNames[0];
      const sheet = workbook.Sheets[sheetName];
      const rows = xlsx.utils.sheet_to_json(sheet, { defval: "" });

      let created = 0,
        updated = 0,
        skipped = 0;
      const errors = [];

      for (let i = 0; i < rows.length; i++) {
        const row = rows[i];
        const username = String(row.username || row.Username || "").trim();
        const password = String(row.password || row.Password || "").trim();
        const rolesRaw = String(row.roles || row.Roles || "").trim();
        const isActiveRaw = row.isActive ?? row.active ?? row.Active ?? "";
        const isActive =
          String(isActiveRaw).toLowerCase() === "true" ||
          String(isActiveRaw) === "1" ||
          isActiveRaw === true;

        if (!username) {
          skipped++;
          continue;
        }

        const roleNames = rolesRaw
          ? rolesRaw
              .split(/[,;]/)
              .map((s) => s.trim())
              .filter(Boolean)
          : [];
        const rolesList =
          roleNames.length > 0
            ? await Role.find({ name: { $in: roleNames } }).lean()
            : [];
        const roleIdSet = new Set(rolesList.map((r) => r.id));

        let user = await User.findOne({ username });
        if (!user) {
          if (!password) {
            skipped++;
            continue;
          }
          const passwordHash = await bcrypt.hash(password, 10);
          user = await User.create({
            id: uuidv4(),
            username,
            passwordHash,
            isActive: roleNames.length ? isActive : true,
          });
          created++;
        } else {
          if (password) {
            user.passwordHash = await bcrypt.hash(password, 10);
          }
          if (String(isActiveRaw).length > 0) user.isActive = isActive;
          await user.save();
          updated++;
        }

        if (roleIdSet.size > 0) {
          await UserRole.deleteMany({ userId: user.id });
          const toCreate = Array.from(roleIdSet).map((roleId) => ({
            userId: user.id,
            roleId,
          }));
          if (toCreate.length > 0) await UserRole.insertMany(toCreate);
        }
      }

      return res.json({ created, updated, skipped, errors });
    } catch (e) {
      // eslint-disable-next-line no-console
      console.error("Import error:", e);
      return res.status(400).json({ message: "Invalid file format" });
    }
  }
);

// Download Excel template
router.get("/import/template", requireAdmin, async (req, res) => {
  const header = ["username", "password", "roles", "isActive"];
  const sample = [
    {
      username: "user1",
      password: "pass123",
      roles: "HeadOfDepartment",
      isActive: true,
    },
    {
      username: "user2",
      password: "pass123",
      roles: "AcademicOfDepartment",
      isActive: true,
    },
  ];
  const worksheet = xlsx.utils.json_to_sheet(sample, { header });
  const workbook = xlsx.utils.book_new();
  xlsx.utils.book_append_sheet(workbook, worksheet, "Users");
  const buf = xlsx.write(workbook, { type: "buffer", bookType: "xlsx" });
  res.setHeader(
    "Content-Type",
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
  );
  res.setHeader(
    "Content-Disposition",
    'attachment; filename="users_template.xlsx"'
  );
  return res.send(buf);
});

module.exports = router;

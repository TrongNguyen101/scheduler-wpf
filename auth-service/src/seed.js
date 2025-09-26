const { connectMongo } = require('./db');
const { loadConfig } = require('./setup');
const { Role, User, UserRole } = require('./models');
const bcrypt = require('bcryptjs');

async function seed() {
  const config = loadConfig();
  await connectMongo(config.mongoUri);

  // Roles
  const baseRoles = [
    { id: 'admin', name: 'Admin' },
    { id: 'training_staff', name: 'TrainingStaff' },
    { id: 'academic_of_department', name: 'AcademicOfDepartment' },
    { id: 'head_of_department', name: 'HeadOfDepartment' },
  ];

  for (const r of baseRoles) {
    await Role.updateOne({ id: r.id }, { $setOnInsert: r }, { upsert: true });
  }

  // Admin user
  const adminUser = await User.findOne({ username: 'admin' });
  if (!adminUser) {
    const passwordHash = await bcrypt.hash('admin123', 10);
    await User.create({ id: 'u_admin', username: 'admin', passwordHash, isActive: true });
  }

  // Map admin role
  await UserRole.updateOne({ userId: 'u_admin', roleId: 'admin' }, { $setOnInsert: { userId: 'u_admin', roleId: 'admin' } }, { upsert: true });

  console.log('[seed] Done.');
  process.exit(0);
}

seed().catch((err) => {
  console.error('[seed] Failed:', err);
  process.exit(1);
});



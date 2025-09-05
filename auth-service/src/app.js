const express = require('express');
const cors = require('cors');
const rateLimit = require('express-rate-limit');
const { loadConfig } = require('./setup');
const authRoutes = require('./routes/auth');
const userRoutes = require('./routes/users');

const app = express();
const config = loadConfig();

app.use(express.json());
app.use(cors({ origin: config.corsOrigin, credentials: false }));

const limiter = rateLimit({ windowMs: 15 * 60 * 1000, max: 100 });
app.use('/auth/login', limiter);

app.get('/health', (req, res) => res.json({ ok: true }));

app.use('/auth', authRoutes);
app.use('/users', userRoutes);

app.use((err, req, res, next) => {
  // eslint-disable-next-line no-console
  console.error(err);
  res.status(err.status || 500).json({ message: err.message || 'Internal error' });
});

module.exports = app;



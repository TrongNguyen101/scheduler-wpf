const express = require("express");
const jwt = require("jsonwebtoken");
const bcrypt = require("bcryptjs");
const { loadConfig } = require("../setup");
const { User, Role, UserRole, RefreshToken } = require("../models");

const router = express.Router();
const config = loadConfig();

// Middleware for authentication (moved to reusable function)
function requireAuth(req, res, next) {
  const auth = req.headers.authorization || "";
  const token = auth.startsWith("Bearer ") ? auth.slice(7) : null;
  if (!token) return res.status(401).json({ message: "Missing token" });
  try {
    req.user = jwt.verify(token, config.accessSecret);
    next();
  } catch (e) {
    return res.status(401).json({ message: "Invalid token" });
  }
}

function signAccessToken(user, roles) {
  const payload = {
    sub: user.id,
    username: user.username,
    roles,
    department: user.department,
  };
  return jwt.sign(payload, config.accessSecret, {
    expiresIn: config.accessExpires,
  });
}

function signRefreshToken(user) {
  const payload = { sub: user.id, type: "refresh" };
  return jwt.sign(payload, config.refreshSecret, {
    expiresIn: config.refreshExpires,
  });
}

async function getRolesForUser(userId) {
  const userRoles = await UserRole.find({ userId }).lean();
  const roleIds = userRoles.map((ur) => ur.roleId);
  const roles = await Role.find({ id: { $in: roleIds } }).lean();
  return roles.map((r) => r.name);
}

router.post("/login", async (req, res) => {
  try {
    const { username, password } = req.body || {};

    if (!username || !password) {
      return res.status(400).json({
        success: false,
        error: {
          code: "MISSING_CREDENTIALS",
          message: "Username and password are required",
        },
        timestamp: new Date().toISOString(),
      });
    }

    const user = await User.findOne({ username }).lean();
    if (!user || !user.isActive) {
      return res.status(401).json({
        success: false,
        error: {
          code: "INVALID_CREDENTIALS",
          message: "Invalid username or password",
        },
        timestamp: new Date().toISOString(),
      });
    }

    const ok = await bcrypt.compare(password, user.passwordHash);
    if (!ok) {
      return res.status(401).json({
        success: false,
        error: {
          code: "INVALID_CREDENTIALS",
          message: "Invalid username or password",
        },
        timestamp: new Date().toISOString(),
      });
    }

    const roles = await getRolesForUser(user.id);
    const accessToken = signAccessToken(user, roles);
    const refreshToken = signRefreshToken(user);

    // Store refresh token
    await RefreshToken.create({
      token: refreshToken,
      userId: user.id,
      exp: Math.floor(Date.now() / 1000) + config.refreshExpires,
      revoked: false,
    });

    // Successful login response
    res.json({
      success: true,
      data: {
        accessToken,
        refreshToken,
        user: {
          id: user.id,
          username: user.username,
          roles,
          department: user.department,
        },
      },
      timestamp: new Date().toISOString(),
    });
  } catch (error) {
    console.error("Login error:", error);
    res.status(500).json({
      success: false,
      error: {
        code: "INTERNAL_ERROR",
        message: "Login failed due to server error",
      },
      timestamp: new Date().toISOString(),
    });
  }
});

router.post("/refresh", async (req, res) => {
  try {
    const { refreshToken } = req.body || {};

    if (!refreshToken) {
      return res.status(400).json({
        success: false,
        error: {
          code: "MISSING_REFRESH_TOKEN",
          message: "Refresh token is required",
        },
        timestamp: new Date().toISOString(),
      });
    }

    // Verify refresh token with proper error handling
    let decoded;
    try {
      decoded = jwt.verify(refreshToken, config.refreshSecret);
    } catch (jwtError) {
      console.error("JWT verification error:", jwtError.message);

      if (jwtError.name === "TokenExpiredError") {
        return res.status(401).json({
          success: false,
          error: {
            code: "REFRESH_TOKEN_EXPIRED",
            message: "Refresh token has expired",
          },
          timestamp: new Date().toISOString(),
        });
      }

      if (jwtError.name === "JsonWebTokenError") {
        return res.status(401).json({
          success: false,
          error: {
            code: "INVALID_REFRESH_TOKEN",
            message: "Invalid refresh token format",
          },
          timestamp: new Date().toISOString(),
        });
      }

      // Other JWT errors
      return res.status(401).json({
        success: false,
        error: {
          code: "INVALID_REFRESH_TOKEN",
          message: "Refresh token verification failed",
        },
        timestamp: new Date().toISOString(),
      });
    }

    const stored = await RefreshToken.findOne({
      token: refreshToken,
      revoked: false,
    }).lean();

    if (!stored) {
      return res.status(401).json({
        success: false,
        error: {
          code: "INVALID_REFRESH_TOKEN",
          message: "Invalid or revoked refresh token",
        },
        timestamp: new Date().toISOString(),
      });
    }

    const user = await User.findOne({ id: decoded.sub }).lean();
    if (!user || !user.isActive) {
      return res.status(401).json({
        success: false,
        error: {
          code: "USER_INACTIVE",
          message: "User account is inactive",
        },
        timestamp: new Date().toISOString(),
      });
    }

    const roles = await getRolesForUser(user.id);
    const accessToken = signAccessToken(user, roles);

    res.json({
      success: true,
      data: { accessToken },
      timestamp: new Date().toISOString(),
    });
  } catch (error) {
    console.error("Token refresh error:", error);

    // This catch should only handle database errors or other unexpected errors
    // JWT errors are already handled above
    res.status(500).json({
      success: false,
      error: {
        code: "INTERNAL_ERROR",
        message: "Failed to refresh token due to server error",
      },
      timestamp: new Date().toISOString(),
    });
  }
});

router.get("/me", requireAuth, (req, res) => {
  return res.json({
    success: true,
    data: {
      id: req.user.sub,
      username: req.user.username,
      roles: req.user.roles,
      department: req.user.department,
    },
    timestamp: new Date().toISOString(),
  });
});

router.post("/logout", async (req, res) => {
  try {
    const { refreshToken } = req.body || {};

    if (refreshToken) {
      await RefreshToken.updateOne(
        { token: refreshToken },
        { $set: { revoked: true } }
      );
    }

    res.json({
      success: true,
      data: { message: "Logged out successfully" },
      timestamp: new Date().toISOString(),
    });
  } catch (error) {
    console.error("Logout error:", error);
    res.status(500).json({
      success: false,
      error: {
        code: "LOGOUT_ERROR",
        message: "Failed to logout",
      },
      timestamp: new Date().toISOString(),
    });
  }
});

module.exports = router;

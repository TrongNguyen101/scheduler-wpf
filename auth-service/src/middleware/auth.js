const jwt = require("jsonwebtoken");
const { loadConfig } = require("../setup");

const config = loadConfig();

// Authentication middleware
function requireAuth(req, res, next) {
  try {
    const auth = req.headers.authorization || "";
    const token = auth.startsWith("Bearer ") ? auth.slice(7) : null;

    if (!token) {
      return res.status(401).json({
        success: false,
        error: {
          code: "MISSING_TOKEN",
          message: "Authentication token is required",
        },
        timestamp: new Date().toISOString(),
      });
    }

    const decoded = jwt.verify(token, config.accessSecret);
    req.user = decoded;
    next();
  } catch (error) {
    console.error("Authentication error:", error.message);

    if (error.name === "TokenExpiredError") {
      return res.status(401).json({
        success: false,
        error: {
          code: "TOKEN_EXPIRED",
          message: "Authentication token has expired",
        },
        timestamp: new Date().toISOString(),
      });
    }

    if (error.name === "JsonWebTokenError") {
      return res.status(401).json({
        success: false,
        error: {
          code: "INVALID_TOKEN",
          message: "Invalid authentication token",
        },
        timestamp: new Date().toISOString(),
      });
    }

    return res.status(401).json({
      success: false,
      error: {
        code: "AUTHENTICATION_FAILED",
        message: "Authentication failed",
      },
      timestamp: new Date().toISOString(),
    });
  }
}

// Role-based authorization middleware
function requireRoles(allowedRoles) {
  return (req, res, next) => {
    if (!req.user) {
      return res.status(401).json({
        success: false,
        error: { code: "UNAUTHORIZED", message: "Authentication required" },
        timestamp: new Date().toISOString(),
      });
    }

    if (
      !Array.isArray(req.user.roles) ||
      !req.user.roles.some((role) => allowedRoles.includes(role))
    ) {
      return res.status(403).json({
        success: false,
        error: { code: "FORBIDDEN", message: "Insufficient permissions" },
        timestamp: new Date().toISOString(),
      });
    }

    next();
  };
}

// Admin role requirement
function requireAdmin(req, res, next) {
  return requireRoles(["Admin"])(req, res, next);
}

// HeadOfDepartment role requirement
function requireHeadOfDepartment(req, res, next) {
  return requireRoles(["HeadOfDepartment", "Admin"])(req, res, next);
}

// Academic staff role requirement
function requireAcademicStaff(req, res, next) {
  return requireRoles(["AcademicOfDepartment", "HeadOfDepartment", "Admin"])(
    req,
    res,
    next
  );
}

module.exports = {
  requireAuth,
  requireRoles,
  requireAdmin,
  requireHeadOfDepartment,
  requireAcademicStaff,
};

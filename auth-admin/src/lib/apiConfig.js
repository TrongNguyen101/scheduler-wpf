// Centralized API configuration
export const API_CONFIG = {
  BASE_URL: import.meta.env.VITE_API_BASE || "http://localhost:4000",

  // API Endpoints
  ENDPOINTS: {
    // Authentication
    AUTH: {
      LOGIN: "/auth/login",
      REFRESH: "/auth/refresh",
      ME: "/auth/me",
      LOGOUT: "/auth/logout",
    },

    // Schedules
    SCHEDULES: {
      BASE: "/schedules",
      STATS: "/schedules/stats",
      UPLOAD: "/schedules/upload",
      BULK_DELETE: "/schedules/bulk-delete",
      DELETE_ALL: "/schedules/all",
    },

    // Users
    USERS: {
      BASE: "/users",
      IMPORT: "/users/import",
      IMPORT_TEMPLATE: "/users/import/template",
    },

    // Departments & Lecturers
    DEPARTMENTS: {
      BASE: "/departments",
      EXPORT: "/departments/export",
    },

    LECTURERS: "/lecturers",
    SUBJECTS: "/subjects",

    // Backups
    BACKUPS: {
      BASE: "/api/backups",
      UPLOAD: "/api/backups/upload",
      LIST: "/api/backups/list",
      DOWNLOAD: "/api/backups/download",
      DELETE: "/api/backups",
    },

    // System
    HEALTH: "/health",
    API_DOCS: "/api",
  },
};

// Helper function to build full URL
export const buildUrl = (endpoint) => {
  return `${API_CONFIG.BASE_URL}${endpoint}`;
};

// Get token from localStorage with consistent key
export const getAuthToken = () => {
  return localStorage.getItem("aa_accessToken");
};

// Standard headers for API requests
export const getAuthHeaders = () => {
  const token = getAuthToken();
  return {
    "Content-Type": "application/json",
    ...(token && { Authorization: `Bearer ${token}` }),
  };
};

// Helper for authenticated API calls
export const apiCall = async (endpoint, options = {}) => {
  const url = buildUrl(endpoint);
  const headers = {
    ...getAuthHeaders(),
    ...options.headers,
  };

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(
      errorData.error?.message ||
        errorData.message ||
        `HTTP ${response.status}: ${response.statusText}`
    );
  }

  return response.json();
};

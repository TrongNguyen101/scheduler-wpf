import axios from "axios";
import React from "react";

const API_BASE = import.meta.env.VITE_API_BASE || "http://localhost:4000";

let accessToken = null;
let refreshToken = null;
let user = null;

// State management cho auth
let authListeners = [];

const LS_KEYS = {
  access: "aa_accessToken",
  refresh: "aa_refreshToken",
  user: "aa_user",
};

function notifyAuthListeners() {
  authListeners.forEach((listener) => listener());
}

export function setTokens(a, r) {
  accessToken = a;
  refreshToken = r || refreshToken;
  try {
    if (a) localStorage.setItem(LS_KEYS.access, a);
    else localStorage.removeItem(LS_KEYS.access);
    if (r) localStorage.setItem(LS_KEYS.refresh, r);
  } catch {}
  notifyAuthListeners();
}

export function getAccessToken() {
  return accessToken;
}

export function setUser(u) {
  user = u;
  try {
    if (u) localStorage.setItem(LS_KEYS.user, JSON.stringify(u));
    else localStorage.removeItem(LS_KEYS.user);
  } catch {}
  notifyAuthListeners();
}
export function getUser() {
  return user;
}

export function useAuthState() {
  const [, forceRender] = React.useReducer((x) => x + 1, 0);

  React.useEffect(() => {
    authListeners.push(forceRender);
    return () => {
      authListeners = authListeners.filter(
        (listener) => listener !== forceRender
      );
    };
  }, []);

  return { user, accessToken, refreshToken };
}

export async function login(username, password) {
  try {
    const res = await axios.post(`${API_BASE}/auth/login`, {
      username,
      password,
    });

    console.log("Raw login response:", res.data);

    // Check if login was successful
    if (!res.data.success) {
      throw new Error(res.data.error?.message || "Login failed");
    }

    // Server returns data in res.data.data structure
    const {
      accessToken: newAccessToken,
      refreshToken: newRefreshToken,
      user: newUser,
    } = res.data.data;

    if (!newAccessToken || !newUser) {
      throw new Error("Invalid response format from server");
    }

    accessToken = newAccessToken;
    refreshToken = newRefreshToken;
    user = newUser;

    try {
      localStorage.setItem(LS_KEYS.access, accessToken);
      localStorage.setItem(LS_KEYS.refresh, refreshToken);
      localStorage.setItem(LS_KEYS.user, JSON.stringify(user));
    } catch (storageError) {
      console.warn("Failed to save to localStorage:", storageError);
    }

    notifyAuthListeners();
    return res.data.data; // Return the actual data object
  } catch (error) {
    console.error("Login error:", error);
    // Re-throw to be handled by the calling component
    throw error;
  }
}

export async function refresh() {
  if (!refreshToken) throw new Error("No refresh token");
  const res = await axios.post(`${API_BASE}/auth/refresh`, { refreshToken });

  // Server returns data in res.data.data structure
  accessToken = res.data.data.accessToken;

  try {
    localStorage.setItem(LS_KEYS.access, accessToken);
  } catch {}
  notifyAuthListeners();
  return accessToken;
}

export function logout() {
  accessToken = null;
  refreshToken = null;
  user = null;
  try {
    localStorage.removeItem(LS_KEYS.access);
    localStorage.removeItem(LS_KEYS.refresh);
    localStorage.removeItem(LS_KEYS.user);
  } catch {}
  notifyAuthListeners();
}

export const api = axios.create();

api.interceptors.request.use(async (config) => {
  if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`;
  return config;
});

let refreshing = false;
let queue = [];

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const { response, config } = error;
    if (response && response.status === 401 && !config.__retry) {
      config.__retry = true;
      if (!refreshing) {
        refreshing = true;
        try {
          await refresh();
          queue.forEach((fn) => fn());
          queue = [];
        } catch (e) {
          logout();
        } finally {
          refreshing = false;
        }
      }
      await new Promise((resolve) => queue.push(resolve));
      config.headers.Authorization = `Bearer ${accessToken}`;
      return api(config);
    }
    return Promise.reject(error);
  }
);

export function initAuth() {
  try {
    const a = localStorage.getItem(LS_KEYS.access);
    const r = localStorage.getItem(LS_KEYS.refresh);
    const u = localStorage.getItem(LS_KEYS.user);
    accessToken = a || null;
    refreshToken = r || null;
    user = u ? JSON.parse(u) : null;
  } catch {}
}

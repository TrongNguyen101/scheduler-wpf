import axios from 'axios'
import React from 'react'

const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:4000'

let accessToken = null
let refreshToken = null
let user = null

const LS_KEYS = {
  access: 'aa_accessToken',
  refresh: 'aa_refreshToken',
  user: 'aa_user',
}

export function setTokens(a, r) {
  accessToken = a
  refreshToken = r || refreshToken
  try {
    if (a) localStorage.setItem(LS_KEYS.access, a); else localStorage.removeItem(LS_KEYS.access)
    if (r) localStorage.setItem(LS_KEYS.refresh, r)
  } catch {}
}

export function getAccessToken() {
  return accessToken
}

export function setUser(u) {
  user = u
  try {
    if (u) localStorage.setItem(LS_KEYS.user, JSON.stringify(u)); else localStorage.removeItem(LS_KEYS.user)
  } catch {}
}
export function getUser() { return user }

export function useAuthState() {
  const [, force] = React.useReducer(x => x + 1, 0)
  React.useEffect(() => {
    const id = setInterval(() => force(), 3000)
    return () => clearInterval(id)
  }, [])
  return { user, accessToken, refreshToken }
}

export async function login(username, password) {
  const res = await axios.post(`${API_BASE}/auth/login`, { username, password })
  accessToken = res.data.accessToken
  refreshToken = res.data.refreshToken
  user = res.data.user
  try {
    localStorage.setItem(LS_KEYS.access, accessToken)
    localStorage.setItem(LS_KEYS.refresh, refreshToken)
    localStorage.setItem(LS_KEYS.user, JSON.stringify(user))
  } catch {}
  return res.data
}

export async function refresh() {
  if (!refreshToken) throw new Error('No refresh token')
  const res = await axios.post(`${API_BASE}/auth/refresh`, { refreshToken })
  accessToken = res.data.accessToken
  return accessToken
}

export function logout() {
  accessToken = null
  refreshToken = null
  user = null
  try {
    localStorage.removeItem(LS_KEYS.access)
    localStorage.removeItem(LS_KEYS.refresh)
    localStorage.removeItem(LS_KEYS.user)
  } catch {}
}

export const api = axios.create()

api.interceptors.request.use(async (config) => {
  if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`
  return config
})

let refreshing = false
let queue = []

api.interceptors.response.use(
  res => res,
  async (error) => {
    const { response, config } = error
    if (response && response.status === 401 && !config.__retry) {
      config.__retry = true
      if (!refreshing) {
        refreshing = true
        try {
          await refresh()
          queue.forEach(fn => fn())
          queue = []
        } catch (e) {
          logout()
        } finally {
          refreshing = false
        }
      }
      await new Promise(resolve => queue.push(resolve))
      config.headers.Authorization = `Bearer ${accessToken}`
      return api(config)
    }
    return Promise.reject(error)
  }
)

export function initAuth() {
  try {
    const a = localStorage.getItem(LS_KEYS.access)
    const r = localStorage.getItem(LS_KEYS.refresh)
    const u = localStorage.getItem(LS_KEYS.user)
    accessToken = a || null
    refreshToken = r || null
    user = u ? JSON.parse(u) : null
  } catch {}
}



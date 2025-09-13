import React from 'react'
import { Routes, Route, Link, Navigate, useNavigate } from 'react-router-dom'
import Login from './pages/Login.jsx'
import Users from './pages/Users.jsx'
import { getAccessToken, logout, useAuthState } from './lib/auth.js'

function PrivateRoute({ children }) {
  const token = getAccessToken()
  if (!token) return <Navigate to="/login" replace />
  return children
}

function Layout() {
  const navigate = useNavigate()
  const { user } = useAuthState()
  return (
    <div style={{ padding: 16 }}>
      <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
        <Link to="/users">Users</Link>
        <div style={{ flex: 1 }} />
        {user && <span>Xin chào, {user.username}</span>}
        <button onClick={() => { logout(); navigate('/login') }}>Đăng xuất</button>
      </div>
      <hr />
      <Routes>
        <Route path="/users" element={<Users />} />
        <Route path="*" element={<Navigate to="/users" replace />} />
      </Routes>
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/*" element={<PrivateRoute><Layout /></PrivateRoute>} />
    </Routes>
  )
}



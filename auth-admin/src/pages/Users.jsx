import React from 'react'
import { api, useAuthState } from '../lib/auth.js'

export default function Users() {
  const { user } = useAuthState()
  const [items, setItems] = React.useState([])
  const [loading, setLoading] = React.useState(true)
  const [error, setError] = React.useState('')
  const [form, setForm] = React.useState({ username: '', password: '', roles: ['Viewer'], isActive: true })
  const [editing, setEditing] = React.useState(null)
  const [editForm, setEditForm] = React.useState({ password: '', roles: [], isActive: true })

  const load = async () => {
    setLoading(true)
    setError('')
    try {
      const res = await api.get('http://localhost:4000/users')
      setItems(res.data)
    } catch (e) {
      setError(e?.response?.data?.message || 'Lỗi tải danh sách')
    } finally {
      setLoading(false)
    }
  }

  React.useEffect(() => { load() }, [])

  const createUser = async () => {
    try {
      await api.post('http://localhost:4000/users', form)
      setForm({ username: '', password: '', roles: ['Viewer'], isActive: true })
      await load()
    } catch (e) {
      alert(e?.response?.data?.message || 'Tạo người dùng thất bại')
    }
  }

  const removeUser = async (id) => {
    if (!confirm('Xóa người dùng?')) return
    try {
      await api.delete(`http://localhost:4000/users/${id}`)
      await load()
    } catch (e) {
      alert('Xóa thất bại')
    }
  }

  const beginEdit = (u) => {
    setEditing(u)
    setEditForm({ password: '', roles: u.roles || [], isActive: !!u.isActive })
  }

  const saveEdit = async () => {
    if (!editing) return
    try {
      const body = { isActive: editForm.isActive, roles: editForm.roles }
      if (editForm.password) body.password = editForm.password
      await api.put(`http://localhost:4000/users/${editing.id}`, body)
      setEditing(null)
      setEditForm({ password: '', roles: [], isActive: true })
      await load()
    } catch (e) {
      alert(e?.response?.data?.message || 'Cập nhật thất bại')
    }
  }

  if (!user?.roles?.includes('Admin')) {
    return <div style={{ padding: 16 }}>Bạn không có quyền Admin</div>
  }

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xl font-semibold">Quản lý người dùng</h2>
        <button onClick={load} className="px-3 py-1.5 rounded bg-indigo-600 text-white hover:bg-indigo-700">Làm mới</button>
      </div>
      {loading ? <div className="text-gray-500">Đang tải...</div> : (
        <div className="overflow-x-auto bg-white border border-gray-200 rounded-xl shadow-sm">
          <table className="min-w-full text-sm">
            <thead className="bg-gray-50 text-gray-600">
              <tr>
                <th className="text-left px-4 py-2 border-b">Username</th>
                <th className="text-left px-4 py-2 border-b">Roles</th>
                <th className="text-left px-4 py-2 border-b">Active</th>
                <th className="text-left px-4 py-2 border-b"></th>
              </tr>
            </thead>
            <tbody>
              {items.map(u => (
                <tr key={u.id} className="odd:bg-white even:bg-gray-50">
                  <td className="px-4 py-2">{u.username}</td>
                  <td className="px-4 py-2">
                    <div className="flex flex-wrap gap-1">
                      {u.roles?.map(r => <span key={r} className="px-2 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-100">{r}</span>)}
                    </div>
                  </td>
                  <td className="px-4 py-2">
                    <span className={u.isActive ? 'px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-100' : 'px-2 py-0.5 rounded bg-rose-50 text-rose-700 border border-rose-100'}>
                      {u.isActive ? 'Yes' : 'No'}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-right space-x-2">
                    <button onClick={() => beginEdit(u)} className="px-2 py-1 rounded bg-amber-500 text-white hover:bg-amber-600">Sửa</button>
                    <button onClick={() => removeUser(u.id)} className="px-2 py-1 rounded bg-rose-600 text-white hover:bg-rose-700">Xóa</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <h3 className="mt-6 mb-2 font-medium">Thêm người dùng</h3>
      <div className="grid gap-2 max-w-xl bg-white border border-gray-200 rounded-xl shadow-sm p-4">
        <input className="px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-400" placeholder="Username" value={form.username} onChange={e=>setForm({ ...form, username: e.target.value })} />
        <input className="px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-400" placeholder="Password" type="password" value={form.password} onChange={e=>setForm({ ...form, password: e.target.value })} />
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
          {['Admin','TrainingStaff','HeadOfTraining','HeadOfDepartment','Viewer'].map(role => (
            <label key={role} className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={form.roles.includes(role)} onChange={e=>{
                const checked = e.target.checked
                setForm(f=> ({ ...f, roles: checked ? Array.from(new Set([...f.roles, role])) : f.roles.filter(r=>r!==role) }))
              }} /> {role}
            </label>
          ))}
        </div>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.isActive} onChange={e=>setForm({ ...form, isActive: e.target.checked })} /> Active
        </label>
        <div>
          <button onClick={createUser} className="px-3 py-2 rounded bg-emerald-600 text-white hover:bg-emerald-700">Tạo</button>
        </div>
      </div>

      {editing && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4">
          <div className="w-full max-w-md bg-white rounded-xl shadow-xl border border-gray-200 p-4">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold">Chỉnh sửa: {editing.username}</h3>
              <button className="text-gray-500 hover:text-gray-700" onClick={() => setEditing(null)}>Đóng</button>
            </div>
            <div className="grid gap-2">
              <input className="px-3 py-2 border rounded-lg" placeholder="Mật khẩu mới (bỏ trống nếu giữ nguyên)" type="password" value={editForm.password} onChange={e=>setEditForm({ ...editForm, password: e.target.value })} />
              <div className="grid grid-cols-2 gap-2">
                {['Admin','TrainingStaff','HeadOfTraining','HeadOfDepartment','Viewer'].map(role => (
                  <label key={role} className="flex items-center gap-2 text-sm">
                    <input type="checkbox" checked={editForm.roles.includes(role)} onChange={e=>{
                      const checked = e.target.checked
                      setEditForm(f=> ({ ...f, roles: checked ? Array.from(new Set([...f.roles, role])) : f.roles.filter(r=>r!==role) }))
                    }} /> {role}
                  </label>
                ))}
              </div>
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" checked={editForm.isActive} onChange={e=>setEditForm({ ...editForm, isActive: e.target.checked })} /> Active
              </label>
            </div>
            <div className="mt-3 flex justify-end gap-2">
              <button className="px-3 py-2 rounded bg-gray-100 hover:bg-gray-200" onClick={() => setEditing(null)}>Hủy</button>
              <button className="px-3 py-2 rounded bg-indigo-600 text-white hover:bg-indigo-700" onClick={saveEdit}>Lưu</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}



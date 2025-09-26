import React from "react";
import { api, useAuthState } from "../lib/auth.js";
import { API_CONFIG, buildUrl } from "../lib/apiConfig.js";

export default function Users() {
  const { user } = useAuthState();
  const [items, setItems] = React.useState([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState("");
  const [form, setForm] = React.useState({
    username: "",
    password: "",
    roles: ["AcademicOfDepartment"],
    department: "",
    isActive: true,
  });
  const [editing, setEditing] = React.useState(null);
  const [editForm, setEditForm] = React.useState({
    password: "",
    roles: [],
    isActive: true,
  });
  const [showCreate, setShowCreate] = React.useState(false);
  const [search, setSearch] = React.useState("");
  const [showImport, setShowImport] = React.useState(false);
  const [importing, setImporting] = React.useState(false);
  const allMajors = [
    "AI",
    "BA",
    "EC",
    "EL",
    "FN",
    "HM",
    "GD",
    "IB",
    "IA",
    "KR",
    "JL",
    "SE",
    "MC",
    "TM",
  ];

  const load = async () => {
    setLoading(true);
    setError("");
    try {
      const params = new URLSearchParams();
      if (search && search.trim()) params.set("q", search.trim());
      const res = await api.get(
        buildUrl(`${API_CONFIG.ENDPOINTS.USERS.BASE}${
          params.toString() ? "?" + params.toString() : ""
        }`)
      );
      setItems(res.data);
    } catch (e) {
      setError(e?.response?.data?.message || "Lỗi tải danh sách");
    } finally {
      setLoading(false);
    }
  };

  React.useEffect(() => {
    load();
  }, []);

  React.useEffect(() => {
    const id = setTimeout(() => {
      load();
    }, 400);
    return () => clearTimeout(id);
  }, [search]);

  const createUser = async () => {
    try {
      if (!form.username || !form.password) {
        alert("Vui lòng nhập username và password!");
        return;
      }

      if (
        form.roles.includes("AcademicOfDepartment") &&
        (!form.department || form.department === "")
      ) {
        alert("Vui lòng chọn chuyên ngành!");
        return;
      }

      await api.post(buildUrl(API_CONFIG.ENDPOINTS.USERS.BASE), form);
      setForm({
        username: "",
        password: "",
        roles: ["AcademicOfDepartment"],
        isActive: true,
      });
      setShowCreate(false);
      await load();
    } catch (e) {
      alert(e?.response?.data?.message || "Tạo người dùng thất bại");
    }
  };

  const removeUser = async (id) => {
    if (!confirm("Xóa người dùng?")) return;
    try {
      await api.delete(buildUrl(`${API_CONFIG.ENDPOINTS.USERS.BASE}/${id}`));
      await load();
    } catch (e) {
      alert("Xóa thất bại");
    }
  };

  const beginEdit = (u) => {
    setEditing(u);
    setEditForm({ password: "", roles: u.roles || [], isActive: !!u.isActive });
  };

  const saveEdit = async () => {
    if (!editing) return;
    try {
      const body = { isActive: editForm.isActive, roles: editForm.roles };
      if (editForm.password) body.password = editForm.password;
      await api.put(buildUrl(`${API_CONFIG.ENDPOINTS.USERS.BASE}/${editing.id}`), body);
      setEditing(null);
      setEditForm({ password: "", roles: [], isActive: true });
      await load();
    } catch (e) {
      alert(e?.response?.data?.message || "Cập nhật thất bại");
    }
  };

  if (!user?.roles?.includes("Admin")) {
    return <div style={{ padding: 16 }}>Bạn không có quyền Admin</div>;
  }

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h2 className="text-2xl font-bold">Quản lý người dùng</h2>
          <p className="text-sm text-gray-500">
            Quản trị tài khoản và phân quyền
          </p>
        </div>
        <div className="flex items-center gap-2">
          <div className="relative">
            <input
              placeholder="Tìm theo username..."
              className="pl-9 pr-3 py-1.5 rounded-lg border border-gray-200 bg-white focus:outline-none focus:ring-2 focus:ring-indigo-400"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-gray-400">
              🔎
            </span>
          </div>
          <button
            onClick={load}
            className="px-3 py-1.5 rounded-lg bg-gray-100 text-gray-700 hover:bg-gray-200 border border-gray-200"
          >
            Làm mới
          </button>
          <button
            onClick={() => {
              setForm({
                username: "",
                password: "",
                roles: ["AcademicOfDepartment"],
                isActive: true,
              });
              setShowCreate(true);
            }}
            className="px-3 py-1.5 rounded-lg bg-emerald-600 text-white hover:bg-emerald-700"
          >
            Thêm mới
          </button>
          <button
            onClick={() => setShowImport(true)}
            className="px-3 py-1.5 rounded-lg bg-indigo-600 text-white hover:bg-indigo-700"
          >
            Nhập Excel
          </button>
        </div>
      </div>
      {error && (
        <div className="mb-4 px-3 py-2 rounded-lg border border-rose-200 bg-rose-50 text-rose-700">
          {error}
        </div>
      )}
      {loading ? (
        <div className="text-gray-500">Đang tải...</div>
      ) : (
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
              {items.map((u) => (
                <tr key={u.id} className="odd:bg-white even:bg-gray-50">
                  <td className="px-4 py-2">{u.username}</td>
                  <td className="px-4 py-2">
                    <div className="flex flex-wrap gap-1">
                      {u.roles?.map((r) => (
                        <span
                          key={r}
                          className="px-2 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-100"
                        >
                          {r}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td className="px-4 py-2">
                    <span
                      className={
                        u.isActive
                          ? "px-2 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-100"
                          : "px-2 py-0.5 rounded bg-rose-50 text-rose-700 border border-rose-100"
                      }
                    >
                      {u.isActive ? "Yes" : "No"}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-right space-x-2">
                    <button
                      onClick={() => beginEdit(u)}
                      className="px-2 py-1 rounded bg-amber-500 text-white hover:bg-amber-600"
                    >
                      Sửa
                    </button>
                    <button
                      onClick={() => removeUser(u.id)}
                      className="px-2 py-1 rounded bg-rose-600 text-white hover:bg-rose-700"
                    >
                      Xóa
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showCreate && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
          <div className="w-full max-w-2xl bg-white rounded-xl shadow-xl border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold">Thêm người dùng</h3>
              <button
                className="text-gray-500 hover:text-gray-700"
                onClick={() => setShowCreate(false)}
              >
                Đóng
              </button>
            </div>
            <div className="grid gap-2">
              <input
                className="px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-400"
                placeholder="Username"
                value={form.username}
                onChange={(e) => setForm({ ...form, username: e.target.value })}
              />
              <input
                className="px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-400"
                placeholder="Password"
                type="password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
              />
              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-2">
                {["Admin", "AcademicOfDepartment", "HeadOfDepartment"].map(
                  (role) => (
                    <label
                      key={role}
                      className="group flex items-center gap-2 text-sm px-2 py-1.5 rounded-lg border border-gray-200 bg-white hover:bg-gray-50"
                    >
                      <input
                        type="checkbox"
                        className="accent-indigo-600"
                        checked={form.roles.includes(role)}
                        onChange={(e) => {
                          const checked = e.target.checked;
                          setForm((f) => ({
                            ...f,
                            roles: checked
                              ? Array.from(new Set([...f.roles, role]))
                              : f.roles.filter((r) => r !== role),
                          }));
                        }}
                      />
                      <span className="whitespace-nowrap">{role}</span>
                    </label>
                  )
                )}
              </div>
              {/* Nếu chọn AcademicOfDepartment thì hiện select major */}
              {form.roles.includes("AcademicOfDepartment") && (
                <select
                  className="px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-400"
                  value={form.major}
                  required
                  onChange={(e) =>
                    setForm({ ...form, department: e.target.value })
                  }
                >
                  <option value="">-- Chọn chuyên ngành --</option>
                  {allMajors.map((m) => (
                    <option key={m} value={m}>
                      {m}
                    </option>
                  ))}
                </select>
              )}
              <label className="flex items-center justify-between text-sm select-none">
                <span>Kích hoạt</span>
                <span
                  className={
                    (form.isActive ? "bg-emerald-500" : "bg-gray-300") +
                    " relative inline-flex h-6 w-11 items-center rounded-full transition-colors"
                  }
                  aria-hidden="true"
                >
                  <span
                    className={
                      (form.isActive ? "translate-x-6" : "translate-x-1") +
                      " inline-block h-4 w-4 transform rounded-full bg-white transition-transform"
                    }
                  />
                </span>
                <input
                  type="checkbox"
                  className="sr-only"
                  checked={form.isActive}
                  onChange={(e) =>
                    setForm({ ...form, isActive: e.target.checked })
                  }
                />
              </label>
            </div>
            <div className="mt-3 flex justify-end gap-2">
              <button
                className="px-3 py-2 rounded bg-gray-100 hover:bg-gray-200"
                onClick={() => setShowCreate(false)}
              >
                Hủy
              </button>
              <button
                onClick={createUser}
                className="px-3 py-2 rounded bg-emerald-600 text-white hover:bg-emerald-700"
              >
                Tạo
              </button>
            </div>
          </div>
        </div>
      )}

      {showImport && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
          <div className="w-full max-w-2xl bg-white rounded-xl shadow-xl border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold">Nhập người dùng từ Excel</h3>
              <button
                className="text-gray-500 hover:text-gray-700"
                onClick={() => setShowImport(false)}
              >
                Đóng
              </button>
            </div>
            <div className="grid gap-3">
              <p className="text-sm text-gray-600">
                Cột bắt buộc: <b>username</b>, <b>password</b>. Tuỳ chọn:{" "}
                <b>roles</b> (phân tách bằng dấu phẩy), <b>isActive</b>.
              </p>
              <div>
                <button
                  className="px-3 py-2 rounded-lg bg-gray-100 hover:bg-gray-200 border border-gray-200 text-sm"
                  onClick={async () => {
                    try {
                      const res = await api.get(
                        buildUrl(API_CONFIG.ENDPOINTS.USERS.IMPORT_TEMPLATE),
                        { responseType: "blob" }
                      );
                      const url = URL.createObjectURL(res.data);
                      const a = document.createElement("a");
                      a.href = url;
                      a.download = "users_template.xlsx";
                      document.body.appendChild(a);
                      a.click();
                      a.remove();
                      URL.revokeObjectURL(url);
                    } catch (e) {
                      alert("Tải file mẫu thất bại");
                    }
                  }}
                >
                  Tạo file mẫu
                </button>
              </div>
              <input
                type="file"
                accept=".xlsx,.xls"
                onChange={async (e) => {
                  const file = e.target.files?.[0];
                  if (!file) return;
                  const formData = new FormData();
                  formData.append("file", file);
                  setImporting(true);
                  try {
                    await api.post(
                      buildUrl(API_CONFIG.ENDPOINTS.USERS.IMPORT),
                      formData,
                      { headers: { "Content-Type": "multipart/form-data" } }
                    );
                    setShowImport(false);
                    await load();
                  } catch (err) {
                    alert(err?.response?.data?.message || "Import thất bại");
                  } finally {
                    setImporting(false);
                    e.target.value = "";
                  }
                }}
              />
              <div className="text-xs text-gray-500">
                Mẹo: Bạn có thể có sheet với cột: username, password, roles,
                isActive
              </div>
            </div>
            <div className="mt-3 flex justify-end gap-2">
              <button
                className="px-3 py-2 rounded bg-gray-100 hover:bg-gray-200"
                onClick={() => setShowImport(false)}
                disabled={importing}
              >
                Đóng
              </button>
              <button
                className="px-3 py-2 rounded bg-indigo-600 text-white opacity-70 cursor-not-allowed"
                disabled
              >
                Tải lên
              </button>
            </div>
          </div>
        </div>
      )}

      {editing && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4">
          <div className="w-full max-w-2xl bg-white rounded-xl shadow-xl border border-gray-200 p-6">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold">Chỉnh sửa: {editing.username}</h3>
              <button
                className="text-gray-500 hover:text-gray-700"
                onClick={() => setEditing(null)}
              >
                Đóng
              </button>
            </div>
            <div className="grid gap-2">
              <input
                className="px-3 py-2 border rounded-lg"
                placeholder="Mật khẩu mới (bỏ trống nếu giữ nguyên)"
                type="password"
                value={editForm.password}
                onChange={(e) =>
                  setEditForm({ ...editForm, password: e.target.value })
                }
              />
              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-2">
                {["Admin", "AcademicOfDepartment", "HeadOfDepartment"].map(
                  (role) => (
                    <label
                      key={role}
                      className="group flex items-center gap-2 text-sm px-2 py-1.5 rounded-lg border border-gray-200 bg-white hover:bg-gray-50"
                    >
                      <input
                        type="checkbox"
                        className="accent-indigo-600"
                        checked={editForm.roles.includes(role)}
                        onChange={(e) => {
                          const checked = e.target.checked;
                          setEditForm((f) => ({
                            ...f,
                            roles: checked
                              ? Array.from(new Set([...f.roles, role]))
                              : f.roles.filter((r) => r !== role),
                          }));
                        }}
                      />
                      <span className="whitespace-nowrap">{role}</span>
                    </label>
                  )
                )}
              </div>
              <label className="flex items-center justify-between text-sm select-none">
                <span>Kích hoạt</span>
                <span
                  className={
                    (editForm.isActive ? "bg-emerald-500" : "bg-gray-300") +
                    " relative inline-flex h-6 w-11 items-center rounded-full transition-colors"
                  }
                  aria-hidden="true"
                >
                  <span
                    className={
                      (editForm.isActive ? "translate-x-6" : "translate-x-1") +
                      " inline-block h-4 w-4 transform rounded-full bg-white transition-transform"
                    }
                  />
                </span>
                <input
                  type="checkbox"
                  className="sr-only"
                  checked={editForm.isActive}
                  onChange={(e) =>
                    setEditForm({ ...editForm, isActive: e.target.checked })
                  }
                />
              </label>
            </div>
            <div className="mt-3 flex justify-end gap-2">
              <button
                className="px-3 py-2 rounded bg-gray-100 hover:bg-gray-200"
                onClick={() => setEditing(null)}
              >
                Hủy
              </button>
              <button
                className="px-3 py-2 rounded bg-indigo-600 text-white hover:bg-indigo-700"
                onClick={saveEdit}
              >
                Lưu
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

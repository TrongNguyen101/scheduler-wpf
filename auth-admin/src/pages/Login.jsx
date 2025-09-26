import React from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../lib/auth.js";

export default function Login() {
  const [username, setUsername] = React.useState("admin");
  const [password, setPassword] = React.useState("admin123");
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState("");
  const navigate = useNavigate();

  const onSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError("");
    try {
      const data = await login(username, password);
      console.log("Login response:", data);
      
      const user = data.user;
      console.log("User data:", user);
      console.log("User roles:", user?.roles);
      
      if (user && user.roles && user.roles.includes("Admin")) {
        navigate("/users");
      } else if (
        user && user.roles && (
          user.roles.includes("HeadOfDepartment") ||
          user.roles.includes("AcademicOfDepartment")
        )
      ) {
        navigate("/lecturers");
      } else {
        navigate("/");
      }
    } catch (err) {
      console.error("Login error:", err);
      // Handle different error response structures
      const errorMessage = 
        err?.response?.data?.error?.message || 
        err?.response?.data?.message || 
        err?.message || 
        "Đăng nhập thất bại";
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-sky-50 to-indigo-50 p-4">
      <form
        onSubmit={onSubmit}
        className="w-full max-w-sm bg-white shadow-xl rounded-xl p-6 space-y-3 border border-gray-100"
      >
        <h2 className="text-2xl font-bold text-gray-800">Đăng nhập</h2>
        {error && (
          <div className="text-sm text-red-600 bg-red-50 border border-red-100 rounded p-2">
            {error}
          </div>
        )}
        <div className="space-y-1">
          <label className="text-sm text-gray-600">Tài khoản</label>
          <input
            className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-400"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
          />
        </div>
        <div className="space-y-1">
          <label className="text-sm text-gray-600">Mật khẩu</label>
          <input
            className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-400"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </div>
        <button
          disabled={loading}
          className="w-full py-2.5 rounded-lg bg-indigo-600 text-white font-medium hover:bg-indigo-700 disabled:opacity-60 transition"
        >
          {loading ? "Đang đăng nhập..." : "Đăng nhập"}
        </button>
      </form>
    </div>
  );
}

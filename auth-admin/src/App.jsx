import React from "react";
import {
  Routes,
  Route,
  Link,
  Navigate,
  useNavigate,
  useLocation,
} from "react-router-dom";
import Login from "./pages/Login.jsx";
import Users from "./pages/Users.jsx";
import LecturerAssignments from "./pages/Lecturers.jsx"; // tạo component riêng nếu cần
import ScheduleReview from "./pages/ScheduleReview.jsx";
import Backups from "./pages/Backups.jsx";
import { getAccessToken, logout, useAuthState } from "./lib/auth.js";
import Unauthorized from "./pages/Unauthorized.jsx";

function PrivateRoute({ children, allowedRoles }) {
  const token = getAccessToken();
  const { user } = useAuthState();

  // Chưa đăng nhập → về login
  if (!token) return <Navigate to="/login" replace />;

  // Nếu chưa có user (state chưa load) → hiển thị loading
  if (!user) return <div>Loading...</div>;

  // Nếu có giới hạn role nhưng user không có bất kỳ role phù hợp → chặn
  if (
    allowedRoles &&
    (!Array.isArray(user.roles) ||
      !user.roles.some((r) => allowedRoles.includes(r)))
  ) {
    return <Navigate to="/unauthorized" replace />;
  }

  return children;
}

function Layout() {
  const navigate = useNavigate();
  const { user } = useAuthState();
  const location = useLocation();
  const pathname = location.pathname;
  return (
    <div className="p-4">
      <div className="sticky top-0 z-30 mb-4">
        <div className="">
          <div className="bg-white/80 backdrop-blur border border-gray-200 rounded-none sm:rounded-2xl shadow-sm">
            <div className="px-4 py-3 flex items-center gap-3">
              <div className="flex items-center gap-2">
                <div className="h-8 w-8 rounded-xl bg-gradient-to-br from-indigo-500 to-emerald-500 shadow" />
                <div className="hidden sm:block">
                  <div className="text-sm font-semibold leading-4">
                    Scheduler Admin
                  </div>
                  <div className="text-xs text-gray-500 leading-3">
                    Bảng điều khiển
                  </div>
                </div>
              </div>
              <div className="flex-1" />
              <nav className="flex items-center gap-1">
                <Link
                  to="/users"
                  className={
                    (pathname.startsWith("/users")
                      ? "bg-gray-900 text-white shadow "
                      : "text-gray-700 hover:bg-gray-100 ") +
                    "px-3 py-2 rounded-lg text-sm font-medium transition"
                  }
                >
                  Người dùng
                </Link>
                <Link
                  to="/lecturers"
                  className={
                    (pathname.startsWith("/lecturers")
                      ? "bg-gray-900 text-white shadow "
                      : "text-gray-700 hover:bg-gray-100 ") +
                    "px-3 py-2 rounded-lg text-sm font-medium transition"
                  }
                >
                  Phân công khoa
                </Link>
                <Link
                  to="/schedules"
                  className={
                    (pathname.startsWith("/schedules")
                      ? "bg-gray-900 text-white shadow "
                      : "text-gray-700 hover:bg-gray-100 ") +
                    "px-3 py-2 rounded-lg text-sm font-medium transition"
                  }
                >
                  Lịch học
                </Link>
                <Link
                  to="/backups"
                  className={
                    (pathname.startsWith("/backups")
                      ? "bg-gray-900 text-white shadow "
                      : "text-gray-700 hover:bg-gray-100 ") +
                    "px-3 py-2 rounded-lg text-sm font-medium transition"
                  }
                >
                  Backup
                </Link>
              </nav>
              <div className="flex-1" />
              <div className="hidden sm:flex items-center gap-2">
                {user && (
                  <span className="px-2.5 py-1.5 rounded-lg text-sm bg-gray-100 text-gray-700 border border-gray-200">
                    Xin chào, <b>{user.username}</b>
                  </span>
                )}
                <button
                  onClick={() => {
                    logout();
                    navigate("/login");
                  }}
                  className="px-3 py-2 rounded-lg text-sm font-medium bg-gradient-to-r from-rose-500 to-rose-600 text-white shadow hover:from-rose-600 hover:to-rose-700"
                >
                  Đăng xuất
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
      <Routes>
        <Route
          path="/users"
          element={
            <PrivateRoute allowedRoles={["Admin"]}>
              <Users />
            </PrivateRoute>
          }
        />
        <Route
          path="/lecturers"
          element={
            <PrivateRoute
              allowedRoles={["HeadOfDepartment", "AcademicOfDepartment"]}
            >
              <LecturerAssignments />
            </PrivateRoute>
          }
        />
        <Route
          path="/schedules"
          element={
            <PrivateRoute
              allowedRoles={[
                "Admin",
                "HeadOfDepartment",
                "AcademicOfDepartment",
              ]}
            >
              <ScheduleReview />
            </PrivateRoute>
          }
        />
        <Route
          path="/backups"
          element={
            <PrivateRoute allowedRoles={["Admin"]}>
              <Backups />
            </PrivateRoute>
          }
        />
        <Route path="*" element={<Navigate to="/users" replace />} />
      </Routes>
    </div>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/unauthorized" element={<Unauthorized />} />
      <Route
        path="/*"
        element={
          <PrivateRoute>
            <Layout />
          </PrivateRoute>
        }
      />
    </Routes>
  );
}

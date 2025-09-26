import React, { useState, useEffect, useCallback } from "react";
import { Table, Pagination } from "../components";

const API_BASE = import.meta.env.VITE_API_URL || "http://localhost:4000";

// Hàm helper để gọi API
const callApi = async (url, options = {}) => {
  const token = localStorage.getItem("accessToken");
  const defaultHeaders = {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };

  const response = await fetch(url, {
    ...options,
    headers: {
      ...defaultHeaders,
      ...options.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  return await response.json();
};

export default function Backups() {
  const [backups, setBackups] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState({});
  const [page, setPage] = useState(1);
  const [limit] = useState(10);
  const [total, setTotal] = useState(0);

  // Load danh sách backup
  const loadBackups = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await callApi(`${API_BASE}/api/backups/list`);
      
      if (response.success) {
        setBackups(response.data || []);
        setTotal(response.data?.length || 0);
      } else {
        throw new Error(response.message || "Không thể tải danh sách backup");
      }
    } catch (err) {
      console.error("Load backups error:", err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  // Xóa backup
  const deleteBackup = async (filename) => {
    if (!confirm(`Bạn có chắc chắn muốn xóa backup "${filename}"?\n\nHành động này không thể hoàn tác.`)) {
      return;
    }

    try {
      setDeleteLoading(prev => ({ ...prev, [filename]: true }));
      
      const response = await callApi(`${API_BASE}/api/backups/${encodeURIComponent(filename)}`, {
        method: "DELETE",
      });

      if (response.success) {
        // Reload danh sách backup
        await loadBackups();
        alert("Xóa backup thành công!");
      } else {
        throw new Error(response.message || "Không thể xóa backup");
      }
    } catch (err) {
      console.error("Delete backup error:", err);
      alert(`Lỗi khi xóa backup: ${err.message}`);
    } finally {
      setDeleteLoading(prev => ({ ...prev, [filename]: false }));
    }
  };

  // Download backup
  const downloadBackup = async (filename) => {
    try {
      const token = localStorage.getItem("accessToken");
      const url = `${API_BASE}/api/backups/download/${encodeURIComponent(filename)}`;
      
      const response = await fetch(url, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      // Tạo blob và download
      const blob = await response.blob();
      const downloadUrl = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = downloadUrl;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(downloadUrl);
    } catch (err) {
      console.error("Download backup error:", err);
      alert(`Lỗi khi tải backup: ${err.message}`);
    }
  };

  // Format date
  const formatDate = (dateString) => {
    if (!dateString) return "N/A";
    return new Date(dateString).toLocaleString("vi-VN");
  };

  // Format file size
  const formatFileSize = (bytes) => {
    if (!bytes) return "N/A";
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${sizes[i]}`;
  };

  useEffect(() => {
    loadBackups();
  }, [loadBackups]);

  const columns = [
    {
      key: "filename",
      header: "Tên File",
      render: (item) => (
        <div className="font-medium text-gray-900">{item.filename}</div>
      ),
    },
    {
      key: "size",
      header: "Kích thước",
      render: (item) => (
        <div className="text-sm text-gray-600">{formatFileSize(item.size)}</div>
      ),
    },
    {
      key: "uploadDate",
      header: "Ngày tải lên",
      render: (item) => (
        <div className="text-sm text-gray-600">{formatDate(item.uploadDate)}</div>
      ),
    },
    {
      key: "checksum",
      header: "Checksum",
      render: (item) => (
        <div className="text-xs font-mono text-gray-500 truncate max-w-32" title={item.checksum}>
          {item.checksum || "N/A"}
        </div>
      ),
    },
    {
      key: "actions",
      header: "Thao tác",
      render: (item) => (
        <div className="flex gap-2">
          <button
            onClick={() => downloadBackup(item.filename)}
            className="px-3 py-1 text-xs font-medium text-blue-600 bg-blue-50 border border-blue-200 rounded-md hover:bg-blue-100 transition-colors"
          >
            📥 Tải xuống
          </button>
          <button
            onClick={() => deleteBackup(item.filename)}
            disabled={deleteLoading[item.filename]}
            className="px-3 py-1 text-xs font-medium text-red-600 bg-red-50 border border-red-200 rounded-md hover:bg-red-100 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {deleteLoading[item.filename] ? "..." : "🗑️ Xóa"}
          </button>
        </div>
      ),
    },
  ];

  // Pagination data
  const startIndex = (page - 1) * limit;
  const endIndex = startIndex + limit;
  const paginatedBackups = backups.slice(startIndex, endIndex);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Quản lý Backup</h1>
          <p className="text-sm text-gray-600 mt-1">
            Quản lý các file backup cơ sở dữ liệu
          </p>
        </div>
        <button
          onClick={loadBackups}
          disabled={loading}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
        >
          {loading ? "🔄 Đang tải..." : "🔄 Làm mới"}
        </button>
      </div>

      {/* Error */}
      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-red-800">❌ {error}</p>
        </div>
      )}

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white p-4 rounded-lg border border-gray-200">
          <div className="flex items-center">
            <div className="p-2 bg-blue-100 rounded-lg">
              <span className="text-blue-600 text-lg">📁</span>
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-600">Tổng số backup</p>
              <p className="text-2xl font-bold text-gray-900">{backups.length}</p>
            </div>
          </div>
        </div>
        
        <div className="bg-white p-4 rounded-lg border border-gray-200">
          <div className="flex items-center">
            <div className="p-2 bg-green-100 rounded-lg">
              <span className="text-green-600 text-lg">💾</span>
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-600">Tổng dung lượng</p>
              <p className="text-2xl font-bold text-gray-900">
                {formatFileSize(backups.reduce((total, backup) => total + (backup.size || 0), 0))}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white p-4 rounded-lg border border-gray-200">
          <div className="flex items-center">
            <div className="p-2 bg-orange-100 rounded-lg">
              <span className="text-orange-600 text-lg">📅</span>
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-600">Backup mới nhất</p>
              <p className="text-sm font-bold text-gray-900">
                {backups.length > 0 
                  ? formatDate(Math.max(...backups.map(b => new Date(b.uploadDate || 0).getTime())))
                  : "N/A"
                }
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-lg border border-gray-200">
        <Table
          data={paginatedBackups}
          columns={columns}
          loading={loading}
          emptyMessage="Không có backup nào"
        />
        
        {total > limit && (
          <div className="px-6 py-4 border-t border-gray-200">
            <Pagination
              currentPage={page}
              totalPages={Math.ceil(total / limit)}
              onPageChange={setPage}
            />
          </div>
        )}
      </div>
    </div>
  );
}
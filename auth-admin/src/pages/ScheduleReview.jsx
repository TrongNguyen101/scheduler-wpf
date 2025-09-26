import React, { useState, useEffect } from 'react';
import { scheduleApi, formatters } from '../lib/scheduleApi.js';
import { useAuthState } from '../lib/auth.js';
import Table from '../components/Table.jsx';
import Pagination from '../components/Pagination.jsx';
import ScheduleFilters from '../components/ScheduleFilters.jsx';
import StatsDashboard from '../components/StatsDashboard.jsx';
import ScheduleModal from '../components/ScheduleModal.jsx';

export default function ScheduleReview() {
  // Auth state
  const { user } = useAuthState();
  
  // State management
  const [schedules, setSchedules] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedRows, setSelectedRows] = useState(new Set());
  
  // Pagination state
  const [pagination, setPagination] = useState({
    currentPage: 1,
    totalPages: 1,
    totalRecords: 0,
    recordsPerPage: 50,
  });

  // Filter state
  const [filters, setFilters] = useState({
    page: 1,
    limit: 50,
    lecturerId: '',
    subjectCode: '',
    groupName: '',
    roomId: '',
    major: '',
    statusSlot: '',
    startDate: '',
    endDate: '',
    sortBy: 'date',
    sortOrder: 'asc',
  });

  // Modal state
  const [modalState, setModalState] = useState({
    isOpen: false,
    scheduleId: null,
    mode: 'view', // 'view' or 'edit'
  });

  // Load schedules effect
  useEffect(() => {
    loadSchedules();
  }, [filters]);

  // Main functions
  const loadSchedules = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await scheduleApi.getSchedules(filters);
      
      setSchedules(response.data || []);
      
      if (response.meta?.pagination) {
        setPagination({
          currentPage: response.meta.pagination.currentPage,
          totalPages: response.meta.pagination.totalPages,
          totalRecords: response.meta.pagination.totalRecords,
          recordsPerPage: response.meta.pagination.recordsPerPage,
        });
      }
    } catch (err) {
      setError(err.message);
      console.error('Failed to load schedules:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleFiltersChange = (newFilters) => {
    setFilters({
      ...newFilters,
      page: 1, // Reset to first page when filters change
      limit: filters.limit,
      sortBy: filters.sortBy,
      sortOrder: filters.sortOrder,
    });
    setSelectedRows(new Set());
  };

  const handleClearFilters = () => {
    setFilters({
      page: 1,
      limit: 50,
      lecturerId: '',
      subjectCode: '',
      groupName: '',
      roomId: '',
      major: '',
      statusSlot: '',
      startDate: '',
      endDate: '',
      sortBy: 'date',
      sortOrder: 'asc',
    });
    setSelectedRows(new Set());
  };

  const handlePageChange = (page) => {
    setFilters({ ...filters, page });
    setSelectedRows(new Set());
  };

  const handleRowSelect = (id, checked) => {
    const newSelectedRows = new Set(selectedRows);
    if (checked) {
      newSelectedRows.add(id);
    } else {
      newSelectedRows.delete(id);
    }
    setSelectedRows(newSelectedRows);
  };

  const handleRowClick = (schedule) => {
    setModalState({
      isOpen: true,
      scheduleId: schedule.scheduleId,
      mode: 'view',
    });
  };

  const handleEditSchedule = (scheduleId) => {
    setModalState({
      isOpen: true,
      scheduleId,
      mode: 'edit',
    });
  };

  const handleDeleteSchedule = async (scheduleId) => {
    if (!confirm('Bạn có chắc chắn muốn xóa lịch học này?')) {
      return;
    }

    try {
      await scheduleApi.deleteSchedule(scheduleId);
      loadSchedules(); // Reload data
      setSelectedRows(new Set()); // Clear selection
    } catch (err) {
      alert(`Lỗi xóa lịch học: ${err.message}`);
    }
  };

  const handleBulkDelete = async () => {
    if (selectedRows.size === 0) {
      alert('Vui lòng chọn ít nhất một lịch học để xóa.');
      return;
    }

    if (!confirm(`Bạn có chắc chắn muốn xóa ${selectedRows.size} lịch học đã chọn?`)) {
      return;
    }

    try {
      await scheduleApi.bulkDelete({
        scheduleIds: Array.from(selectedRows),
      });
      loadSchedules(); // Reload data
      setSelectedRows(new Set()); // Clear selection
    } catch (err) {
      alert(`Lỗi xóa hàng loạt: ${err.message}`);
    }
  };

  const handleDeleteAllSchedules = async () => {
    // Double confirmation for safety
    if (!confirm('⚠️ CẢNH BÁO: Bạn sắp xóa TẤT CẢ lịch học trong hệ thống!\n\nĐiều này KHÔNG THỂ HOÀN TÁC!\n\nBạn có chắc chắn muốn tiếp tục?')) {
      return;
    }

    if (!confirm('Xác nhận lần cuối: Bạn có thực sự muốn xóa tất cả lịch học?\n\nNhấn OK để xóa hoàn toàn tất cả dữ liệu lịch học.')) {
      return;
    }

    try {
      setLoading(true);
      const result = await scheduleApi.deleteAllSchedules();
      
      if (result.success) {
        alert(`✅ Đã xóa thành công ${result.meta.deletedCount} lịch học!`);
        loadSchedules(); // Reload data
        setSelectedRows(new Set()); // Clear selection
      } else {
        throw new Error(result.error?.message || 'Có lỗi xảy ra');
      }
    } catch (err) {
      alert(`❌ Lỗi khi xóa tất cả lịch học: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleSortChange = (column, order) => {
    setFilters({
      ...filters,
      sortBy: column,
      sortOrder: order,
      page: 1, // Reset to first page
    });
  };

  const handleModalSave = (updatedSchedule) => {
    // Reload data to reflect changes
    loadSchedules();
  };

  // Table columns configuration
  const columns = [
    {
      key: 'scheduleId',
      header: 'Mã lịch',
      render: (item) => (
        <div className="text-sm font-medium text-gray-900">
          {item.scheduleId}
        </div>
      ),
    },
    {
      key: 'groupName',
      header: 'Nhóm/Lớp',
      render: (item) => (
        <div className="text-sm text-gray-900">{item.groupName}</div>
      ),
    },
    {
      key: 'subjectCode',
      header: 'Môn học',
      render: (item) => (
        <div className="text-sm text-gray-900">{item.subjectCode}</div>
      ),
    },
    {
      key: 'date',
      header: 'Ngày học',
      render: (item) => (
        <div className="text-sm text-gray-900">
          {formatters.formatDate(item.date)}
        </div>
      ),
    },
    {
      key: 'slotTime',
      header: 'Thời gian',
      render: (item) => (
        <div className="text-sm text-gray-900">{item.slotTime}</div>
      ),
    },
    {
      key: 'roomName',
      header: 'Phòng',
      render: (item) => (
        <div className="text-sm text-gray-900">{item.roomName}</div>
      ),
    },
    {
      key: 'lecturerName',
      header: 'Giảng viên',
      render: (item) => (
        <div className="text-sm text-gray-900">{item.lecturerName}</div>
      ),
    },
    {
      key: 'statusSlot',
      header: 'Trạng thái',
      render: (item) => (
        <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${formatters.getStatusColor(item.statusSlot)}`}>
          {formatters.formatStatus(item.statusSlot)}
        </span>
      ),
    },
    {
      key: 'actions',
      header: 'Thao tác',
      render: (item) => (
        <div className="flex space-x-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleEditSchedule(item.scheduleId);
            }}
            className="text-indigo-600 hover:text-indigo-900 text-sm font-medium"
          >
            Sửa
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleDeleteSchedule(item.scheduleId);
            }}
            className="text-red-600 hover:text-red-900 text-sm font-medium"
          >
            Xóa
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Quản lý lịch học</h1>
        <p className="mt-1 text-sm text-gray-600">
          Xem, tìm kiếm và quản lý thông tin lịch học trong hệ thống.
        </p>
      </div>

      {/* Quick Actions - Only for HeadOfDepartment */}
      {user?.roles?.includes('HeadOfDepartment') && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-sm font-medium text-red-800">Thao tác nguy hiểm</h3>
              <p className="text-xs text-red-600 mt-1">
                Chỉ dành cho Trưởng khoa - Không thể hoàn tác
              </p>
            </div>
            <button
              onClick={handleDeleteAllSchedules}
              className="bg-red-700 hover:bg-red-800 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors duration-200 flex items-center gap-2"
              title="⚠️ CẢNH BÁO: Xóa tất cả lịch học trong hệ thống!"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
              Xóa tất cả lịch học
            </button>
          </div>
        </div>
      )}

      {/* Statistics Dashboard */}
      <StatsDashboard 
        filters={{
          lecturerId: filters.lecturerId,
          major: filters.major,
          startDate: filters.startDate,
          endDate: filters.endDate,
        }} 
      />

      {/* Filters */}
      <ScheduleFilters
        filters={filters}
        onFiltersChange={handleFiltersChange}
        onClearFilters={handleClearFilters}
        loading={loading}
      />

      {/* Bulk Actions */}
      {selectedRows.size > 0 && (
        <div className="bg-blue-50 border border-blue-200 rounded-md p-4 mb-6">
          <div className="flex items-center justify-between">
            <div className="text-sm text-blue-700">
              Đã chọn {selectedRows.size} lịch học
            </div>
            <div className="flex space-x-2">
              <button
                onClick={() => setSelectedRows(new Set())}
                className="text-sm text-blue-600 hover:text-blue-800"
              >
                Bỏ chọn tất cả
              </button>
              <button
                onClick={handleBulkDelete}
                className="bg-red-600 text-white px-3 py-1 rounded text-sm hover:bg-red-700"
              >
                Xóa đã chọn
              </button>
              {/* Delete All Button - Only for HeadOfDepartment */}
              {user?.roles?.includes('HeadOfDepartment') && (
                <button
                  onClick={handleDeleteAllSchedules}
                  className="bg-red-800 text-white px-3 py-1 rounded text-sm hover:bg-red-900 border-2 border-red-600"
                  title="⚠️ CẢNH BÁO: Xóa tất cả lịch học trong hệ thống!"
                >
                  🗑️ Xóa tất cả
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Error Display */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-md p-4 mb-6">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-red-800">
                Lỗi tải dữ liệu
              </h3>
              <div className="mt-2 text-sm text-red-700">
                {error}
              </div>
              <div className="mt-3">
                <button
                  onClick={loadSchedules}
                  className="bg-red-100 px-3 py-1 rounded text-sm text-red-800 hover:bg-red-200"
                >
                  Thử lại
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Table */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        <Table
          columns={columns}
          data={schedules}
          loading={loading}
          onRowClick={handleRowClick}
          selectedRows={selectedRows}
          onRowSelect={handleRowSelect}
        />

        {/* Pagination */}
        <Pagination
          currentPage={pagination.currentPage}
          totalPages={pagination.totalPages}
          totalRecords={pagination.totalRecords}
          recordsPerPage={pagination.recordsPerPage}
          onPageChange={handlePageChange}
        />
      </div>

      {/* Modal */}
      <ScheduleModal
        isOpen={modalState.isOpen}
        onClose={() => setModalState({ ...modalState, isOpen: false })}
        scheduleId={modalState.scheduleId}
        mode={modalState.mode}
        onSave={handleModalSave}
      />
    </div>
  );
}
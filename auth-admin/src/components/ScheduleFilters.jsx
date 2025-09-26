import React, { useState, useEffect } from 'react';

export default function ScheduleFilters({ 
  filters, 
  onFiltersChange, 
  onClearFilters,
  loading = false 
}) {
  const [localFilters, setLocalFilters] = useState(filters);

  useEffect(() => {
    setLocalFilters(filters);
  }, [filters]);

  const handleInputChange = (key, value) => {
    const newFilters = { ...localFilters, [key]: value };
    setLocalFilters(newFilters);
  };

  const handleApplyFilters = () => {
    onFiltersChange(localFilters);
  };

  const handleClearFilters = () => {
    const clearedFilters = {
      lecturerId: '',
      subjectCode: '',
      groupName: '',
      roomId: '',
      major: '',
      statusSlot: '',
      startDate: '',
      endDate: '',
    };
    setLocalFilters(clearedFilters);
    onClearFilters();
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter') {
      handleApplyFilters();
    }
  };

  return (
    <div className="bg-white p-6 rounded-lg shadow mb-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-medium text-gray-900">Bộ lọc tìm kiếm</h3>
        <div className="flex space-x-2">
          <button
            type="button"
            onClick={handleClearFilters}
            className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
          >
            <svg className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
            Xóa bộ lọc
          </button>
          <button
            type="button"
            onClick={handleApplyFilters}
            disabled={loading}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
          >
            <svg className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            {loading ? 'Đang tìm...' : 'Tìm kiếm'}
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {/* Lecturer ID */}
        <div>
          <label htmlFor="lecturerId" className="block text-sm font-medium text-gray-700 mb-1">
            Mã giảng viên
          </label>
          <input
            type="text"
            id="lecturerId"
            value={localFilters.lecturerId || ''}
            onChange={(e) => handleInputChange('lecturerId', e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="VD: GV001"
            className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
          />
        </div>

        {/* Subject Code */}
        <div>
          <label htmlFor="subjectCode" className="block text-sm font-medium text-gray-700 mb-1">
            Mã môn học
          </label>
          <input
            type="text"
            id="subjectCode"
            value={localFilters.subjectCode || ''}
            onChange={(e) => handleInputChange('subjectCode', e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="VD: PRN231"
            className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
          />
        </div>

        {/* Group Name */}
        <div>
          <label htmlFor="groupName" className="block text-sm font-medium text-gray-700 mb-1">
            Tên nhóm/lớp
          </label>
          <input
            type="text"
            id="groupName"
            value={localFilters.groupName || ''}
            onChange={(e) => handleInputChange('groupName', e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="VD: SE1801"
            className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
          />
        </div>

        {/* Room ID */}
        <div>
          <label htmlFor="roomId" className="block text-sm font-medium text-gray-700 mb-1">
            Mã phòng
          </label>
          <input
            type="text"
            id="roomId"
            value={localFilters.roomId || ''}
            onChange={(e) => handleInputChange('roomId', e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="VD: R301"
            className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
          />
        </div>

        {/* Major */}
        <div>
          <label htmlFor="major" className="block text-sm font-medium text-gray-700 mb-1">
            Chuyên ngành
          </label>
          <input
            type="text"
            id="major"
            value={localFilters.major || ''}
            onChange={(e) => handleInputChange('major', e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="VD: Software Engineering"
            className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
          />
        </div>

        {/* Status */}
        <div>
          <label htmlFor="statusSlot" className="block text-sm font-medium text-gray-700 mb-1">
            Trạng thái
          </label>
          <select
            id="statusSlot"
            value={localFilters.statusSlot || ''}
            onChange={(e) => handleInputChange('statusSlot', e.target.value)}
            className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
          >
            <option value="">Tất cả trạng thái</option>
            <option value="ACTIVE">Hoạt động</option>
            <option value="CANCELLED">Đã hủy</option>
            <option value="COMPLETED">Hoàn thành</option>
            <option value="PENDING">Chờ xử lý</option>
          </select>
        </div>

        {/* Start Date */}
        <div>
          <label htmlFor="startDate" className="block text-sm font-medium text-gray-700 mb-1">
            Từ ngày
          </label>
          <input
            type="date"
            id="startDate"
            value={localFilters.startDate || ''}
            onChange={(e) => handleInputChange('startDate', e.target.value)}
            className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
          />
        </div>

        {/* End Date */}
        <div>
          <label htmlFor="endDate" className="block text-sm font-medium text-gray-700 mb-1">
            Đến ngày
          </label>
          <input
            type="date"
            id="endDate"
            value={localFilters.endDate || ''}
            onChange={(e) => handleInputChange('endDate', e.target.value)}
            className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
          />
        </div>
      </div>
    </div>
  );
}
import React, { useState, useEffect } from 'react';
import { scheduleApi } from '../lib/scheduleApi.js';

export default function StatsDashboard({ filters = {} }) {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadStats();
  }, [filters]);

  const loadStats = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await scheduleApi.getStats(filters);
      setStats(response.data);
    } catch (err) {
      setError(err.message);
      console.error('Failed to load stats:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
        {[1, 2, 3, 4].map(i => (
          <div key={i} className="bg-white overflow-hidden shadow rounded-lg animate-pulse">
            <div className="p-5">
              <div className="flex items-center">
                <div className="flex-shrink-0">
                  <div className="h-8 w-8 bg-gray-200 rounded"></div>
                </div>
                <div className="ml-5 w-0 flex-1">
                  <div className="h-4 bg-gray-200 rounded mb-2"></div>
                  <div className="h-6 bg-gray-200 rounded"></div>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-md p-4 mb-6">
        <div className="flex">
          <div className="flex-shrink-0">
            <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-red-800">
              Lỗi tải thống kê
            </h3>
            <div className="mt-2 text-sm text-red-700">
              {error}
            </div>
          </div>
        </div>
      </div>
    );
  }

  const statItems = [
    {
      id: 'total',
      name: 'Tổng số lịch học',
      value: stats?.totalSchedules || 0,
      icon: (
        <svg className="h-8 w-8 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
      ),
      color: 'text-blue-600',
      bgColor: 'bg-blue-100',
    },
    {
      id: 'lecturers',
      name: 'Số giảng viên',
      value: stats?.uniqueLecturersCount || 0,
      icon: (
        <svg className="h-8 w-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
        </svg>
      ),
      color: 'text-green-600',
      bgColor: 'bg-green-100',
    },
    {
      id: 'subjects',
      name: 'Số môn học',
      value: stats?.uniqueSubjectsCount || 0,
      icon: (
        <svg className="h-8 w-8 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
        </svg>
      ),
      color: 'text-purple-600',
      bgColor: 'bg-purple-100',
    },
    {
      id: 'rooms',
      name: 'Số phòng học',
      value: stats?.uniqueRoomsCount || 0,
      icon: (
        <svg className="h-8 w-8 text-yellow-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
        </svg>
      ),
      color: 'text-yellow-600',
      bgColor: 'bg-yellow-100',
    },
  ];

  return (
    <div className="mb-6">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
        {statItems.map((item) => (
          <div key={item.id} className="bg-white overflow-hidden shadow rounded-lg">
            <div className="p-5">
              <div className="flex items-center">
                <div className="flex-shrink-0">
                  <div className={`p-2 ${item.bgColor} rounded-md`}>
                    {item.icon}
                  </div>
                </div>
                <div className="ml-5 w-0 flex-1">
                  <dl>
                    <dt className="text-sm font-medium text-gray-500 truncate">
                      {item.name}
                    </dt>
                    <dd className={`text-lg font-medium ${item.color}`}>
                      {item.value.toLocaleString('vi-VN')}
                    </dd>
                  </dl>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Status Breakdown */}
      {stats?.statusBreakdown && Object.keys(stats.statusBreakdown).length > 0 && (
        <div className="bg-white shadow rounded-lg p-6">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Phân bố trạng thái</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {Object.entries(stats.statusBreakdown).map(([status, count]) => {
              const statusInfo = {
                ACTIVE: { label: 'Hoạt động', color: 'text-green-600', bgColor: 'bg-green-100' },
                CANCELLED: { label: 'Đã hủy', color: 'text-red-600', bgColor: 'bg-red-100' },
                COMPLETED: { label: 'Hoàn thành', color: 'text-blue-600', bgColor: 'bg-blue-100' },
                PENDING: { label: 'Chờ xử lý', color: 'text-yellow-600', bgColor: 'bg-yellow-100' },
              };
              
              const info = statusInfo[status] || { label: status, color: 'text-gray-600', bgColor: 'bg-gray-100' };
              
              return (
                <div key={status} className={`${info.bgColor} rounded-lg p-3 text-center`}>
                  <div className={`text-2xl font-bold ${info.color}`}>
                    {count.toLocaleString('vi-VN')}
                  </div>
                  <div className="text-sm text-gray-600 mt-1">
                    {info.label}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
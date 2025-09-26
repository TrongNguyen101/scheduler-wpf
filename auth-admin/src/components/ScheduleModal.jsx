import React, { useState, useEffect } from 'react';
import { scheduleApi, formatters } from '../lib/scheduleApi.js';

export default function ScheduleModal({ 
  isOpen, 
  onClose, 
  scheduleId, 
  mode = 'view', // 'view', 'edit'
  onSave = null 
}) {
  const [schedule, setSchedule] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [editedSchedule, setEditedSchedule] = useState(null);

  useEffect(() => {
    if (isOpen && scheduleId) {
      loadSchedule();
    }
  }, [isOpen, scheduleId]);

  const loadSchedule = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await scheduleApi.getScheduleById(scheduleId);
      setSchedule(response.data);
      setEditedSchedule(response.data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!editedSchedule) return;

    try {
      setSaving(true);
      setError(null);
      
      const response = await scheduleApi.updateSchedule(scheduleId, editedSchedule);
      setSchedule(response.data);
      
      if (onSave) {
        onSave(response.data);
      }
      
      onClose();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleInputChange = (field, value) => {
    setEditedSchedule(prev => ({
      ...prev,
      [field]: value
    }));
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" onClick={onClose}></div>

        <span className="hidden sm:inline-block sm:align-middle sm:h-screen">&#8203;</span>

        <div className="relative inline-block align-bottom bg-white rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full sm:p-6">
          <div className="absolute top-0 right-0 pt-4 pr-4">
            <button
              type="button"
              className="bg-white rounded-md text-gray-400 hover:text-gray-600 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              onClick={onClose}
            >
              <span className="sr-only">Đóng</span>
              <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div className="sm:flex sm:items-start">
            <div className="w-full">
              <div className="mb-4">
                <h3 className="text-lg leading-6 font-medium text-gray-900">
                  {mode === 'edit' ? 'Chỉnh sửa lịch học' : 'Chi tiết lịch học'}
                </h3>
              </div>

              {loading && (
                <div className="flex justify-center py-12">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
                </div>
              )}

              {error && (
                <div className="bg-red-50 border border-red-200 rounded-md p-4 mb-4">
                  <div className="text-sm text-red-700">{error}</div>
                </div>
              )}

              {schedule && (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  {/* Schedule ID */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Mã lịch học
                    </label>
                    <div className="text-sm text-gray-900 bg-gray-50 px-3 py-2 rounded">
                      {schedule.scheduleId}
                    </div>
                  </div>

                  {/* Group Name */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Nhóm/Lớp
                    </label>
                    {mode === 'edit' ? (
                      <input
                        type="text"
                        value={editedSchedule?.groupName || ''}
                        onChange={(e) => handleInputChange('groupName', e.target.value)}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      />
                    ) : (
                      <div className="text-sm text-gray-900">{schedule.groupName}</div>
                    )}
                  </div>

                  {/* Subject Code */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Mã môn học
                    </label>
                    {mode === 'edit' ? (
                      <input
                        type="text"
                        value={editedSchedule?.subjectCode || ''}
                        onChange={(e) => handleInputChange('subjectCode', e.target.value)}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      />
                    ) : (
                      <div className="text-sm text-gray-900">{schedule.subjectCode}</div>
                    )}
                  </div>

                  {/* Date */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Ngày học
                    </label>
                    {mode === 'edit' ? (
                      <input
                        type="date"
                        value={editedSchedule?.date ? new Date(editedSchedule.date).toISOString().split('T')[0] : ''}
                        onChange={(e) => handleInputChange('date', e.target.value)}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      />
                    ) : (
                      <div className="text-sm text-gray-900">{formatters.formatDate(schedule.date)}</div>
                    )}
                  </div>

                  {/* Slot Time */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Thời gian
                    </label>
                    {mode === 'edit' ? (
                      <input
                        type="text"
                        value={editedSchedule?.slotTime || ''}
                        onChange={(e) => handleInputChange('slotTime', e.target.value)}
                        placeholder="VD: 07:30-09:00"
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      />
                    ) : (
                      <div className="text-sm text-gray-900">{schedule.slotTime}</div>
                    )}
                  </div>

                  {/* Room Name */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Phòng học
                    </label>
                    {mode === 'edit' ? (
                      <input
                        type="text"
                        value={editedSchedule?.roomName || ''}
                        onChange={(e) => handleInputChange('roomName', e.target.value)}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      />
                    ) : (
                      <div className="text-sm text-gray-900">{schedule.roomName}</div>
                    )}
                  </div>

                  {/* Session No */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Buổi học
                    </label>
                    {mode === 'edit' ? (
                      <input
                        type="number"
                        min="1"
                        value={editedSchedule?.sessionNo || ''}
                        onChange={(e) => handleInputChange('sessionNo', parseInt(e.target.value))}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      />
                    ) : (
                      <div className="text-sm text-gray-900">Buổi {schedule.sessionNo}</div>
                    )}
                  </div>

                  {/* Lecturer Name */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Giảng viên
                    </label>
                    {mode === 'edit' ? (
                      <input
                        type="text"
                        value={editedSchedule?.lecturerName || ''}
                        onChange={(e) => handleInputChange('lecturerName', e.target.value)}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      />
                    ) : (
                      <div className="text-sm text-gray-900">{schedule.lecturerName}</div>
                    )}
                  </div>

                  {/* Slot Type */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Loại buổi học
                    </label>
                    {mode === 'edit' ? (
                      <select
                        value={editedSchedule?.slotTypeCode || ''}
                        onChange={(e) => handleInputChange('slotTypeCode', e.target.value)}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      >
                        <option value="THEORY">Lý thuyết</option>
                        <option value="LAB">Thực hành</option>
                        <option value="PRACTICE">Thực hành</option>
                      </select>
                    ) : (
                      <div className="text-sm text-gray-900">{formatters.formatSlotType(schedule.slotTypeCode)}</div>
                    )}
                  </div>

                  {/* Status */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Trạng thái
                    </label>
                    {mode === 'edit' ? (
                      <select
                        value={editedSchedule?.statusSlot || ''}
                        onChange={(e) => handleInputChange('statusSlot', e.target.value)}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      >
                        <option value="ACTIVE">Hoạt động</option>
                        <option value="CANCELLED">Đã hủy</option>
                        <option value="COMPLETED">Hoàn thành</option>
                        <option value="PENDING">Chờ xử lý</option>
                      </select>
                    ) : (
                      <div className="text-sm text-gray-900">
                        <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${formatters.getStatusColor(schedule.statusSlot)}`}>
                          {formatters.formatStatus(schedule.statusSlot)}
                        </span>
                      </div>
                    )}
                  </div>

                  {/* Part of Day */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Buổi trong ngày
                    </label>
                    {mode === 'edit' ? (
                      <select
                        value={editedSchedule?.partOfDay || ''}
                        onChange={(e) => handleInputChange('partOfDay', e.target.value)}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      >
                        <option value="MORNING">Sáng</option>
                        <option value="AFTERNOON">Chiều</option>
                        <option value="EVENING">Tối</option>
                      </select>
                    ) : (
                      <div className="text-sm text-gray-900">{formatters.formatPartOfDay(schedule.partOfDay)}</div>
                    )}
                  </div>

                  {/* Major */}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Chuyên ngành
                    </label>
                    {mode === 'edit' ? (
                      <input
                        type="text"
                        value={editedSchedule?.major || ''}
                        onChange={(e) => handleInputChange('major', e.target.value)}
                        className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                      />
                    ) : (
                      <div className="text-sm text-gray-900">{schedule.major}</div>
                    )}
                  </div>

                  {/* Created/Updated info */}
                  <div className="md:col-span-2 pt-4 border-t">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs text-gray-500">
                      <div>
                        <strong>Được tạo:</strong> {formatters.formatDateTime(schedule.createdAt)}
                      </div>
                      <div>
                        <strong>Cập nhật cuối:</strong> {formatters.formatDateTime(schedule.updatedAt)}
                      </div>
                      {schedule.uploadedBy && (
                        <div>
                          <strong>Upload bởi:</strong> {schedule.uploadedBy}
                        </div>
                      )}
                      {schedule.uploadBatch && (
                        <div>
                          <strong>Batch ID:</strong> {schedule.uploadBatch}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              )}

              {/* Actions */}
              {schedule && (
                <div className="mt-6 flex justify-end space-x-3">
                  <button
                    type="button"
                    onClick={onClose}
                    className="bg-white py-2 px-4 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                  >
                    {mode === 'edit' ? 'Hủy' : 'Đóng'}
                  </button>
                  {mode === 'edit' && (
                    <button
                      type="button"
                      onClick={handleSave}
                      disabled={saving}
                      className="inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
                    >
                      {saving ? 'Đang lưu...' : 'Lưu thay đổi'}
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
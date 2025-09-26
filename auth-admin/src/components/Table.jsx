import React from 'react';

export default function Table({ 
  columns, 
  data, 
  loading = false, 
  onRowClick = null,
  selectedRows = new Set(),
  onRowSelect = null,
  className = ""
}) {
  if (loading) {
    return (
      <div className="animate-pulse">
        <div className="h-12 bg-gray-200 rounded mb-4"></div>
        {[1, 2, 3, 4, 5].map(i => (
          <div key={i} className="h-8 bg-gray-100 rounded mb-2"></div>
        ))}
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
        <h3 className="mt-2 text-sm font-medium text-gray-900">Không có dữ liệu</h3>
        <p className="mt-1 text-sm text-gray-500">Không tìm thấy dữ liệu phù hợp với bộ lọc hiện tại.</p>
      </div>
    );
  }

  return (
    <div className={`overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg ${className}`}>
      <table className="min-w-full divide-y divide-gray-300">
        <thead className="bg-gray-50">
          <tr>
            {onRowSelect && (
              <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-12">
                <input
                  type="checkbox"
                  className="focus:ring-indigo-500 h-4 w-4 text-indigo-600 border-gray-300 rounded"
                  checked={data.length > 0 && data.every(item => selectedRows.has(item._id || item.scheduleId))}
                  onChange={(e) => {
                    if (e.target.checked) {
                      data.forEach(item => onRowSelect(item._id || item.scheduleId, true));
                    } else {
                      data.forEach(item => onRowSelect(item._id || item.scheduleId, false));
                    }
                  }}
                />
              </th>
            )}
            {columns.map((column, index) => (
              <th
                key={index}
                scope="col"
                className={`px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider ${column.className || ''}`}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="bg-white divide-y divide-gray-200">
          {data.map((item, rowIndex) => (
            <tr
              key={item._id || item.scheduleId || rowIndex}
              className={`${
                onRowClick ? 'hover:bg-gray-50 cursor-pointer' : ''
              } ${selectedRows.has(item._id || item.scheduleId) ? 'bg-indigo-50' : ''}`}
              onClick={() => onRowClick && onRowClick(item)}
            >
              {onRowSelect && (
                <td className="px-6 py-4 whitespace-nowrap w-12">
                  <input
                    type="checkbox"
                    className="focus:ring-indigo-500 h-4 w-4 text-indigo-600 border-gray-300 rounded"
                    checked={selectedRows.has(item._id || item.scheduleId)}
                    onChange={(e) => {
                      e.stopPropagation();
                      onRowSelect(item._id || item.scheduleId, e.target.checked);
                    }}
                  />
                </td>
              )}
              {columns.map((column, colIndex) => (
                <td key={colIndex} className={`px-6 py-4 whitespace-nowrap ${column.cellClassName || ''}`}>
                  {column.render ? column.render(item, item[column.key]) : (item[column.key] || '-')}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
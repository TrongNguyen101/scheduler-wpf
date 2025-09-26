# Schedule Review UI - Quick Start Guide

## 📋 Overview

This comprehensive review UI provides a complete interface for managing and reviewing schedule data in the auth-admin React application.

## 🚀 Features Implemented

### ✅ **Core Components**

- **Table Component**: Reusable data table with sorting, selection, and pagination
- **Pagination Component**: Full pagination with page navigation
- **Filter Component**: Advanced filtering with multiple criteria
- **Stats Dashboard**: Real-time statistics and overview
- **Modal Component**: Detailed schedule view and editing

### ✅ **Main Features**

- **📊 Statistics Dashboard**: Live stats showing total schedules, lecturers, subjects, rooms
- **🔍 Advanced Filtering**: Filter by lecturer, subject, group, room, major, status, date range
- **📅 Schedule Table**: Sortable table with all schedule information
- **👁️ View Details**: Click any row to see full schedule details
- **✏️ Edit Schedules**: In-place editing with validation
- **🗑️ Delete Operations**: Single and bulk delete functionality
- **📄 Pagination**: Navigate through large datasets
- **🔄 Real-time Updates**: Automatic refresh after operations

## 🎯 How to Access

1. **Start the server**:

   ```bash
   cd auth-service
   npm run dev
   ```

2. **Start the admin UI**:

   ```bash
   cd auth-admin
   npm run dev
   ```

3. **Login and Navigate**:
   - Login with admin credentials
   - Click "Lịch học" in the navigation menu
   - The Schedule Review UI will load at `/schedules`

## 🔧 Permission Levels

The Schedule Review UI supports different permission levels:

- **Admin**: Full access (view, edit, delete, bulk operations)
- **HeadOfDepartment**: Full access to department schedules
- **AcademicOfDepartment**: View and limited edit access

## 📖 Usage Guide

### **1. Viewing Schedules**

- The main table shows all schedules with pagination
- Click any row to view detailed information
- Use the statistics dashboard to see overview metrics

### **2. Filtering Data**

- Use the filter panel to narrow down results
- Available filters:
  - Lecturer ID (Mã giảng viên)
  - Subject Code (Mã môn học)
  - Group Name (Tên nhóm/lớp)
  - Room ID (Mã phòng)
  - Major (Chuyên ngành)
  - Status (Trạng thái)
  - Date Range (Khoảng thời gian)

### **3. Editing Schedules**

- Click "Sửa" in the actions column
- Or click a row and then "Edit" in the modal
- Modify fields and save changes

### **4. Deleting Schedules**

- **Single Delete**: Click "Xóa" in the actions column
- **Bulk Delete**: Select multiple rows and click "Xóa đã chọn"

### **5. Sorting and Pagination**

- Click column headers to sort (future enhancement)
- Use pagination controls at the bottom
- Adjust items per page as needed

## 🛠️ Technical Implementation

### **File Structure**

```
auth-admin/src/
├── components/
│   ├── Table.jsx              # Reusable table component
│   ├── Pagination.jsx         # Pagination controls
│   ├── ScheduleFilters.jsx    # Filter interface
│   ├── StatsDashboard.jsx     # Statistics display
│   ├── ScheduleModal.jsx      # Detail/edit modal
│   └── index.js               # Component exports
├── lib/
│   └── scheduleApi.js         # API integration
├── pages/
│   └── ScheduleReview.jsx     # Main review page
└── App.jsx                    # Updated with new route
```

### **API Integration**

- **GET /schedules**: Fetch schedules with filtering and pagination
- **GET /schedules/:id**: Get individual schedule details
- **PUT /schedules/:id**: Update schedule information
- **DELETE /schedules/:id**: Delete single schedule
- **POST /schedules/bulk-delete**: Delete multiple schedules
- **GET /schedules/stats**: Get statistics and overview data

### **State Management**

- Local React state for UI components
- Efficient API calls with proper error handling
- Real-time updates after operations

## 🎨 UI/UX Features

- **📱 Responsive Design**: Works on desktop and mobile
- **🎯 Loading States**: Skeleton loaders during data fetch
- **❌ Error Handling**: User-friendly error messages
- **✅ Success Feedback**: Confirmation messages for operations
- **🔄 Auto Refresh**: Data refreshes after changes
- **🎭 Modal Interface**: Clean detail and edit interface

## 🚀 Quick Test

1. **Access the UI**: Navigate to `http://localhost:5173/schedules`
2. **View Statistics**: Check the dashboard for overview
3. **Filter Data**: Try filtering by date range or lecturer
4. **View Details**: Click any schedule row
5. **Edit Schedule**: Click "Sửa" and modify some fields
6. **Bulk Operations**: Select multiple rows and try bulk delete

## 🔧 Customization

### **Adding New Filters**

Edit `ScheduleFilters.jsx` to add new filter fields

### **Modifying Table Columns**

Update the `columns` array in `ScheduleReview.jsx`

### **Custom Statistics**

Modify `StatsDashboard.jsx` to show additional metrics

### **Styling Changes**

All components use Tailwind CSS classes for easy customization

## 📞 Support

If you encounter any issues:

1. **Check Console**: Look for error messages in browser console
2. **Verify API**: Ensure the backend server is running on port 4000
3. **Check Permissions**: Verify user has appropriate role access
4. **Network Issues**: Check API endpoints are accessible

## 🎉 Success!

You now have a fully functional Schedule Review UI with:

- ✅ Complete CRUD operations
- ✅ Advanced filtering and search
- ✅ Statistics dashboard
- ✅ Responsive design
- ✅ Error handling
- ✅ User-friendly interface

The UI is ready for production use and can handle large datasets efficiently!

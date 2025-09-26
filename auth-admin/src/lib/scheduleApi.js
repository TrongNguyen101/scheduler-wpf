import { getAccessToken } from "./auth.js";

const API_BASE_URL = "http://localhost:4000";

// Helper function to make authenticated API calls
async function apiCall(endpoint, options = {}) {
  const token = getAccessToken();

  const config = {
    headers: {
      "Content-Type": "application/json",
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options.headers,
    },
    ...options,
  };

  const response = await fetch(`${API_BASE_URL}${endpoint}`, config);

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(
      errorData.error?.message ||
        `HTTP ${response.status}: ${response.statusText}`
    );
  }

  return response.json();
}

// Schedule API functions
export const scheduleApi = {
  // Get schedules with filters and pagination
  async getSchedules(params = {}) {
    const queryParams = new URLSearchParams();

    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== "") {
        queryParams.append(key, value);
      }
    });

    const queryString = queryParams.toString();
    const endpoint = `/schedules${queryString ? `?${queryString}` : ""}`;

    return apiCall(endpoint);
  },

  // Get schedule by ID
  async getScheduleById(id) {
    return apiCall(`/schedules/${id}`);
  },

  // Update schedule
  async updateSchedule(id, data) {
    return apiCall(`/schedules/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  // Delete schedule
  async deleteSchedule(id) {
    return apiCall(`/schedules/${id}`, {
      method: "DELETE",
    });
  },

  // Bulk upload schedules
  async bulkUpload(schedules, options = {}) {
    return apiCall("/schedules/upload", {
      method: "POST",
      body: JSON.stringify({
        schedules,
        overwriteExisting: options.overwriteExisting || false,
        validateOnly: options.validateOnly || false,
      }),
    });
  },

  // Get schedule statistics
  async getStats(filters = {}) {
    const queryParams = new URLSearchParams();

    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== "") {
        queryParams.append(key, value);
      }
    });

    const queryString = queryParams.toString();
    const endpoint = `/schedules/stats${queryString ? `?${queryString}` : ""}`;

    return apiCall(endpoint);
  },

  // Bulk delete schedules
  async bulkDelete(options = {}) {
    return apiCall("/schedules/bulk-delete", {
      method: "POST",
      body: JSON.stringify(options),
    });
  },

  // Delete ALL schedules - DANGER: This will remove all schedules
  async deleteAllSchedules() {
    return apiCall("/schedules/all", {
      method: "DELETE",
    });
  },
};

// Helper functions for formatting
export const formatters = {
  formatDate(dateString) {
    if (!dateString) return "";
    try {
      return new Date(dateString).toLocaleDateString("vi-VN");
    } catch {
      return dateString;
    }
  },

  formatDateTime(dateString) {
    if (!dateString) return "";
    try {
      return new Date(dateString).toLocaleString("vi-VN");
    } catch {
      return dateString;
    }
  },

  formatPartOfDay(partOfDay) {
    const mapping = {
      MORNING: "Sáng",
      AFTERNOON: "Chiều",
      EVENING: "Tối",
    };
    return mapping[partOfDay] || partOfDay;
  },

  formatSlotType(slotType) {
    const mapping = {
      THEORY: "Lý thuyết",
      LAB: "Thực hành",
      PRACTICE: "Thực hành",
    };
    return mapping[slotType] || slotType;
  },

  formatStatus(status) {
    const mapping = {
      ACTIVE: "Hoạt động",
      CANCELLED: "Đã hủy",
      COMPLETED: "Hoàn thành",
      PENDING: "Chờ xử lý",
    };
    return mapping[status] || status;
  },

  getStatusColor(status) {
    const colors = {
      ACTIVE: "bg-green-100 text-green-800",
      CANCELLED: "bg-red-100 text-red-800",
      COMPLETED: "bg-blue-100 text-blue-800",
      PENDING: "bg-yellow-100 text-yellow-800",
    };
    return colors[status] || "bg-gray-100 text-gray-800";
  },
};

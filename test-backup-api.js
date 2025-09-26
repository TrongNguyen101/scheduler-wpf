// Test script để kiểm tra delete backup API
const API_BASE = "http://localhost:4000";

// Mock data để test
const testData = {
  accessToken: "your-test-token", // Thay bằng token thực
  filename: "test-backup.db"
};

// Hàm test delete backup
const testDeleteBackup = async () => {
  try {
    console.log("🧪 Testing DELETE backup API...");
    
    const response = await fetch(`${API_BASE}/api/backups/${encodeURIComponent(testData.filename)}`, {
      method: "DELETE",
      headers: {
        "Authorization": `Bearer ${testData.accessToken}`,
        "Content-Type": "application/json"
      }
    });

    const result = await response.json();
    
    console.log("📊 Response status:", response.status);
    console.log("📋 Response data:", result);
    
    if (response.ok && result.success) {
      console.log("✅ DELETE backup API works correctly!");
    } else {
      console.log("❌ DELETE backup API failed:", result.message);
    }
    
  } catch (error) {
    console.error("💥 Error testing DELETE backup API:", error);
  }
};

// Hàm test list backup
const testListBackups = async () => {
  try {
    console.log("🧪 Testing LIST backups API...");
    
    const response = await fetch(`${API_BASE}/api/backups/list`, {
      headers: {
        "Authorization": `Bearer ${testData.accessToken}`,
        "Content-Type": "application/json"
      }
    });

    const result = await response.json();
    
    console.log("📊 Response status:", response.status);
    console.log("📋 Response data:", result);
    
    if (response.ok && result.success) {
      console.log("✅ LIST backups API works correctly!");
      console.log(`📁 Found ${result.data?.length || 0} backup files`);
    } else {
      console.log("❌ LIST backups API failed:", result.message);
    }
    
  } catch (error) {
    console.error("💥 Error testing LIST backups API:", error);
  }
};

// Chạy tests
console.log("🚀 Starting backup API tests...");
console.log("ℹ️  Make sure backend server is running on", API_BASE);
console.log("ℹ️  Update accessToken in this file with a valid token");
console.log("=====================================");

// Uncomment các dòng dưới để chạy test khi có token hợp lệ
// testListBackups();
// testDeleteBackup();
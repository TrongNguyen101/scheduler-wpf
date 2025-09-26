// Test login API để kiểm tra cấu trúc response
async function testLogin() {
  try {
    const response = await fetch("http://localhost:4000/auth/login", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        username: "admin",
        password: "admin123",
      }),
    });

    const data = await response.json();
    console.log("Response status:", response.status);
    console.log("Response data:", JSON.stringify(data, null, 2));

    if (data.success) {
      console.log("Login successful!");
      console.log(
        "Access token:",
        data.data.accessToken ? "Present" : "Missing"
      );
      console.log("User data:", data.data.user);
      console.log("User roles:", data.data.user.roles);
    } else {
      console.log("Login failed:", data.error?.message);
    }
  } catch (error) {
    console.error("Request failed:", error.message);
  }
}

// Chạy test nếu được gọi trực tiếp
if (typeof window !== "undefined") {
  console.log("Testing login API...");
  testLogin();
}

export { testLogin };

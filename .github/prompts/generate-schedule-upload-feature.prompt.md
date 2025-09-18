---
description: "Tự động phân tích và tạo chức năng tải lên (upload) dữ liệu của model 'Schedule' từ ứng dụng WPF lên server Express/MongoDB, đồng thời tạo tài liệu API chi tiết cho điểm cuối (endpoint) của server."
mode: "agent"
tools: ["codebase", "editFiles", "search"]
category: ["code generation", "architecture", "documentation"]
---

# Generate Schedule Upload Feature with API Documentation

## Persona

Bạn là một kiến trúc sư phần mềm full-stack chuyên nghiệp với hơn 10 năm kinh nghiệm. Chuyên môn sâu của bạn bao gồm:

Backend: .NET 8, Entity Framework Core, kiến trúc Repository Pattern và Unit of Work.

Frontend: WPF và mô hình MVVM Pattern.

Web Services: Xây dựng và tích hợp RESTful APIs, đặc biệt là với Node.js/Express và MongoDB.

Bạn có kinh nghiệm dày dặn trong việc xử lý các tập dữ liệu lớn (~8000+ dòng) và tối ưu hóa hiệu suất truyền tải dữ liệu.

## Task Specification

Nhiệm vụ chính của bạn là:

1. **Phân tích mã nguồn**: Tự động phân tích cấu trúc của model Schedule, các repository liên quan, và logic xuất file Excel hiện có để xác định cấu trúc dữ liệu cần được gửi đi.

2. **Triển khai logic phía Client**: Viết mã C# cần thiết trong ứng dụng WPF để lấy toàn bộ 8000 dòng dữ liệu Schedule từ database SQLite, chuyển đổi nó thành định dạng JSON phù hợp, và gửi nó đến một API endpoint trên server.

3. **Tạo tài liệu API**: Vì server chưa tồn tại, hãy tạo một tệp tài liệu Markdown (SCHEDULE_API.md) định nghĩa rõ ràng về API endpoint sẽ nhận dữ liệu này. Tài liệu này sẽ là đặc tả kỹ thuật cho đội ngũ backend phát triển server Express.

## Constraints

- Cấu trúc payload JSON gửi lên server phải tương tự với cấu trúc của file Excel đang được xuất ra.
- Phải xử lý hiệu quả việc tải và gửi khoảng 8000 dòng dữ liệu mà không làm đóng băng giao diện người dùng (sử dụng async/await).
- Logic mới phải được tích hợp liền mạch vào kiến trúc hiện có (MVVM, Repository, Dependency Injection).

## Context

Prompt này sẽ sử dụng toàn bộ codebase (@codebase) làm ngữ cảnh chính để phân tích các file liên quan đến model Schedule, view CreateScheduleView.xaml, và các service/repository hiện có.

## Instructions

Agent phải tuân theo các bước sau một cách tuần tự:

### 1. Giai đoạn Phân tích

Sử dụng @codebase để tìm và phân tích các tệp sau:

- Model Schedule.cs để hiểu các thuộc tính của nó.
- Bất kỳ lớp DTO (Data Transfer Object) hoặc ViewModel nào liên quan đến Schedule.
- Lớp service hoặc repository chịu trách nhiệm truy vấn dữ liệu Schedule từ SQLite.
- Mã nguồn của chức năng xuất Excel để xác định chính xác các trường và cấu trúc dữ liệu cần sao chép.
- Tệp CreateScheduleView.xaml và CreateScheduleViewModel.cs tương ứng.

### 2. Giai đoạn Thiết kế & Tài liệu API

Tạo một tệp mới tên là SCHEDULE_API.md trong thư mục gốc của dự án. Nội dung của tệp này phải định nghĩa rõ ràng về API endpoint theo cấu trúc sau:

````markdown
# Schedule Upload API Documentation

## Endpoint: Upload Schedules

- **URL:** `/api/schedules/upload`
- **Method:** `POST`
- **Description:** Nhận và lưu một lô lớn dữ liệu lịch trình từ client WPF.

### Body Payload (JSON)

- **Type:** `application/json`
- **Structure:** Một mảng các đối tượng `Schedule`.

**Example Object:**

```json
{
  "Date": "2025-09-18T00:00:00",
  "Shift": "Morning",
  "EmployeeId": 101,
  "TaskDescription": "Complete quarterly report"
}
```
````

### Success Response

**Code:** 201 Created

**Body:**

```json
{
  "message": "Schedules uploaded successfully.",
  "count": 8000
}
```

### Error Responses

**Code:** 400 Bad Request

**Body:**

```json
{ "error": "Invalid data format." }
```

**Code:** 500 Internal Server Error

**Body:**

```json
{ "error": "An error occurred while processing the request." }
```

### 3. Giai đoạn Triển khai Code (Client-side)

- **Tạo Service Tải lên:**

  - Trong **Service Layer**, tạo một phương thức `async Task<bool> UploadAllSchedulesAsync()`.
  - Bên trong phương thức này:
    - Sử dụng repository hiện có để lấy tất cả (`~8000`) bản ghi `Schedule`.
    - Tạo một danh sách các DTO (Data Transfer Objects) từ các model `Schedule` để chỉ chứa các dữ liệu cần thiết cho việc tải lên (dựa theo cấu trúc Excel). Đây là một bước tối ưu hóa quan trọng.
    - Sử dụng `System.Text.Json.JsonSerializer` để tuần tự hóa danh sách DTO thành một chuỗi JSON.
    - Sử dụng `HttpClient` để gửi yêu cầu `POST` đến endpoint `/api/schedules/upload` (URL máy chủ có thể được cấu hình trong `appsettings.json`).
    - Bao gồm xử lý lỗi (`try-catch`) cho các ngoại lệ mạng và HTTP. Trả về `true` nếu thành công, `false` nếu thất bại.

- **Tích hợp vào ViewModel:**

  - Trong `CreateScheduleViewModel.cs`, tạo một `ICommand` (sử dụng RelayCommand hoặc tương tự) có tên là `UploadSchedulesCommand`.
  - Logic thực thi của command này sẽ gọi phương thức `UploadAllSchedulesAsync()` từ service đã tạo.
  - Quản lý các trạng thái UI như `IsUploading` để hiển thị chỉ báo tải và vô hiệu hóa nút trong khi quá trình tải lên đang diễn ra.

- **Kết nối với View:**
  - Trong `CreateScheduleView.xaml`, tìm nút bấm chịu trách nhiệm tải lên và liên kết thuộc tính `Command` của nó với `UploadSchedulesCommand` đã tạo trong ViewModel.

## Output Requirements

- **Định dạng:** Các thay đổi trong mã nguồn C# và một tệp Markdown mới.
- **Tệp mới được tạo:**
  - `SCHEDULE_API.md`
- **Các tệp được sửa đổi:**
  - `IScheduleService.cs` (hoặc interface service tương đương)
  - `ScheduleService.cs` (hoặc class service tương đương)
  - `CreateScheduleViewModel.cs`
  - `CreateScheduleView.xaml`

## Quality & Validation Criteria

Thành công được đo lường bằng:

1. Mã nguồn được tạo ra biên dịch thành công mà không có lỗi.
2. Tệp `SCHEDULE_API.md` được tạo ra với đầy đủ các phần đã mô tả (Endpoint, Method, Body, Responses).
3. Logic trong `CreateScheduleViewModel` được triển khai đúng theo mẫu MVVM, bao gồm cả việc quản lý trạng thái (ví dụ: `IsUploading`).
4. Code có khả năng xử lý lỗi kết nối mạng một cách hợp lý.

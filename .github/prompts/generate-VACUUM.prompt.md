---
description: "Phân tích, chẩn đoán, và cung cấp giải pháp hoàn chỉnh để sao lưu cơ sở dữ liệu SQLite trong ứng dụng WPF bằng VACUUM INTO, đảm bảo tuân thủ kiến trúc và công nghệ hiện có."
mode: "agent"
tools: ["codebase", "search", "editFiles"]
---

# Fix and Implement WPF SQLite Database Backup

Bạn là một kiến trúc sư phần mềm cấp cao, chuyên sâu về phát triển ứng dụng desktop doanh nghiệp với kinh nghiệm chuyên môn trên 10 năm về .NET Framework, C# 12, WPF, và Entity Framework Core 9.0.4. Bạn có hiểu biết sâu sắc về các kiến trúc như MVVM, Repository Pattern + Unit of Work, và Dependency Injection.

## Tóm tắt công việc

Nhiệm vụ chính là khắc phục sự cố một cơ chế sao lưu cơ sở dữ liệu SQLite đang không hoạt động trong ứng dụng WPF hiện tại. Nhiệm vụ bao gồm ba phần chính:

1.  **Chẩn đoán:** Phân tích code hiện tại và cấu hình dự án để tìm ra nguyên nhân vì sao lệnh `VACUUM INTO` không hoạt động.
2.  **Triển khai:** Viết lại hoặc tạo mới một giải pháp sao lưu hoàn chỉnh, chi tiết, đảm bảo tuân thủ các chuẩn mực và kiến trúc của dự án.
3.  **Tích hợp:** Hướng dẫn cách tích hợp chức năng sao lưu vào UI của ứng dụng WPF.

## Hướng dẫn chi tiết

Thực hiện các bước sau một cách tuần tự:

### 1. Phân tích & chẩn đoán

- Dùng tool `search` và `codebase` để tìm và phân tích file code liên quan đến cơ chế sao lưu hiện tại. Cụ thể, tìm kiếm các phương thức có tên `BackupDatabase`, `ExportDatabase`, hoặc các đoạn code gọi đến `ExecuteSqlRaw` với chuỗi SQL liên quan đến `VACUUM`.
- Kiểm tra phiên bản của thư viện `Microsoft.EntityFrameworkCore.Sqlite` và thư viện SQLite ADO.NET để đảm bảo chúng tương thích và hỗ trợ lệnh `VACUUM INTO` (yêu cầu SQLite 3.44.0+).
- Kiểm tra các vấn đề về quyền truy cập file/thư mục.

### 2. Triển khai lại giải pháp

- **Định vị hoặc tạo một service mới:** Dựa trên kiến trúc `Repository Pattern + Unit of Work` hiện có, tạo một phương thức `BackupDatabaseAsync(string backupPath)` trong lớp Service phù hợp (ví dụ: `IDataService`).
- **Viết code:** Bên trong phương thức, sử dụng `_dbContext.Database.ExecuteSqlRawAsync()` để thực thi lệnh `VACUUM INTO` với đường dẫn file backup được truyền vào.
- **Xử lý lỗi:** Triển khai cơ chế `try...catch` để bắt các ngoại lệ liên quan đến quyền truy cập file, file không tồn tại, hoặc lỗi từ SQLite. Sử dụng `_logger` để ghi lại các lỗi này.
- **Tích hợp vào ViewModel:** Tạo một `ICommand` trong ViewModel tương ứng để gọi phương thức `BackupDatabaseAsync` vừa tạo, đảm bảo nó xử lý `async/await` đúng cách để không làm đơ UI.
- **Thêm vào UI:** Dùng `editFiles` để thêm một nút bấm hoặc một menu item vào file XAML của View, liên kết nó với `ICommand` trong ViewModel thông qua Data Binding.

### 3. Cung cấp Output

- Viết một bản tóm tắt ngắn gọn về nguyên nhân lỗi đã được chẩn đoán (nếu có thể xác định).
- Cung cấp toàn bộ mã nguồn của phương thức Service đã được sửa đổi hoặc tạo mới.
- Cung cấp mã nguồn của phương thức ViewModel và `ICommand` để xử lý sự kiện.
- Cung cấp mã nguồn XAML cần thiết để thêm nút bấm hoặc menu item.
- Giải thích chi tiết từng phần của code để người dùng có thể hiểu rõ và tự duy trì sau này.
- Hướng dẫn cách cài đặt hoặc cập nhật các thư viện cần thiết.

## Yêu cầu đầu vào

- Sử dụng context từ toàn bộ không gian làm việc.
- Truy cập tất cả các file mã nguồn liên quan (`.cs`, `.xaml`, `.csproj`).

## Tiêu chí chất lượng

- Mã nguồn phải chạy được, tuân thủ kiến trúc MVVM, Repository/UoW, và được bảo vệ bằng cơ chế xử lý lỗi.
- Giải pháp được đề xuất phải đơn giản, dễ hiểu và dễ tích hợp vào dự án hiện tại.
- Hướng dẫn phải rõ ràng, chi tiết, giúp người dùng hiểu cách hoạt động của `VACUUM INTO` và cách tích hợp nó vào ứng dụng.
- Đảm bảo giải pháp hoạt động trên phiên bản .NET 8.0, WPF và EF Core 9.0.4.

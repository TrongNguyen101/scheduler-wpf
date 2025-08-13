### Kế hoạch kiểm thử hệ thống (System Test) — Scheduler WPF

#### 1) Mục tiêu
- Xác nhận ứng dụng lập lịch học hoạt động đúng chức năng trên Windows 11 (Acer Nitro 5), ổn định dữ liệu SQLite và không lỗi UI/luồng nghiệp vụ chính.

#### 2) Phạm vi và mô-đun
- UI WPF theo mô hình MVVM: `Views/*`, `ViewModel/*` (đặc biệt `CreateScheduleViewModel`).
- Nghiệp vụ/lớp dịch vụ: `ServiceRefactor/*` (Schedule, Room, Lecturer, Curriculum, GroupName, Notification).
- Tầng repo/EF Core SQLite: `Repository/*`, `Data/DataContext.cs`.
- Thuật toán tạo lịch: `Algorithm/*` (`CreateScheduleTree`, `Version2CreateSchedule`, `ScheduleCommonSubjectVersion3`, `RoomSchedulerOnOff`).
- Xuất Excel: `ScheduleServices.ExportToExcel` (Syncfusion XlsIO).

Ngoài phạm vi: cài đặt/giấy phép Syncfusion (được cấp qua appsettings), tối ưu/tùy chỉnh ngoài test.

#### 3) Giả định & ràng buộc
- Máy kiểm thử: Acer Nitro 5 Windows 11, CPU 6–8C/12–16T (ví dụ i5-11400H/i7-11800H), RAM ≥ 16GB, SSD NVMe, màn hình 1920×1080.
- .NET 8 Desktop runtime, SDK 8.x, Visual Studio 2022 hoặc `dotnet` CLI.
- Quyền ghi thư mục người dùng và thư mục dự án (tạo `AppData/app.db`, `logsTime`).
- Dữ liệu mẫu ban đầu có trong `AppData/app.db`. App có cơ chế reset/copy DB sang `%LOCALAPPDATA%/SchedulerApp/app.db`.

#### 4) Môi trường kiểm thử
- HĐH: Windows 11 23H2/24H2 x64.
- .NET SDK: 8.0.x.
- SQLite: đi kèm qua `Microsoft.EntityFrameworkCore.Sqlite`.
- Công cụ hỗ trợ: Visual Studio 2022, Event Viewer, PowerShell, bộ gõ tiếng Việt tắt khi nhập tìm kiếm.

Thiết lập nhanh:
1) Mở solution `SchedulerSolution/SchedulerWpfApp.sln` trong VS 2022.
2) Cấu hình startup project `SchedulerWpfApp`, x64.
3) Kiểm tra `appsettings.json` có key Syncfusion (đã cung cấp) và kết nối SQLite mặc định.

#### 5) Dữ liệu kiểm thử
- DB mặc định: `SchedulerSolution/SchedulerWpfApp/AppData/app.db` (được copy vào `%LOCALAPPDATA%/SchedulerApp/app.db` khi chọn “Xóa toàn bộ dữ liệu”).
- Tạo bộ dữ liệu lớn: thêm nhiều `GroupClass`, `Room`, `Lecturer`, `CurriculumSubject` để phục vụ test tải (tham khảo `Migrations/*`).
- Reset dữ liệu: trong UI, dùng “Xóa toàn bộ dữ liệu” để khôi phục app.db gốc.

#### 6) Ma trận kiểm thử chức năng chính

1) Khởi động ứng dụng và cấu hình
- Bước: Mở app. Kỳ vọng: Không crash, log tạo ở `logsTime/app_yyyyMMdd.log`. Syncfusion license hợp lệ (không hiện lỗi).

2) Tải dữ liệu lịch hiện có
- Bước: Vào màn “Tạo lịch”, danh sách lớp/phòng/giảng viên hiển thị. Kỳ vọng: `GroupNames/Rooms/Lecturers` khác rỗng nếu DB có dữ liệu.

3) Lọc theo tuần/lớp/phòng/giảng viên
- Bước: Chọn năm/tuần, chọn chế độ hiển thị CLASS/ROOM/LECTURER. Kỳ vọng: Lịch hiển thị đúng phạm vi ngày và bộ lọc tương ứng.

4) Tạo lịch mới (CreateSchedule)
- Tiền điều kiện: Chọn Major cho Group A và Group B, chọn ngày bắt đầu.
- Bước: Nhấn “Tạo lịch”. Kỳ vọng: Thanh tiến trình chạy, thông báo thành công, lịch sinh ra và hiển thị theo tuần tương ứng.
- Xác minh: Không tạo trùng phòng (OFF) cho cùng ngày/slot, không trùng giảng viên cho cùng ngày/slot.

5) Xuất Excel
- Bước: Chọn “Xuất Excel”, lưu file. Kỳ vọng: File `.xlsx` sinh ra, cột đúng như `ExportToExcel` định nghĩa.

6) Chỉnh sửa slot (Edit)
- Bước: Mở form chỉnh sửa một slot, đổi phòng/giảng viên/slot/time. Kỳ vọng: Ràng buộc kiểm tra trùng phòng/giảng viên hoạt động, lưu thành công cập nhật DB.

7) Kéo-thả hoán đổi (Drag & Drop)
- Bước: Kéo một slot sang ô trống/ô có slot khác, xác nhận nếu có popup hoán đổi. Kỳ vọng: Không vi phạm trùng phòng/giảng viên, UI và DB đồng bộ.

8) Tạo slot thủ công (Create Slot)
- Bước: Mở dialog tạo slot, nhập đủ trường bắt buộc. Kỳ vọng: Validate đầy đủ, tạo mới thành công, hiển thị đúng.

9) Xóa slot
- Bước: Chọn slot, xóa, xác nhận. Kỳ vọng: Slot biến mất khỏi UI và DB, reset identity được thực thi trong transaction.

10) Xóa toàn bộ dữ liệu
- Bước: Chọn “Xóa toàn bộ dữ liệu” và xác nhận. Kỳ vọng: DB người dùng bị thay thế bằng bản gốc, dữ liệu hiển thị lại theo bản mẫu.

11) Ổn định qua phiên làm việc
- Bước: Mở/đóng app nhiều lần, tạo/sửa/xóa lặp lại. Kỳ vọng: Không rò rỉ tài nguyên, không hỏng DB, log Trace không lỗi nghiêm trọng.

12) Lỗi ngoại lệ và thông báo
- Bước: Gây lỗi (mất file appsettings, mất app.db). Kỳ vọng: Thông báo lỗi thân thiện, không crash.

#### 7) Tiêu chí chấp nhận
- Không có crash hoặc lỗi không xử lý trong luồng chính.
- Tất cả ràng buộc trùng phòng/giảng viên hoạt động chính xác.
- Xuất Excel thành công với dữ liệu đã lọc.
- Tạo lịch/hoán đổi/chỉnh sửa cập nhật đúng DB và UI.

#### 8) Hướng dẫn test tự động (đề xuất)
- UI Automation: FlaUI (.NET) để tự động hóa mở app, click, chọn combobox, kéo-thả. Yêu cầu gán `AutomationProperties.AutomationId` cho control quan trọng.
- Unit/Integration: xUnit + EFCore InMemory/SQLite in-memory cho service/repository (mock `IUnitOfWork`, `INotificationService`).

#### 9) Quy ước ghi log và báo cáo lỗi
- Nhật ký: `logsTime/app_yyyyMMdd.log` (Trace). Gắn ID ca kiểm thử vào thông điệp log khi chạy script tự động.
- Báo cáo: tổng hợp Pass/Fail theo tính năng, đính kèm ảnh chụp màn hình (UI) và file Excel xuất ra.

#### 10) Ma trận truy vết (ví dụ rút gọn)
- Yêu cầu “Không trùng phòng OFF”: Test 4, 6, 7, 8.
- Yêu cầu “Không trùng giảng viên”: Test 4, 6, 7, 8.
- Yêu cầu “Xuất Excel”: Test 5.
- Yêu cầu “Reset dữ liệu”: Test 10.



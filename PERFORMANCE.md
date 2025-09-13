### Kế hoạch kiểm thử hiệu năng — Scheduler WPF trên Acer Nitro 5 (Windows 11)

#### 1) Mục tiêu
- Đo tốc độ tạo lịch, cập nhật/xóa hàng loạt, tải UI theo tuần, xuất Excel; xác định ngưỡng người dùng/dữ liệu trước khi cần tối ưu thuật toán hoặc I/O.

#### 2) Chỉ số theo dõi (KPI)
- Tạo lịch (CreateSchedule): tổng thời gian và số slot tạo; mục tiêu < 10 giây với 100–300 lớp/tuần, 8 slot/ngày, 2 phiên A/P; CPU < 70% trung bình, RAM tăng < 1.5GB.
- Xóa tất cả (DeleteAll): < 5 giây với 10k slot.
- Lọc/hiển thị tuần: chuyển tuần -> render bảng < 200 ms (không tính tải dữ liệu).
- Kéo-thả/Chỉnh sửa: phản hồi < 100 ms; lưu DB < 300 ms.
- Xuất Excel: < 5 giây cho 10k dòng.

#### 3) Dữ liệu tải (Profiles)
- Nhỏ: 20 lớp, 2 phòng/tòa, 10 giảng viên, 50 môn.
- Vừa: 100 lớp, 10 phòng, 40 giảng viên, 200 môn.
- Lớn: 300 lớp, 25 phòng, 100 giảng viên, 600 môn; lịch 10 tuần ~ hàng chục nghìn slot.

Tạo dữ liệu: dùng tính năng “Xóa toàn bộ dữ liệu” để copy DB gốc, sau đó thêm mới bằng tool nội bộ hoặc migration seed script (khuyến nghị viết tool seeding riêng dùng EF Core).

#### 4) Thiết lập đo đạc
- Bật Trace vào file: đã cấu hình trong `App.xaml.cs` tạo `logsTime/app_yyyyMMdd.log`.
- Dùng Event Tracing for Windows (ETW) hoặc Windows Performance Recorder (WPR) để lấy CPU, Disk, GC.
- Dùng Performance Monitor (perfmon) giám sát Memory, % Processor Time, Disk Queue Length khi chạy kịch bản.
- Bật Stopwatch trong code nơi trọng yếu nếu cần (đã có log ở `CreateScheduleViewModel.CreateScheduleDemo`).

#### 5) Kịch bản kiểm thử hiệu năng

Scenario A — Generate Schedules (nặng CPU/thuật toán)
1) Chọn tập majors lớn cho Group A/B, ngày bắt đầu mặc định.
2) Nhấn “Tạo lịch”, đo thời gian tổng từ log và GC/CPU từ WPR.
3) Kỳ vọng: thời gian < KPI; nếu vượt, xem xét tối ưu:
   - Giảm boxing/alloc, tái sử dụng collection trong `Version2CreateSchedule` và `ScheduleCommonSubjectVersion3`.
   - Dùng cấu trúc Lookup/Dictionary (đã có) cho tra cứu O(1).
   - Hạn chế LINQ lồng sâu trong vòng lặp lớn; chuyển sang for/foreach rõ ràng.
   - Batch insert: hiện add từng item trong transaction; cân nhắc `AddRangeAsync` trong repo.

Scenario B — Delete All (I/O DB)
1) Với DB lớn, chạy “Xóa tất cả dữ liệu”.
2) Đo thời gian và số transaction; kỳ vọng < 5 giây cho 10k slot.
3) Tối ưu gợi ý: xóa theo batch, dùng lệnh SQL trực tiếp `DELETE FROM Schedules; VACUUM;` và reset identity 1 lần.

Scenario C — Weekly View Filtering (UI)
1) Chọn tuần khác nhau, chuyển chế độ CLASS/ROOM/LECTURER.
2) Đo thời gian từ thay đổi combobox đến khi bảng render hoàn tất.
3) Tối ưu gợi ý: ảo hóa ItemsControl, tránh tạo đối tượng `TimetableCellViewModel` thừa, tránh `SlotRows.Clear()` rồi add từng cell nếu có thể batch update.

Scenario D — Drag & Drop/Update
1) Kéo-thả 100 lần liên tiếp giữa các ô, theo dõi latency và lỗi.
2) Tối ưu gợi ý: debounce lưu DB, hợp nhất cập nhật, kiểm tra xung đột trên tập con thay vì toàn bộ danh sách.

Scenario E — Export Excel
1) Xuất 10k dòng, đo thời gian và file size.
2) Tối ưu gợi ý: tắt auto-fit, tắt định dạng dư thừa, ghi theo mảng (bulk) thay vì ô lẻ.

#### 6) Quy trình đo tiêu chuẩn
- Warmup 1 lần trước khi ghi số liệu.
- Mỗi kịch bản chạy 5 lần, báo cáo trung bình, p95, p99 thời gian.
- Đóng app/clean `%LOCALAPPDATA%/SchedulerApp/app.db` nếu cần trở về snapshot đồng nhất.

#### 7) Rủi ro và biện pháp
- GC/Fragmentation: theo dõi Gen2 collections; nếu cao, xem lại cấp phát tạm thời lớn.
- UI Freeze: dùng async/await nơi I/O; thanh tiến trình đã có, tránh `Task.Delay` trong vòng lặp nếu không cần thiết ở release.
- SQLite contention: đảm bảo `Cache=Shared`, tránh quá nhiều transaction nhỏ.

#### 8) Báo cáo
- Bảng KPI theo profile dữ liệu; biểu đồ thời gian; ảnh chụp PerfMon/WPR; log extract từ `logsTime`.
- Nêu rõ commit hash, cấu hình máy (CPU/GPU/RAM/SSD, nền tảng driver/power plan), nhiệt độ nếu thermal throttling.



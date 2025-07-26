using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    /// <summary>
    /// Lớp thực hiện nhiệm vụ phân công giảng viên vào các lịch học đã xếp sẵn.
    /// Sử dụng chiến lược Greedy để tìm giảng viên phù hợp nhất cho toàn bộ các buổi học của một lớp-môn học,
    /// đảm bảo tính nhất quán và tuân thủ các ràng buộc phức tạp.
    /// </summary>
    public class LecturerAssignmentService
    {
        /// <summary>
        /// Lưu trữ trạng thái hiện tại của mỗi giảng viên trong quá trình phân công.
        /// Key là LecturerId, Value là đối tượng LecturerAssignmentState.
        /// </summary>
        private Dictionary<string, LecturerAssignmentState> _lecturerStateMap = new();

        /// <summary>
        /// Đối tượng logger để ghi lại các bước của thuật toán.
        /// Được cung cấp từ bên ngoài để tăng tính linh hoạt.
        /// </summary>
        private readonly ILogger<LecturerAssignmentService> _logger;

        /// <summary>
        /// Constructor của service.
        /// </summary>
        /// <param name="logger">Một đối tượng triển khai ILogger để sử dụng cho việc ghi log.</param>
        public LecturerAssignmentService(ILogger<LecturerAssignmentService> logger)
        {
            _logger = logger;
        }

        // <summary>
        /// Khởi tạo trạng thái ban đầu cho tất cả giảng viên từ danh sách đăng ký môn học.
        /// </summary>
        /// <param name="lecturerSubjects">Danh sách thông tin phân công giảng viên dạy các môn.</param>
        private void InitializeLecturerStates(List<LecturerSubject> lecturerSubjects)
        {
            _logger.LogInformation("Bắt đầu khởi tạo trạng thái giảng viên...");
            _lecturerStateMap = lecturerSubjects
                .GroupBy(ls => ls.LecturerId) // 1. Nhóm tất cả các bản ghi phân công theo ID của giảng viên.
                .ToDictionary(                // 2. Chuyển đổi mỗi nhóm thành một cặp Key-Value cho Dictionary.
                    g => g.Key,               // Key của Dictionary là ID của giảng viên.
                    g => {
                        var firstRecord = g.First(); // Lấy một bản ghi đại diện để lấy thông tin chung.
                        var state = new LecturerAssignmentState  // Value là một đối tượng LecturerAssignmentState được tạo ra từ thông tin của nhóm.
                        {
                            LecturerId = g.Key,
                            LecturerName = firstRecord.LecturerName ?? "",
                            LecturerAccount = firstRecord.Lecturer.LecturerAccount ?? "",
                        };

                        _logger.LogInformation($"Bắt đầud duyệt qua từng môn hoc của giảng viên {firstRecord.Lecturer.LecturerAccount}");
                        foreach (var ls in g) // Duyệt qua từng môn học mà giảng viên này được phân công.
                        {
                            if (!string.IsNullOrEmpty(ls.SubjectCode))
                            {
                                state.MaxClassesPerSubject[ls.SubjectCode] = ls.NumberOfClasses ?? 0;  // Thiết lập số lớp tối đa giảng viên có thể dạy cho môn này.

                                if (ls.NumberOfClasses.HasValue && ls.NumberOfClasses > 0) // Kiểm tra an toàn để tránh lỗi chia cho 0.
                                {
                                    state.RequiredSlotsPerSubject[ls.SubjectCode] = (ls.TotalSlots / ls.NumberOfClasses) ?? 2; // Tính số slot yêu cầu cho mỗi lớp của môn này.
                                }
                                else
                                {
                                    state.RequiredSlotsPerSubject[ls.SubjectCode] = 2; // Nếu không có thông tin, gán giá trị mặc định an toàn.
                                }
                            }
                            _logger.LogInformation($"- Môn {ls.SubjectCode} với {ls.NumberOfClasses} lớp, tổng số slot {ls.TotalSlots}");
                        }
                        return state;
                    });
            _logger.LogInformation($"Hoàn tất khởi tạo. Đã tải trạng thái cho {_lecturerStateMap.Count} giảng viên.");
        }

        /// <summary>
        /// Thực hiện thuật toán phân công giảng viên cho danh sách lịch học.
        /// </summary>
        /// <param name="lecturerSubjects">Thông tin phân công dạy của giảng viên.</param>
        /// <param name="allSchedules">Toàn bộ lịch học cần được phân công.</param>
        public void AssignLecturers(List<LecturerSubject> lecturerSubjects, List<Schedule> allSchedules)
        {
            _logger.LogInformation("--- BẮT ĐẦU THUẬT TOÁN PHÂN CÔNG GIẢNG VIÊN ---");

            InitializeLecturerStates(lecturerSubjects);

            // Nhóm các lịch học chưa được phân công theo cặp (Tên lớp, Mã môn học).
            // Điều này đảm bảo chúng ta xử lý toàn bộ các buổi của một lớp-môn học cùng lúc.
            var groupedSchedules = allSchedules
                .Where(s => string.IsNullOrEmpty(s.LecturerId) && !string.IsNullOrEmpty(s.SubjectCode) && !string.IsNullOrEmpty(s.GroupName))
                .GroupBy(s => (s.GroupName!, s.SubjectCode!));

            _logger.LogInformation($"Tìm thấy {groupedSchedules.Count()} nhóm Lớp-Môn học cần phân công.");

            // Lặp qua từng nhóm LỚP-MÔN HỌC, không phải từng slot riêng lẻ
            foreach (var classSubjectGroup in groupedSchedules)
            {
                string groupName = classSubjectGroup.Key.Item1;
                string subjectCode = classSubjectGroup.Key.Item2;
                var schedulesInGroup = classSubjectGroup.ToList();

                _logger.LogInformation($"Đang xử lý Lớp: '{groupName}', Môn: '{subjectCode}' ({schedulesInGroup.Count} buổi học).");

                // Tìm một giảng viên DUY NHẤT phù hợp cho TẤT CẢ các buổi học của lớp này
                var bestCandidate = FindBestLecturerForEntireClass(subjectCode, schedulesInGroup);

                if (bestCandidate != null)
                {
                    foreach (var schedule in schedulesInGroup) // Nếu tìm thấy, gán giảng viên đó cho tất cả các buổi học
                    {
                        schedule.LecturerId = bestCandidate.LecturerId;
                        schedule.LecturerName = bestCandidate.LecturerName;
                        schedule.LecturerAccount = bestCandidate.LecturerAccount;
                        bestCandidate.Assign(schedule); // Cập nhật trạng thái của giảng viên sau khi gán.
                    }
                    _logger.LogInformation($"Gán thành công GV '{bestCandidate.LecturerName}' cho Lớp '{groupName}', Môn '{subjectCode}'.");
                }
                else
                {
                    // Ghi log lỗi nếu không có giảng viên nào thỏa mãn tất cả các ràng buộc.
                    _logger.LogError($"Không tìm được giảng viên phù hợp cho TOÀN BỘ các buổi của Lớp '{groupName}', Môn '{subjectCode}'.");
                }
            }
            _logger.LogInformation("--- KẾT THÚC THUẬT TOÁN PHÂN CÔNG GIẢNG VIÊN ---");
        }

        /// <summary>
        /// Tìm giảng viên tốt nhất (theo tiêu chí Greedy) có thể dạy toàn bộ các buổi học cho một lớp-môn học cụ thể.
        /// </summary>
        /// <param name="subjectCode">Mã môn học cần giảng viên.</param>
        /// <param name="schedulesInGroup">Danh sách tất cả các buổi học của lớp-môn học đó.</param>
        /// <returns>Đối tượng LecturerAssignmentState của giảng viên phù hợp nhất, hoặc null nếu không tìm thấy.</returns>
        private LecturerAssignmentState? FindBestLecturerForEntireClass(string subjectCode, List<Schedule> schedulesInGroup)
        {
            // 1. Lọc ra những giảng viên có thể dạy môn này
            var potentialLecturers = _lecturerStateMap.Values
                .Where(l => l.MaxClassesPerSubject.ContainsKey(subjectCode));

            // 2. Tìm ứng viên thỏa mãn TẤT CẢ các ràng buộc cho TẤT CẢ các lịch học trong nhóm
            var validCandidates = potentialLecturers
                .Where(lecturer =>
                    schedulesInGroup.All(schedule => // Phải thỏa mãn TẤT CẢ (All) các lịch
                        lecturer.IsAvailable(schedule) &&                // Ràng buộc 1: Phải rảnh vào thời gian đó.
                        lecturer.CanTeachThisClassSubject(schedule) &&   // Ràng buộc 2: Có thể dạy lớp/môn này (không dạy 2 môn/lớp, chưa quá số lớp).
                        lecturer.IsValidDayOfWeekForTwoClasses(schedule) // Ràng buộc 3: Tuân thủ quy tắc về cặp ngày nếu chỉ dạy 2 lớp.
                    )
                )
                // Bước 3: Sắp xếp các ứng viên hợp lệ theo chiến lược Greedy.
                .OrderBy(l => l.GetAssignedGroupCount(subjectCode))  // Ưu tiên 1: Chọn người đang dạy ít lớp nhất cho môn học này.
                .ThenBy(l => l.TotalAssignedGroups) // Ưu tiên 2: Nếu bằng nhau, chọn người có tổng số lớp được giao ít nhất (để cân bằng tải chung).
                .ToList();

            return validCandidates.FirstOrDefault(); // Trả về ứng viên tốt nhất (người đứng đầu danh sách sau khi sắp xếp), hoặc null nếu không có ai hợp lệ.
        }
    }
}

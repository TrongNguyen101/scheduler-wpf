using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    public class GetLecturerForSubject
    {
        public (string lecturerId, string lecturerName, string lecturerAccount) FindLecturerForSubject(string subjectId, Dictionary<string, List<LecturerSubject>> lecturerBySubject, int lecturerIndex)
        {
            if (subjectId == null || !lecturerBySubject.TryGetValue(subjectId, out var lecturers))
            {
                return (null, null, null); // No lecturer found for the subject
            }
            var lecturername = lecturers.ElementAtOrDefault(lecturerIndex - 1)?.LecturerName; // Get the lecturer's name at the specified index
            var lecturerId = lecturers.ElementAtOrDefault(lecturerIndex - 1)?.LecturerId; // Get the lecturer's ID at the specified index
            var lecturerAccount = lecturers.ElementAtOrDefault(lecturerIndex - 1)?.Lecturer?.LecturerAccount; // Get the lecturer's account at the specified index

            return (lecturerId ?? "No lecturer", lecturername ?? "No lecturer", lecturerAccount ?? "No lecturer"); // Return the first lecturer's name or "No lecturer" if not found
        }

        public Dictionary<string, List<LecturerSubject>> FilterLecturerInSession(
                                                                                Dictionary<string, List<LecturerSubject>> lecturerBySubject,
                                                                                List<LecturerRequest> lecturerRequests,
                                                                                string currentSessionFilter)
        {
            // Tạo dictionary kết quả
            var filteredLecturers = new Dictionary<string, List<LecturerSubject>>();

            // Duyệt qua từng môn học trong lecturerBySubject
            foreach (var subject in lecturerBySubject)
            {
                string subjectId = subject.Key;
                List<LecturerSubject> lecturers = subject.Value;

                // Lọc danh sách giảng viên cho môn học này
                var availableLecturers = lecturers.Where(lecturer =>
                {
                    // Kiểm tra xem giảng viên này có bận trong session và ngày cụ thể hay không
                    bool isTeachingInSession = lecturerRequests.Any(request =>
                        request.LecturerId == lecturer.LecturerId // So khớp giảng viên
                        && request.Session == currentSessionFilter // So khớp buổi (AM/PM)
                        && request.DayName == null // So khớp ngày
                        && request.SlotType == null // So khop offline/online
                        );

                    // Giữ lại giảng viên nếu KHÔNG dạy trong session này
                    return !isTeachingInSession;
                }).ToList();

                // Nếu có giảng viên thỏa mãn, thêm vào dictionary kết quả
                if (availableLecturers.Any())
                {
                    filteredLecturers.Add(subjectId, availableLecturers);
                }
            }
            return filteredLecturers;
        }

        public Dictionary<string, List<LecturerSubject>> FilterLecturerInDate(Dictionary<string, List<LecturerSubject>> lecturerBySubject,
                                                                                List<LecturerRequest> lecturerRequests,
                                                                                string currentSessionFilter,
                                                                                DateTime currentDateFilter)
        {
            // Tạo dictionary kết quả
            var filteredLecturers = new Dictionary<string, List<LecturerSubject>>();

            // Duyệt qua từng môn học trong lecturerBySubject
            foreach (var subject in lecturerBySubject)
            {
                string subjectId = subject.Key;
                List<LecturerSubject> lecturers = subject.Value;

                string dayName = currentDateFilter.ToString("dddd").ToLower(); // Lấy tên ngày trong tuần (thứ 2, thứ 3, ...)
                // Lọc danh sách giảng viên cho môn học này
                var availableLecturers = lecturers.Where(lecturer =>
                {
                    // Kiểm tra xem giảng viên này có bận trong session và ngày cụ thể hay không
                    bool isTeachingInSession = lecturerRequests.Any(request =>
                        request.LecturerId == lecturer.LecturerId // So khớp giảng viên
                        && request.Session == currentSessionFilter // So khớp buổi (AM/PM)
                        && request.DayName.ToLower() == dayName // So khớp ngày
                        && request.SlotType == null // So khop offline/online
                        );

                    // Giữ lại giảng viên nếu KHÔNG bận trong session và ngày này
                    return !isTeachingInSession;
                }).ToList();

                // Nếu có giảng viên thỏa mãn, thêm vào dictionary kết quả
                if (availableLecturers.Any())
                {
                    filteredLecturers.Add(subjectId, availableLecturers);
                }
            }
            return filteredLecturers;
        }

        public Dictionary<string, List<LecturerSubject>> FilterLecturerAtSlot(Dictionary<string, List<LecturerSubject>> lecturerBySubject,
                                                                                List<LecturerRequest> lecturerRequests,
                                                                                DateTime currentDateFilter)
        {
            // Tạo dictionary kết quả
            var filteredLecturers = new Dictionary<string, List<LecturerSubject>>();

            // Duyệt qua từng môn học trong lecturerBySubject
            foreach (var subject in lecturerBySubject)
            {
                string subjectId = subject.Key;
                List<LecturerSubject> lecturers = subject.Value;

                string dayName = currentDateFilter.ToString("dddd").ToLower(); // Lấy tên ngày trong tuần (thứ 2, thứ 3, ...)
                // Lọc danh sách giảng viên cho môn học này
                var availableLecturers = lecturers.Where(lecturer =>
                {
                    // Kiểm tra xem giảng viên này có lịch dạy trong session và ngày cụ thể hay không
                    bool isTeachingInSession = lecturerRequests.Any(request =>
                        request.LecturerId == lecturer.LecturerId // So khớp giảng viên
                        && request.Session == null // So khớp buổi (AM/PM)
                        && request.DayName.ToLower() == dayName // So khớp ngày
                        && request.SlotType == null // So khop offline/online
                        );

                    // Giữ lại giảng viên nếu KHÔNG dạy trong session này
                    return !isTeachingInSession;
                }).ToList();

                // Nếu có giảng viên thỏa mãn, thêm vào dictionary kết quả
                if (availableLecturers.Any())
                {
                    filteredLecturers.Add(subjectId, availableLecturers);
                }
            }
            return filteredLecturers;
        }
    }
}

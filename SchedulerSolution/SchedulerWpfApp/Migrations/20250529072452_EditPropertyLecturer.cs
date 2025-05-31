using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class EditPropertyLecturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GroupName",
                keyColumn: "ClassId",
                keyValue: "CL01");

            migrationBuilder.DeleteData(
                table: "GroupName",
                keyColumn: "ClassId",
                keyValue: "CL02");

            migrationBuilder.DeleteData(
                table: "GroupName",
                keyColumn: "ClassId",
                keyValue: "CL03");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "1");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "2");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L1");

            migrationBuilder.DeleteData(
                table: "LecturerRequests",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "LecturerRequests",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "LecturerSubject",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Person",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectCode",
                keyValue: "ENW11");

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectCode",
                keyValue: "PRN211");

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectCode",
                keyValue: "SWP391");

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectCode",
                keyValue: "SWR302");

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectCode",
                keyValue: "SWT301");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L10");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L11");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L12");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L13");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L14");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L15");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L16");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L17");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L2");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L3");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L4");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L5");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L6");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L7");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L8");

            migrationBuilder.DeleteData(
                table: "Lecturer",
                keyColumn: "LecturerId",
                keyValue: "L9");

            migrationBuilder.AddColumn<string>(
                name: "Major",
                table: "Lecturer",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Major",
                table: "Lecturer");

            migrationBuilder.InsertData(
                table: "GroupName",
                columns: new[] { "ClassId", "Category", "Major", "NumberOfScheduler", "NumberOfStudents" },
                values: new object[,]
                {
                    { "CL01", "Class room", "SE", 5, 35 },
                    { "CL02", "Class room", "MC", 5, 35 },
                    { "CL03", "Computer lab", "SE", 5, 35 }
                });

            migrationBuilder.InsertData(
                table: "Lecturer",
                columns: new[] { "LecturerId", "LecturerName", "Role" },
                values: new object[,]
                {
                    { "1", "Nguyễn Văn A", null },
                    { "2", "Trần Thị B", null },
                    { "L1", "Nguyen Van Xoai", null },
                    { "L10", "Nguyen Thi Cam", null },
                    { "L11", "Nguyen Thi Mit", null },
                    { "L12", "Nguyen Thi Leo", null },
                    { "L13", "Nguyen Thi Man", null },
                    { "L14", "Nguyen Teo Em", null },
                    { "L15", "Nguyen Thi Cam", null },
                    { "L16", "Nguyen Thi Chuoi", null },
                    { "L17", "Nguyen Thi Hoa", null },
                    { "L2", "Sờ Mai", null },
                    { "L3", "Nguyen Mang Gồ", null },
                    { "L4", "Nguyen Vỉa Hè", null },
                    { "L5", "Nguyen Hoa Hong", null },
                    { "L6", "Nguyen Thi Hoa", null },
                    { "L7", "Nguyen Thi Bưởi", null },
                    { "L8", "Nguyen Thi Đào", null },
                    { "L9", "Nguyen Thi Oi", null }
                });

            migrationBuilder.InsertData(
                table: "LecturerRequests",
                columns: new[] { "Id", "DayName", "LecturerId", "Session", "SlotTime", "SlotType" },
                values: new object[,]
                {
                    { 1, "Monday", "L1", "A", null, null },
                    { 2, "Wednesday", "L1", "A", null, null }
                });

            migrationBuilder.InsertData(
                table: "Person",
                columns: new[] { "Id", "Email", "FirstName", "LastName", "Phone" },
                values: new object[,]
                {
                    { 1, "john.doe@example.com", "John", "Doe", "123-456-7890" },
                    { 2, "Jane.smith@example.com", "Jane", "Smith", "987-654-3210" },
                    { 3, "Alice.johnson@example.com", "Alice", "Johnson", "555-123-4567" }
                });

            migrationBuilder.InsertData(
                table: "Subject",
                columns: new[] { "SubjectCode", "Major", "SemesterId", "SlotsPerWeek", "SubjectName", "TotalSessions" },
                values: new object[,]
                {
                    { "ENW11", "SE", "", 1, "English", 10 },
                    { "PRN211", "SE", "", 2, "Programming", 20 },
                    { "SWP391", "SE", "", 2, "Software Engineering", 20 },
                    { "SWR302", "SE", "", 2, "Software Requirement", 20 },
                    { "SWT301", "SE", "", 2, "Software Testing", 20 }
                });

            migrationBuilder.InsertData(
                table: "LecturerSubject",
                columns: new[] { "Id", "LecturerId", "LecturerName", "NumberOfClasses", "SubjectCode", "SubjectCode1" },
                values: new object[,]
                {
                    { 1, "L2", "Sờ Mai", 1, "SWP391", null },
                    { 2, "L3", "Nguyen Mang Gồ", 1, "SWP391", null },
                    { 3, "L4", "Nguyen Vỉa Hè", 1, "SWP391", null },
                    { 4, "L5", "Nguyen Hoa Hong", 1, "SWT301", null },
                    { 5, "L6", "Nguyen Thi Hoa", 1, "SWT301", null },
                    { 6, "L7", "Nguyen Thi Bưởi", 1, "SWT301", null },
                    { 7, "L8", "Nguyen Thi Đào", 1, "SWT301", null },
                    { 8, "L9", "Nguyen Thi Oi", 1, "SWR302", null },
                    { 9, "L10", "Nguyen Thi Cam", 1, "SWR302", null },
                    { 10, "L11", "Nguyen Thi Mit", 1, "SWR302", null },
                    { 11, "L12", "Nguyen Thi Leo", 1, "SWR302", null },
                    { 12, "L13", "Nguyen Thi Man", 1, "PRN211", null },
                    { 13, "L14", "Nguyen Teo Em", 1, "PRN211", null },
                    { 14, "L15", "Nguyen Thi Cam", 1, "PRN211", null },
                    { 15, "L16", "Nguyen Thi Chuoi", 1, "PRN211", null },
                    { 16, "L17", "Nguyen Thi Hoa", 1, "ENW11", null }
                });
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDataTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupName",
                columns: table => new
                {
                    ClassId = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Major = table.Column<string>(type: "TEXT", nullable: true),
                    NumberOfStudents = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberOfScheduler = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupName", x => x.ClassId);
                });

            migrationBuilder.CreateTable(
                name: "Lecturer",
                columns: table => new
                {
                    LecturerId = table.Column<string>(type: "TEXT", nullable: false),
                    LecturerName = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecturer", x => x.LecturerId);
                });

            migrationBuilder.CreateTable(
                name: "LecturerRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LecturerId = table.Column<string>(type: "TEXT", nullable: false),
                    DayName = table.Column<string>(type: "TEXT", nullable: false),
                    Session = table.Column<string>(type: "TEXT", nullable: false),
                    SlotTime = table.Column<string>(type: "TEXT", nullable: true),
                    SlotType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LecturerRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomNo = table.Column<string>(type: "TEXT", nullable: true),
                    PartOfDay = table.Column<string>(type: "TEXT", nullable: true),
                    SlotTime = table.Column<string>(type: "TEXT", nullable: true),
                    StatusSlot = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Major = table.Column<string>(type: "TEXT", nullable: true),
                    SubjectCode = table.Column<string>(type: "TEXT", nullable: true),
                    GroupName = table.Column<string>(type: "TEXT", nullable: true),
                    LecturerId = table.Column<string>(type: "TEXT", nullable: true),
                    TypeSlot = table.Column<string>(type: "TEXT", nullable: true),
                    SessionNo = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotTypeCode = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.ScheduleId);
                });

            migrationBuilder.CreateTable(
                name: "Subject",
                columns: table => new
                {
                    SubjectCode = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectName = table.Column<string>(type: "TEXT", nullable: true),
                    Major = table.Column<string>(type: "TEXT", nullable: true),
                    TotalSessions = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotsPerWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    SemesterId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subject", x => x.SubjectCode);
                });

            migrationBuilder.CreateTable(
                name: "LecturerSubject",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LecturerId = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectCode = table.Column<string>(type: "TEXT", nullable: false),
                    LecturerName = table.Column<string>(type: "TEXT", nullable: true),
                    NumberOfClasses = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectCode1 = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LecturerSubject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LecturerSubject_Lecturer_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "Lecturer",
                        principalColumn: "LecturerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LecturerSubject_Subject_SubjectCode1",
                        column: x => x.SubjectCode1,
                        principalTable: "Subject",
                        principalColumn: "SubjectCode");
                });

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
                table: "Subject",
                columns: new[] { "SubjectCode", "Major", "SemesterId", "SlotsPerWeek", "SubjectName", "TotalSessions" },
                values: new object[,]
                {
                    { "ENW11", "SE", "", 1, "English", 10 },
                    { "HCM202", "SE", "SU25", 20, "Tw tuong Ho Chi Minh", 1 },
                    { "PRN211", "SE", "", 2, "Programming", 20 },
                    { "SEP492", "SE", "SU25", 20, "Do an tot nghiep", 1 },
                    { "SWP391", "SE", "", 2, "Software Engineering", 20 },
                    { "SWR302", "SE", "", 2, "Software Requirement", 20 },
                    { "SWT301", "SE", "", 2, "Software Testing", 20 },
                    { "WDP201", "SE", "SU25", 20, "Web development", 1 }
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

            migrationBuilder.CreateIndex(
                name: "IX_LecturerSubject_LecturerId",
                table: "LecturerSubject",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_LecturerSubject_SubjectCode1",
                table: "LecturerSubject",
                column: "SubjectCode1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupName");

            migrationBuilder.DropTable(
                name: "LecturerRequests");

            migrationBuilder.DropTable(
                name: "LecturerSubject");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "Lecturer");

            migrationBuilder.DropTable(
                name: "Subject");
        }
    }
}

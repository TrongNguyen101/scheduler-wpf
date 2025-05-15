using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLectureDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lecturer",
                columns: table => new
                {
                    LecturerId = table.Column<string>(type: "TEXT", nullable: false),
                    LecturerName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecturer", x => x.LecturerId);
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
                    NumberOfClasses = table.Column<int>(type: "INTEGER", nullable: false)
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
                        name: "FK_LecturerSubject_Subject_SubjectCode",
                        column: x => x.SubjectCode,
                        principalTable: "Subject",
                        principalColumn: "SubjectCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Lecturer",
                columns: new[] { "LecturerId", "LecturerName" },
                values: new object[,]
                {
                    { "1", "Nguyễn Văn A" },
                    { "2", "Trần Thị B" }
                });

            migrationBuilder.InsertData(
                table: "Subject",
                columns: new[] { "SubjectCode", "Major", "SemesterId", "SlotsPerWeek", "SubjectName", "TotalSessions" },
                values: new object[,]
                {
                    { "HCM202", "SE", "SU25", 20, "Tw tuong Ho Chi Minh", 1 },
                    { "SEP492", "SE", "SU25", 20, "Do an tot nghiep", 1 },
                    { "WDP201", "SE", "SU25", 20, "Web development", 1 }
                });

            migrationBuilder.InsertData(
                table: "LecturerSubject",
                columns: new[] { "Id", "LecturerId", "LecturerName", "NumberOfClasses", "SubjectCode" },
                values: new object[,]
                {
                    { 1, "1", "Nguyễn Văn A", 10, "SEP492" },
                    { 2, "2", "Trần Thị B", 5, "SEP492" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LecturerSubject_LecturerId",
                table: "LecturerSubject",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_LecturerSubject_SubjectCode",
                table: "LecturerSubject",
                column: "SubjectCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LecturerSubject");

            migrationBuilder.DropTable(
                name: "Lecturer");

            migrationBuilder.DropTable(
                name: "Subject");
        }
    }
}

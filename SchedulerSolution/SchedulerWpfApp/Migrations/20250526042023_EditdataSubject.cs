using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class EditdataSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectCode",
                keyValue: "HCM202");

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectCode",
                keyValue: "SEP492");

            migrationBuilder.DeleteData(
                table: "Subject",
                keyColumn: "SubjectCode",
                keyValue: "WDP201");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Subject",
                columns: new[] { "SubjectCode", "Major", "SemesterId", "SlotsPerWeek", "SubjectName", "TotalSessions" },
                values: new object[,]
                {
                    { "HCM202", "SE", "SU25", 20, "Tw tuong Ho Chi Minh", 1 },
                    { "SEP492", "SE", "SU25", 20, "Do an tot nghiep", 1 },
                    { "WDP201", "SE", "SU25", 20, "Web development", 1 }
                });
        }
    }
}

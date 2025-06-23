using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulesProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LecturerName",
                table: "Schedules",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LecturerName",
                table: "Schedules");
        }
    }
}

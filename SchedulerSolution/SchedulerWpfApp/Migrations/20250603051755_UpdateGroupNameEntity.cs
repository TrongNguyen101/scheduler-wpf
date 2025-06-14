using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGroupNameEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfScheduler",
                table: "GroupName");

            migrationBuilder.RenameColumn(
                name: "NumberOfStudents",
                table: "GroupName",
                newName: "Term");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "GroupName",
                newName: "Department");

            migrationBuilder.AddColumn<string>(
                name: "Course",
                table: "GroupName",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Course",
                table: "GroupName");

            migrationBuilder.RenameColumn(
                name: "Term",
                table: "GroupName",
                newName: "NumberOfStudents");

            migrationBuilder.RenameColumn(
                name: "Department",
                table: "GroupName",
                newName: "Category");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfScheduler",
                table: "GroupName",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}

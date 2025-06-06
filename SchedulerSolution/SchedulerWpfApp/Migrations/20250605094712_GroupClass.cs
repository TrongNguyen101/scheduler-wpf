using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class GroupClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Course",
                table: "GroupClass",
                newName: "Term");

            migrationBuilder.AddColumn<string>(
                name: "CurriculumCode",
                table: "GroupClass",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurriculumCode",
                table: "GroupClass");

            migrationBuilder.RenameColumn(
                name: "Term",
                table: "GroupClass",
                newName: "Course");
        }
    }
}

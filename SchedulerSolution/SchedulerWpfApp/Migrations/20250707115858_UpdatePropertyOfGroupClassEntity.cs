using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePropertyOfGroupClassEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PartOfDay",
                table: "GroupClass",
                newName: "PartOfDayInTheFirstTerm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PartOfDayInTheFirstTerm",
                table: "GroupClass",
                newName: "PartOfDay");
        }
    }
}

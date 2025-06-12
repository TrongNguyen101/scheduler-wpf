using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Curriculum_CurriculumCode",
                table: "Curriculum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Curriculum_CurriculumCode",
                table: "Curriculum",
                column: "CurriculumCode",
                unique: true);
        }
    }
}

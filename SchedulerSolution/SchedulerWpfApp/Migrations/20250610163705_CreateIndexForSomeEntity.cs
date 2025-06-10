using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class CreateIndexForSomeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LecturerSubject_LecturerId",
                table: "LecturerSubject");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumSubject_CurriculumCode",
                table: "CurriculumSubject");

            migrationBuilder.CreateIndex(
                name: "IX_RoomName",
                table: "Room",
                column: "RoomName");

            migrationBuilder.CreateIndex(
                name: "IX_LecturerId_SubjectCode",
                table: "LecturerSubject",
                columns: new[] { "LecturerId", "SubjectCode" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumCode",
                table: "GroupClass",
                column: "CurriculumCode");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumCode_SubjectCode",
                table: "CurriculumSubject",
                columns: new[] { "CurriculumCode", "SubjectCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Curriculum_CurriculumCode",
                table: "Curriculum",
                column: "CurriculumCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomName",
                table: "Room");

            migrationBuilder.DropIndex(
                name: "IX_LecturerId_SubjectCode",
                table: "LecturerSubject");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumCode",
                table: "GroupClass");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumCode_SubjectCode",
                table: "CurriculumSubject");

            migrationBuilder.DropIndex(
                name: "IX_Curriculum_CurriculumCode",
                table: "Curriculum");

            migrationBuilder.CreateIndex(
                name: "IX_LecturerSubject_LecturerId",
                table: "LecturerSubject",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumSubject_CurriculumCode",
                table: "CurriculumSubject",
                column: "CurriculumCode");
        }
    }
}

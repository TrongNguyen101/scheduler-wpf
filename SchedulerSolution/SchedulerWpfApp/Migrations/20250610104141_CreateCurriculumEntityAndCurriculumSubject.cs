using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class CreateCurriculumEntityAndCurriculumSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "NumberOfClasses",
                table: "LecturerSubject",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "Major",
                table: "LecturerSubject",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectName",
                table: "LecturerSubject",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Term",
                table: "LecturerSubject",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalSLots",
                table: "LecturerSubject",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Curriculum",
                columns: table => new
                {
                    CurriculumCode = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curriculum", x => x.CurriculumCode);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumSubject",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurriculumCode = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SubjectNameEnglish = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SubjectNameVietnamese = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    TermNo = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCombo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Credit = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSLots = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumSubject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumSubject_Curriculum_CurriculumCode",
                        column: x => x.CurriculumCode,
                        principalTable: "Curriculum",
                        principalColumn: "CurriculumCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumSubject_Subject_SubjectCode",
                        column: x => x.SubjectCode,
                        principalTable: "Subject",
                        principalColumn: "SubjectCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumSubject_CurriculumCode",
                table: "CurriculumSubject",
                column: "CurriculumCode");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumSubject_SubjectCode",
                table: "CurriculumSubject",
                column: "SubjectCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurriculumSubject");

            migrationBuilder.DropTable(
                name: "Curriculum");

            migrationBuilder.DropColumn(
                name: "Major",
                table: "LecturerSubject");

            migrationBuilder.DropColumn(
                name: "SubjectName",
                table: "LecturerSubject");

            migrationBuilder.DropColumn(
                name: "Term",
                table: "LecturerSubject");

            migrationBuilder.DropColumn(
                name: "TotalSLots",
                table: "LecturerSubject");

            migrationBuilder.AlterColumn<int>(
                name: "NumberOfClasses",
                table: "LecturerSubject",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}

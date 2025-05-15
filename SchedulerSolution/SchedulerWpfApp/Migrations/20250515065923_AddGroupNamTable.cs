using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupNamTable : Migration
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

            migrationBuilder.InsertData(
                table: "GroupName",
                columns: new[] { "ClassId", "Category", "Major", "NumberOfScheduler", "NumberOfStudents" },
                values: new object[,]
                {
                    { "CL01", "Class room", "SE", 5, 35 },
                    { "CL02", "Class room", "MC", 5, 35 },
                    { "CL03", "Computer lab", "SE", 5, 35 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupName");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRelationshipOfScheduleEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_GroupClass_GroupName",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Lecturer_LecturerId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Room_RoomId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Subject_SubjectCode",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "Person");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_GroupName",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_LecturerId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_RoomId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SubjectCode",
                table: "Schedules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Person",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_GroupName",
                table: "Schedules",
                column: "GroupName");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_LecturerId",
                table: "Schedules",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_RoomId",
                table: "Schedules",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SubjectCode",
                table: "Schedules",
                column: "SubjectCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_GroupClass_GroupName",
                table: "Schedules",
                column: "GroupName",
                principalTable: "GroupClass",
                principalColumn: "GroupName",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Lecturer_LecturerId",
                table: "Schedules",
                column: "LecturerId",
                principalTable: "Lecturer",
                principalColumn: "LecturerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Room_RoomId",
                table: "Schedules",
                column: "RoomId",
                principalTable: "Room",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Subject_SubjectCode",
                table: "Schedules",
                column: "SubjectCode",
                principalTable: "Subject",
                principalColumn: "SubjectCode",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

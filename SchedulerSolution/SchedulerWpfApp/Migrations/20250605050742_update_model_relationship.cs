using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class update_model_relationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LecturerSubject_Lecturer_LecturerId",
                table: "LecturerSubject");

            migrationBuilder.DropForeignKey(
                name: "FK_LecturerSubject_Subject_SubjectCode1",
                table: "LecturerSubject");

            migrationBuilder.DropTable(
                name: "GroupName");

            migrationBuilder.DropIndex(
                name: "IX_LecturerSubject_SubjectCode1",
                table: "LecturerSubject");

            migrationBuilder.DropColumn(
                name: "Major",
                table: "Subject");

            migrationBuilder.DropColumn(
                name: "SubjectCode1",
                table: "LecturerSubject");

            migrationBuilder.RenameColumn(
                name: "TotalSessions",
                table: "Subject",
                newName: "TotalTime");

            migrationBuilder.RenameColumn(
                name: "SlotsPerWeek",
                table: "Subject",
                newName: "TotalCredits");

            migrationBuilder.RenameColumn(
                name: "SemesterId",
                table: "Subject",
                newName: "SubjectNameVietnamese");

            migrationBuilder.RenameColumn(
                name: "Major",
                table: "Lecturer",
                newName: "Department");

            migrationBuilder.AlterColumn<int>(
                name: "SessionNo",
                table: "Schedules",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "RoomNo",
                table: "Schedules",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TypeOfRoom",
                table: "Room",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TotalPersons",
                table: "Room",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RoomName",
                table: "Room",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Building",
                table: "Room",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Floor",
                table: "Room",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Room",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "DistanceNote ",
                table: "LecturerRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasHealthIssue",
                table: "LecturerRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GroupClass",
                columns: table => new
                {
                    GroupName = table.Column<string>(type: "TEXT", nullable: false),
                    Course = table.Column<string>(type: "TEXT", nullable: true),
                    Major = table.Column<string>(type: "TEXT", nullable: true),
                    Department = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupClass", x => x.GroupName);
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
                name: "IX_Schedules_RoomNo",
                table: "Schedules",
                column: "RoomNo");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SubjectCode",
                table: "Schedules",
                column: "SubjectCode");

            migrationBuilder.CreateIndex(
                name: "IX_LecturerSubject_SubjectCode",
                table: "LecturerSubject",
                column: "SubjectCode");

            migrationBuilder.CreateIndex(
                name: "IX_LecturerRequests_LecturerId",
                table: "LecturerRequests",
                column: "LecturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_LecturerRequests_Lecturer_LecturerId",
                table: "LecturerRequests",
                column: "LecturerId",
                principalTable: "Lecturer",
                principalColumn: "LecturerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LecturerSubject_Lecturer_LecturerId",
                table: "LecturerSubject",
                column: "LecturerId",
                principalTable: "Lecturer",
                principalColumn: "LecturerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LecturerSubject_Subject_SubjectCode",
                table: "LecturerSubject",
                column: "SubjectCode",
                principalTable: "Subject",
                principalColumn: "SubjectCode",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_Schedules_Room_RoomNo",
                table: "Schedules",
                column: "RoomNo",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LecturerRequests_Lecturer_LecturerId",
                table: "LecturerRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LecturerSubject_Lecturer_LecturerId",
                table: "LecturerSubject");

            migrationBuilder.DropForeignKey(
                name: "FK_LecturerSubject_Subject_SubjectCode",
                table: "LecturerSubject");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_GroupClass_GroupName",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Lecturer_LecturerId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Room_RoomNo",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Subject_SubjectCode",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "GroupClass");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_GroupName",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_LecturerId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_RoomNo",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_SubjectCode",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_LecturerSubject_SubjectCode",
                table: "LecturerSubject");

            migrationBuilder.DropIndex(
                name: "IX_LecturerRequests_LecturerId",
                table: "LecturerRequests");

            migrationBuilder.DropColumn(
                name: "Building",
                table: "Room");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "Room");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Room");

            migrationBuilder.DropColumn(
                name: "DistanceNote ",
                table: "LecturerRequests");

            migrationBuilder.DropColumn(
                name: "HasHealthIssue",
                table: "LecturerRequests");

            migrationBuilder.RenameColumn(
                name: "TotalTime",
                table: "Subject",
                newName: "TotalSessions");

            migrationBuilder.RenameColumn(
                name: "TotalCredits",
                table: "Subject",
                newName: "SlotsPerWeek");

            migrationBuilder.RenameColumn(
                name: "SubjectNameVietnamese",
                table: "Subject",
                newName: "SemesterId");

            migrationBuilder.RenameColumn(
                name: "Department",
                table: "Lecturer",
                newName: "Major");

            migrationBuilder.AddColumn<string>(
                name: "Major",
                table: "Subject",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SessionNo",
                table: "Schedules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RoomNo",
                table: "Schedules",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TypeOfRoom",
                table: "Room",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "TotalPersons",
                table: "Room",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "RoomName",
                table: "Room",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "SubjectCode1",
                table: "LecturerSubject",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupName",
                columns: table => new
                {
                    ClassId = table.Column<string>(type: "TEXT", nullable: false),
                    Course = table.Column<string>(type: "TEXT", nullable: true),
                    Department = table.Column<string>(type: "TEXT", nullable: true),
                    Major = table.Column<string>(type: "TEXT", nullable: true),
                    Term = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupName", x => x.ClassId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LecturerSubject_SubjectCode1",
                table: "LecturerSubject",
                column: "SubjectCode1");

            migrationBuilder.AddForeignKey(
                name: "FK_LecturerSubject_Lecturer_LecturerId",
                table: "LecturerSubject",
                column: "LecturerId",
                principalTable: "Lecturer",
                principalColumn: "LecturerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LecturerSubject_Subject_SubjectCode1",
                table: "LecturerSubject",
                column: "SubjectCode1",
                principalTable: "Subject",
                principalColumn: "SubjectCode");
        }
    }
}

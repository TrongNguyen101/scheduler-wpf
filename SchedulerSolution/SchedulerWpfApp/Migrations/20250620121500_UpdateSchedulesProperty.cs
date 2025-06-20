using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulerWpfApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchedulesProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Room_RoomNo",
                table: "Schedules");

            migrationBuilder.RenameColumn(
                name: "RoomNo",
                table: "Schedules",
                newName: "RoomId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_RoomNo",
                table: "Schedules",
                newName: "IX_Schedules_RoomId");

            migrationBuilder.AddColumn<string>(
                name: "RoomName",
                table: "Schedules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Term",
                table: "GroupClass",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Room_RoomId",
                table: "Schedules",
                column: "RoomId",
                principalTable: "Room",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Room_RoomId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "RoomName",
                table: "Schedules");

            migrationBuilder.RenameColumn(
                name: "RoomId",
                table: "Schedules",
                newName: "RoomNo");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_RoomId",
                table: "Schedules",
                newName: "IX_Schedules_RoomNo");

            migrationBuilder.AlterColumn<string>(
                name: "Term",
                table: "GroupClass",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Room_RoomNo",
                table: "Schedules",
                column: "RoomNo",
                principalTable: "Room",
                principalColumn: "RoomId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

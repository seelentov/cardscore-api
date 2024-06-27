using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cardscore_api.Migrations
{
    /// <inheritdoc />
    public partial class Notif14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActionType",
                table: "Notifications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlayerUrl",
                table: "Notifications",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlayerUrl2",
                table: "Notifications",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Time",
                table: "Notifications",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PlayerUrl",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PlayerUrl2",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Time",
                table: "Notifications");
        }
    }
}

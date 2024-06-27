using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cardscore_api.Migrations
{
    /// <inheritdoc />
    public partial class Notif15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlayerName",
                table: "Notifications",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlayerName2",
                table: "Notifications",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerName",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PlayerName2",
                table: "Notifications");
        }
    }
}

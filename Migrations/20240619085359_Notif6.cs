using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cardscore_api.Migrations
{
    /// <inheritdoc />
    public partial class Notif6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpoToken",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpoToken",
                table: "Users");
        }
    }
}

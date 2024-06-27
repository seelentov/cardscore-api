using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cardscore_api.Migrations
{
    /// <inheritdoc />
    public partial class Notif4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Notifications",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Notifications");
        }
    }
}

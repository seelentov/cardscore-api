using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cardscore_api.Migrations
{
    /// <inheritdoc />
    public partial class Notif50 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leagues_ReglamentId",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "Reglaments");

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_ReglamentId",
                table: "Leagues",
                column: "ReglamentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leagues_ReglamentId",
                table: "Leagues");

            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "Reglaments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_ReglamentId",
                table: "Leagues",
                column: "ReglamentId",
                unique: true);
        }
    }
}

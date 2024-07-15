using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cardscore_api.Migrations
{
    /// <inheritdoc />
    public partial class NewParse2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leagues_Reglaments_ReglamentId",
                table: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_Leagues_ReglamentId",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "ReglamentId",
                table: "Leagues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReglamentId",
                table: "Leagues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_ReglamentId",
                table: "Leagues",
                column: "ReglamentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leagues_Reglaments_ReglamentId",
                table: "Leagues",
                column: "ReglamentId",
                principalTable: "Reglaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

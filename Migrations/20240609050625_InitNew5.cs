using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cardscore_api.Migrations
{
    /// <inheritdoc />
    public partial class InitNew5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeagueUser");

            migrationBuilder.AddColumn<int>(
                name: "UserNotificationOptionType",
                table: "UserNotificationOptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Leagues",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leagues_UserId",
                table: "Leagues",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leagues_Users_UserId",
                table: "Leagues",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leagues_Users_UserId",
                table: "Leagues");

            migrationBuilder.DropIndex(
                name: "IX_Leagues_UserId",
                table: "Leagues");

            migrationBuilder.DropColumn(
                name: "UserNotificationOptionType",
                table: "UserNotificationOptions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Leagues");

            migrationBuilder.CreateTable(
                name: "LeagueUser",
                columns: table => new
                {
                    FavoritesId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueUser", x => new { x.FavoritesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_LeagueUser_Leagues_FavoritesId",
                        column: x => x.FavoritesId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeagueUser_UsersId",
                table: "LeagueUser",
                column: "UsersId");
        }
    }
}

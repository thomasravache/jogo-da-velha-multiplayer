using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicTacToe.Modules.Gameplay.Migrations
{
    /// <inheritdoc />
    public partial class AddGameType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameType",
                schema: "Gameplay",
                table: "MatchResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_GameType",
                schema: "Gameplay",
                table: "MatchResults",
                column: "GameType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchResults_GameType",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "GameType",
                schema: "Gameplay",
                table: "MatchResults");
        }
    }
}

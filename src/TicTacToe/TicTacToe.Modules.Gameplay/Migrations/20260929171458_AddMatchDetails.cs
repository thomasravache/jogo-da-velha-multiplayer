using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicTacToe.Modules.Gameplay.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                schema: "Gameplay",
                table: "MatchResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndReason",
                schema: "Gameplay",
                table: "MatchResults",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalBoard",
                schema: "Gameplay",
                table: "MatchResults",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                schema: "Gameplay",
                table: "MatchResults",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MoveCount",
                schema: "Gameplay",
                table: "MatchResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WinnerSide",
                schema: "Gameplay",
                table: "MatchResults",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WinningLine",
                schema: "Gameplay",
                table: "MatchResults",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "EndReason",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "FinalBoard",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "Mode",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "MoveCount",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "WinnerSide",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "WinningLine",
                schema: "Gameplay",
                table: "MatchResults");
        }
    }
}

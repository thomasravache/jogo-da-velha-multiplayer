using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicTacToe.Modules.Gameplay.Migrations
{
    /// <inheritdoc />
    public partial class AddChessInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FinalFen",
                schema: "Gameplay",
                table: "MatchResults",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MovesSan",
                schema: "Gameplay",
                table: "MatchResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeControl",
                schema: "Gameplay",
                table: "MatchResults",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalFen",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "MovesSan",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "TimeControl",
                schema: "Gameplay",
                table: "MatchResults");
        }
    }
}

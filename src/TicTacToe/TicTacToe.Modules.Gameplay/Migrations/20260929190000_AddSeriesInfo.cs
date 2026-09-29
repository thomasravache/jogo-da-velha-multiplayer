using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicTacToe.Modules.Gameplay.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BestOf",
                schema: "Gameplay",
                table: "MatchResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoundNumber",
                schema: "Gameplay",
                table: "MatchResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SeriesId",
                schema: "Gameplay",
                table: "MatchResults",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BestOf",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "RoundNumber",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                schema: "Gameplay",
                table: "MatchResults");
        }
    }
}

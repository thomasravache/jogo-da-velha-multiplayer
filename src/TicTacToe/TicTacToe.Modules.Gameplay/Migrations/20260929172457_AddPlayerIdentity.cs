using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicTacToe.Modules.Gameplay.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlayerOId",
                schema: "Gameplay",
                table: "MatchResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerXId",
                schema: "Gameplay",
                table: "MatchResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_PlayerOId",
                schema: "Gameplay",
                table: "MatchResults",
                column: "PlayerOId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_PlayerXId",
                schema: "Gameplay",
                table: "MatchResults",
                column: "PlayerXId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchResults_PlayerOId",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropIndex(
                name: "IX_MatchResults_PlayerXId",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "PlayerOId",
                schema: "Gameplay",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "PlayerXId",
                schema: "Gameplay",
                table: "MatchResults");
        }
    }
}

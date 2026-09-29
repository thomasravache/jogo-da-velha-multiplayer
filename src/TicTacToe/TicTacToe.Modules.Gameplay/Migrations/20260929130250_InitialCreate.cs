using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicTacToe.Modules.Gameplay.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Gameplay");

            migrationBuilder.CreateTable(
                name: "MatchResults",
                schema: "Gameplay",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerXName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlayerOName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WinnerName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PlayedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchResults",
                schema: "Gameplay");
        }
    }
}

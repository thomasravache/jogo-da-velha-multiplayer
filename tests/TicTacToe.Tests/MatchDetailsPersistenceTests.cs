using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0036: persistência dos detalhes da partida

public class MatchDetailsPersistenceTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string Migrations = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations");

    private static DbContextOptions<GameplayDbContext> NewOptions() =>
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static GameResultService Service(GameplayDbContext db) => new(db, NullLogger<GameResultService>.Instance);

    [Fact(DisplayName = "SPEC-0036:UT-06 — IsPrivateMatch distingue sala privada de fila")]
    [Trait("Category", "SPEC-0036:UT-06")]
    public void IsPrivateMatch_ShouldDistinguishRoomFromQueue()
    {
        var service = new MatchmakingService();

        var code = service.CreatePrivateRoom("host", "Ana");
        var privateMatch = service.JoinPrivateRoom(code, "guest", "Bia");
        Assert.NotNull(privateMatch);
        Assert.True(service.IsPrivateMatch(privateMatch.Value));

        Assert.Null(service.JoinQueue("q1", "Caio"));
        var queued = service.JoinQueue("q2", "Duda");
        Assert.NotNull(queued);
        Assert.False(service.IsPrivateMatch(queued.Value));
    }

    [Fact(DisplayName = "SPEC-0036:IT-01 — SaveResultAsync grava duração, lances, motivo, lado, linha, tabuleiro e modo")]
    [Trait("Category", "SPEC-0036:IT-01")]
    public async Task SaveResultAsync_ShouldPersistMatchDetails()
    {
        var options = NewOptions();
        await using (var db = new GameplayDbContext(options))
        {
            var time = new GameSessionMatchDetailsTests.TestTime(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
            using var line = new GameSession(enableBackgroundTimer: false, timeProvider: time) { Mode = GameMode.Private };
            line.SetPlayerName(Player.X, "Ana");
            line.SetPlayerName(Player.O, "Ana"); // mesmo apelido nos dois lados
            line.MakeMove(0, Player.X);
            line.MakeMove(3, Player.O);
            line.MakeMove(1, Player.X);
            line.MakeMove(4, Player.O);
            time.Advance(TimeSpan.FromSeconds(42));
            line.MakeMove(2, Player.X);
            await Service(db).SaveResultAsync(line);

            using var draw = new GameSession(enableBackgroundTimer: false) { Mode = GameMode.Solo };
            draw.SetPlayerName(Player.X, "Bia");
            draw.SetPlayerName(Player.O, "Caio");
            foreach (var (c, p) in new[] { (0, Player.X), (1, Player.O), (2, Player.X), (4, Player.O), (3, Player.X), (5, Player.O), (7, Player.X), (6, Player.O), (8, Player.X) })
                draw.MakeMove(c, p);
            await Service(db).SaveResultAsync(draw);

            using var wo = new GameSession(enableBackgroundTimer: false);
            wo.SetPlayerName(Player.X, "Duda");
            wo.SetPlayerName(Player.O, "Edu");
            for (var i = 0; i < GameSession.DefaultTurnTimeSeconds; i++) wo.Tick();
            await Service(db).SaveResultAsync(wo);
        }

        await using var check = new GameplayDbContext(options);
        var rows = await check.MatchResults.ToListAsync();
        var byLine = rows.Single(r => r.EndReason == EndReason.Line);
        Assert.Equal(42, byLine.DurationSeconds);
        Assert.Equal(5, byLine.MoveCount);
        Assert.Equal("X", byLine.WinnerSide);
        Assert.Equal("0,1,2", byLine.WinningLine);
        Assert.Equal("XXXOO----", byLine.FinalBoard);
        Assert.Equal(GameMode.Private, byLine.Mode);

        var draw2 = rows.Single(r => r.EndReason == EndReason.Draw);
        Assert.Null(draw2.WinnerSide);
        Assert.Null(draw2.WinningLine);
        Assert.Equal(9, draw2.MoveCount);
        Assert.Equal(GameMode.Solo, draw2.Mode);

        var wo2 = rows.Single(r => r.EndReason == EndReason.Timeout);
        Assert.Equal("O", wo2.WinnerSide);
        Assert.Null(wo2.WinningLine);
        Assert.Equal(GameMode.Online, wo2.Mode);
    }

    [Fact(DisplayName = "SPEC-0036:IT-02 — Migration AddMatchDetails só adiciona colunas anuláveis e linhas antigas continuam legíveis")]
    [Trait("Category", "SPEC-0036:IT-02")]
    public async Task Migration_ShouldOnlyAddNullableColumns_AndLegacyRowsStillLoad()
    {
        var file = Directory.GetFiles(Migrations, "*_AddMatchDetails.cs").Single(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal));
        var source = File.ReadAllText(file);

        Assert.Equal(7, Regex.Matches(source, @"AddColumn<").Count);
        Assert.DoesNotContain("DropColumn", source);
        Assert.DoesNotContain("AlterColumn", source);
        Assert.DoesNotContain("DropTable", source);
        Assert.Equal(7, Regex.Matches(source, @"nullable: true").Count);

        var options = NewOptions();
        var when = DateTime.UtcNow;
        await using (var db = new GameplayDbContext(options))
        {
            db.MatchResults.Add(new MatchResult { PlayerXName = "Antigo", PlayerOName = "Legado", WinnerName = "Antigo", PlayedAt = when });
            await db.SaveChangesAsync();
        }

        await using var read = new GameplayDbContext(options);
        var service = Service(read);
        var recent = await service.GetRecentAsync(10);
        var board = await service.GetLeaderboardAsync(10);

        Assert.Single(recent);
        Assert.Null(recent[0].DurationSeconds);
        Assert.Null(recent[0].EndReason);
        Assert.Equal("Antigo", board.Single().PlayerName);
    }
}

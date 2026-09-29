using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;

namespace TicTacToe.Tests;

// SPEC-0053: colunas do xadrez em MatchResult, migration AddChessInfo e gravação de partida longa

public class ChessPersistenceTests
{
    private static readonly string RootDir = FindRoot();

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TicTacToe.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }

    private static DbContextOptions<GameplayDbContext> NewOptions() =>
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    [Fact(DisplayName = "SPEC-0053:CH-01 — SaveResultAsync do jogo da velha continua gravando como antes")]
    [Trait("Category", "SPEC-0053:CH-01")]
    public async Task SaveResultAsync_ShouldKeepTicTacToeBehavior()
    {
        await using var ctx = new GameplayDbContext(NewOptions());
        var service = new GameResultService(ctx, NullLogger<GameResultService>.Instance);
        var game = new GameSession(enableBackgroundTimer: false);
        game.SetPlayerName(Player.X, "Thomas");
        game.SetPlayerName(Player.O, "Ana");
        foreach (var (cell, player) in new[] { (0, Player.X), (3, Player.O), (1, Player.X), (4, Player.O), (2, Player.X) })
        {
            game.MakeMove(cell, player);
        }

        await service.SaveResultAsync(game);

        var row = await ctx.MatchResults.SingleAsync();
        Assert.Equal(GameType.TicTacToe, row.GameType);
        Assert.Equal("Thomas", row.WinnerName);
        Assert.Equal(EndReason.Line, row.EndReason);
        Assert.Null(row.TimeControl);
        Assert.Null(row.MovesSan);
        Assert.Null(row.FinalFen);
    }

    [Fact(DisplayName = "SPEC-0053:IT-02 — Migration AddChessInfo só tem AddColumn anulável das 3 colunas")]
    [Trait("Category", "SPEC-0053:IT-02")]
    public void Migration_ShouldBeAdditiveNullableColumns()
    {
        var dir = Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations");
        var file = Directory.GetFiles(dir, "*_AddChessInfo.cs").Single(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal));
        var full = File.ReadAllText(file);
        var up = full[full.IndexOf("void Up(", StringComparison.Ordinal)..full.IndexOf("void Down(", StringComparison.Ordinal)];

        Assert.Equal(3, Regex.Count(up, @"AddColumn<string>"));
        Assert.Equal(3, Regex.Count(up, @"nullable: true"));
        Assert.DoesNotContain("nullable: false", up);
        Assert.Contains("nvarchar(16)", up);
        Assert.Contains("nvarchar(max)", up);
        Assert.Contains("nvarchar(100)", up);
        Assert.Contains("\"TimeControl\"", up);
        Assert.Contains("\"MovesSan\"", up);
        Assert.Contains("\"FinalFen\"", up);
        Assert.DoesNotContain("DropColumn", up);
        Assert.DoesNotContain("AlterColumn", up);
        Assert.DoesNotContain("DropTable", up);
        Assert.DoesNotContain("CreateIndex", up);
    }

    [Fact(DisplayName = "SPEC-0053:IT-02 — Modelo: MovesSan sem limite, TimeControl 16 e FinalFen 100")]
    [Trait("Category", "SPEC-0053:IT-02")]
    public async Task Model_ShouldDeclareColumnSizes()
    {
        await using var ctx = new GameplayDbContext(NewOptions());
        var entity = ctx.Model.FindEntityType(typeof(MatchResult))!;

        Assert.Null(entity.FindProperty(nameof(MatchResult.MovesSan))!.GetMaxLength());
        Assert.Equal(16, entity.FindProperty(nameof(MatchResult.TimeControl))!.GetMaxLength());
        Assert.Equal(100, entity.FindProperty(nameof(MatchResult.FinalFen))!.GetMaxLength());
    }

    [Fact(DisplayName = "SPEC-0053:IT-02 — Linhas antigas (sem colunas de xadrez) continuam legíveis")]
    [Trait("Category", "SPEC-0053:IT-02")]
    public async Task OldRows_ShouldStillBeReadable()
    {
        await using var ctx = new GameplayDbContext(NewOptions());
        ctx.MatchResults.Add(new MatchResult { PlayerXName = "Ana", PlayerOName = "Bia", WinnerName = "Ana" });
        await ctx.SaveChangesAsync();
        var service = new GameResultService(ctx, NullLogger<GameResultService>.Instance);

        var recent = await service.GetRecentAsync();
        var board = await service.GetLeaderboardAsync();

        var row = Assert.Single(recent);
        Assert.Null(row.TimeControl);
        Assert.Equal("Ana", Assert.Single(board).PlayerName);
    }

    [Fact(DisplayName = "SPEC-0053:IT-04 — Partida de 300 meios-lances é gravada inteira")]
    [Trait("Category", "SPEC-0053:IT-04")]
    public async Task SaveChessAsync_ShouldStoreLongGameWithoutTruncation()
    {
        await using var ctx = new GameplayDbContext(NewOptions());
        var service = new GameResultService(ctx, NullLogger<GameResultService>.Instance);
        var moves = string.Join(' ', Enumerable.Range(0, 300).Select(i => i % 2 == 0 ? $"Nbd{i % 8 + 1}+" : $"Rae{i % 8 + 1}x"));

        await service.SaveChessAsync(new ChessMatchRecord(
            "Ana", "Bia", null, null, null, EndReason.Repetition, 300, 600, "blitz5+0", moves, "8/8/8/8/8/8/8/K6k w - - 0 151", GameMode.Online));

        var row = await ctx.MatchResults.SingleAsync();
        Assert.Equal(moves, row.MovesSan);
        Assert.Equal(300, row.MovesSan!.Split(' ').Length);
        Assert.Equal(300, row.MoveCount);
    }

    [Fact(DisplayName = "SPEC-0053:IT-04 — SaveChessAsync mapeia o registro para as colunas do xadrez")]
    [Trait("Category", "SPEC-0053:IT-04")]
    public async Task SaveChessAsync_ShouldMapRecord()
    {
        await using var ctx = new GameplayDbContext(NewOptions());
        var service = new GameResultService(ctx, NullLogger<GameResultService>.Instance);
        var white = Guid.NewGuid();

        await service.SaveChessAsync(new ChessMatchRecord(
            "Ana", "Bia", white, null, "X", EndReason.Checkmate, 4, 42, "blitz5+0", "f3 e5 g4 Qh4#", "fen", GameMode.Private));

        var row = await ctx.MatchResults.SingleAsync();
        Assert.Equal(GameType.Chess, row.GameType);
        Assert.Equal(("Ana", "Bia"), (row.PlayerXName, row.PlayerOName));
        Assert.Equal(white, row.PlayerXId);
        Assert.Null(row.PlayerOId);
        Assert.Equal("X", row.WinnerSide);
        Assert.Equal("Ana", row.WinnerName);
        Assert.Equal(EndReason.Checkmate, row.EndReason);
        Assert.Equal(42, row.DurationSeconds);
        Assert.Equal(GameMode.Private, row.Mode);
        Assert.Equal("fen", row.FinalFen);
    }
}

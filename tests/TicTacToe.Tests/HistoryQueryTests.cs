using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0038: histórico avançado — consultas sobre o banco (EF InMemory)

/// <summary>Dados de partida para os testes do histórico (SPEC-0038).</summary>
internal static class HistoryData
{
    public static readonly Guid Me = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static Guid GameId(int i) => new(i + 1, 0, 0, new byte[8]);

    public static DbContextOptions<GameplayDbContext> NewOptions() =>
        new DbContextOptionsBuilder<GameplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    public static async Task Seed(DbContextOptions<GameplayDbContext> options, IEnumerable<MatchResult> results)
    {
        await using var db = new GameplayDbContext(options);
        db.MatchResults.AddRange(results);
        await db.SaveChangesAsync();
    }

    public static GameResultService Service(GameplayDbContext db) => new(db, NullLogger<GameResultService>.Instance);

    /// <summary>
    /// 25 partidas do jogador <see cref="Me"/> (i = 0 é a mais recente): i%5 ∈ {0,1} vitória, ∈ {2,4} derrota, 3 empate;
    /// joga de X quando i é par; adversário "Rival{i%3}"; duração 100 − i; W.O.: 1 (V/desconexão), 4 (D/tempo), 6 (V/tempo), 9 (D/abandono), 14 (D/desconexão).
    /// </summary>
    public static List<MatchResult> TwentyFive(DateTime now)
    {
        var list = new List<MatchResult>();
        for (var i = 0; i < 25; i++)
        {
            var iAmX = i % 2 == 0;
            var mySide = iAmX ? "X" : "O";
            var otherSide = iAmX ? "O" : "X";
            var outcome = (i % 5) switch { 0 or 1 => 'W', 3 => 'D', _ => 'L' };
            var winnerSide = outcome switch { 'W' => mySide, 'L' => otherSide, _ => null };
            var reason = i switch
            {
                1 or 14 => EndReason.Disconnect,
                4 or 6 => EndReason.Timeout,
                9 => EndReason.Abandon,
                _ => outcome == 'D' ? EndReason.Draw : EndReason.Line,
            };
            var opponent = $"Rival{i % 3}";
            var mine = "Eu";
            list.Add(new MatchResult
            {
                Id = GameId(i),
                PlayerXName = iAmX ? mine : opponent,
                PlayerOName = iAmX ? opponent : mine,
                PlayerXId = iAmX ? Me : Guid.NewGuid(),
                PlayerOId = iAmX ? Guid.NewGuid() : Me,
                WinnerSide = winnerSide,
                WinnerName = winnerSide is null ? null : (winnerSide == mySide ? mine : opponent),
                EndReason = reason,
                DurationSeconds = 100 - i,
                MoveCount = 5,
                PlayedAt = now.AddMinutes(-i),
                Mode = GameMode.Online,
            });
        }

        return list;
    }

    public static Guid[] Ids(params int[] indexes) => indexes.Select(GameId).ToArray();

    public static HistoryQuery Query(HistoryFilter filter = HistoryFilter.All, string? opponent = null,
        HistorySort sort = HistorySort.Recent, int page = 1, HistoryScope scope = HistoryScope.Mine, Guid? player = null, int pageSize = 10) =>
        new(scope == HistoryScope.Mine ? player ?? Me : null, scope, filter, opponent, sort, page, pageSize);
}

public class HistoryQueryTests
{
    private static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<HistoryPage> Run(HistoryQuery query, Action<List<MatchResult>>? tweak = null)
    {
        var options = HistoryData.NewOptions();
        var data = HistoryData.TwentyFive(Now);
        tweak?.Invoke(data);
        await HistoryData.Seed(options, data);
        await using var db = new GameplayDbContext(options);
        return await HistoryData.Service(db).GetHistoryAsync(query);
    }

    [Fact(DisplayName = "SPEC-0038:CH-01 — GetRecentAsync(10) continua devolvendo as 10 mais recentes em ordem decrescente")]
    [Trait("Category", "SPEC-0038:CH-01")]
    public async Task GetRecentAsync_ShouldStillReturnTenNewestDescending()
    {
        var options = HistoryData.NewOptions();
        await HistoryData.Seed(options, HistoryData.TwentyFive(Now));
        await using var db = new GameplayDbContext(options);

        var recent = await HistoryData.Service(db).GetRecentAsync(10);

        Assert.Equal(HistoryData.Ids(0, 1, 2, 3, 4, 5, 6, 7, 8, 9), recent.Select(m => m.Id).ToArray());
    }

    [Fact(DisplayName = "SPEC-0038:IT-01 — Sem filtro: 25 partidas em 3 páginas, mais recentes primeiro, com contagens")]
    [Trait("Category", "SPEC-0038:IT-01")]
    public async Task Default_ShouldReturnFirstPageWithCounts()
    {
        var page = await Run(HistoryData.Query());

        Assert.Equal(HistoryData.Ids(0, 1, 2, 3, 4, 5, 6, 7, 8, 9), page.Items.Select(i => i.Id).ToArray());
        Assert.Equal(25, page.TotalItems);
        Assert.Equal(1, page.Page);
        Assert.Equal(3, page.PageCount);
        Assert.Equal(new HistoryCounts(25, 10, 10, 5, 5), page.Counts);
    }

    [Fact(DisplayName = "SPEC-0038:IT-01 — Item pessoal traz resultado, lado, motivo de W.O., vencedor e duração")]
    [Trait("Category", "SPEC-0038:IT-01")]
    public async Task Item_ShouldCarryPersonalPerspective()
    {
        var page = await Run(HistoryData.Query());

        var win = page.Items[0]; // i=0: vitória de X (eu), Line
        Assert.Equal(HistoryOutcome.Win, win.Outcome);
        Assert.True(win.IAmX);
        Assert.False(win.WalkOver);
        Assert.Equal("X", win.WinnerSide);
        Assert.Equal("Eu", win.WinnerName);
        Assert.Equal("Eu", win.PlayerXName);
        Assert.Equal("Rival0", win.PlayerOName);
        Assert.Equal(100, win.DurationSeconds);
        Assert.Equal(GameMode.Online, win.Mode);

        var walkoverLoss = page.Items[4]; // i=4: derrota por tempo, eu de X
        Assert.Equal(HistoryOutcome.Loss, walkoverLoss.Outcome);
        Assert.True(walkoverLoss.WalkOver);
        Assert.Equal("Tempo esgotado", walkoverLoss.Reason);

        var draw = page.Items[3]; // i=3: empate, eu de O
        Assert.Equal(HistoryOutcome.Draw, draw.Outcome);
        Assert.False(draw.IAmX);
        Assert.Null(draw.WinnerSide);
        Assert.Null(draw.WinnerName);
        Assert.Equal("Grid completo sem vencedor", draw.Reason);
    }

    [Theory(DisplayName = "SPEC-0038:IT-01 — Cada filtro restringe a lista; as contagens não mudam")]
    [Trait("Category", "SPEC-0038:IT-01")]
    [InlineData(HistoryFilter.Wins, new[] { 0, 1, 5, 6, 10, 11, 15, 16, 20, 21 })]
    [InlineData(HistoryFilter.Losses, new[] { 2, 4, 7, 9, 12, 14, 17, 19, 22, 24 })]
    [InlineData(HistoryFilter.Draws, new[] { 3, 8, 13, 18, 23 })]
    [InlineData(HistoryFilter.WalkOvers, new[] { 1, 4, 6, 9, 14 })]
    public async Task Filter_ShouldRestrictList(HistoryFilter filter, int[] expected)
    {
        var page = await Run(HistoryData.Query(filter));

        Assert.Equal(HistoryData.Ids(expected), page.Items.Select(i => i.Id).ToArray());
        Assert.Equal(expected.Length, page.TotalItems);
        Assert.Equal(new HistoryCounts(25, 10, 10, 5, 5), page.Counts);
    }

    [Fact(DisplayName = "SPEC-0038:IT-01 — Paginação: página 3 traz o resto, com clamp nos limites")]
    [Trait("Category", "SPEC-0038:IT-01")]
    public async Task Paging_ShouldSliceAndClamp()
    {
        var last = await Run(HistoryData.Query(page: 3));
        Assert.Equal(HistoryData.Ids(20, 21, 22, 23, 24), last.Items.Select(i => i.Id).ToArray());
        Assert.Equal(3, last.Page);
        Assert.Equal(25, last.Counts.All);

        var beyond = await Run(HistoryData.Query(page: 99));
        Assert.Equal(3, beyond.Page);
        Assert.Equal(5, beyond.Items.Count);

        var below = await Run(HistoryData.Query(page: 0));
        Assert.Equal(1, below.Page);
        Assert.Equal(10, below.Items.Count);
    }

    [Fact(DisplayName = "SPEC-0038:IT-01 — Busca por adversário ignora caixa, vale só para o adversário e ajusta as contagens")]
    [Trait("Category", "SPEC-0038:IT-01")]
    public async Task Opponent_ShouldMatchOnlyTheOpponentName()
    {
        var page = await Run(HistoryData.Query(opponent: "  rIVAL1 "));

        // Rival1: i%3==1 → 1,4,7,10,13,16,19,22
        Assert.Equal(HistoryData.Ids(1, 4, 7, 10, 13, 16, 19, 22), page.Items.Select(i => i.Id).ToArray());
        Assert.Equal(8, page.TotalItems);
        // 1V(W.O.) 4L(W.O.) 7L 10W 13D 16W 19L 22L → 3 V, 4 D... conferido por índice
        Assert.Equal(new HistoryCounts(8, 3, 4, 1, 2), page.Counts);

        var mineName = await Run(HistoryData.Query(opponent: "Eu"));
        Assert.Empty(mineName.Items); // o próprio nome não é adversário
    }

    [Fact(DisplayName = "SPEC-0038:IT-01 — Ordenação: recentes, menor duração (nulos por último) e por resultado")]
    [Trait("Category", "SPEC-0038:IT-01")]
    public async Task Sort_ShouldFollowTheContract()
    {
        var fastest = await Run(HistoryData.Query(sort: HistorySort.ShortestDuration), d => d[3].DurationSeconds = null);
        Assert.Equal(HistoryData.Ids(24, 23, 22, 21, 20, 19, 18, 17, 16, 15), fastest.Items.Select(i => i.Id).ToArray());
        var lastPage = await Run(HistoryData.Query(sort: HistorySort.ShortestDuration, page: 3), d => d[3].DurationSeconds = null);
        Assert.Equal(HistoryData.GameId(3), lastPage.Items[^1].Id);

        var byResult = await Run(HistoryData.Query(sort: HistorySort.Result));
        Assert.Equal(HistoryData.Ids(0, 1, 5, 6, 10, 11, 15, 16, 20, 21), byResult.Items.Select(i => i.Id).ToArray());
        var second = await Run(HistoryData.Query(sort: HistorySort.Result, page: 2));
        Assert.Equal(HistoryData.Ids(3, 8, 13, 18, 23, 2, 4, 7, 9, 12), second.Items.Select(i => i.Id).ToArray());
    }

    [Fact(DisplayName = "SPEC-0038:IT-02 — Escopo pessoal só traz as partidas do jogador; Todos traz tudo, inclusive as antigas")]
    [Trait("Category", "SPEC-0038:IT-02")]
    public async Task Scope_ShouldSeparateMineFromAll()
    {
        var options = HistoryData.NewOptions();
        var data = HistoryData.TwentyFive(Now);
        data.Add(new MatchResult { Id = HistoryData.GameId(100), PlayerXName = "Velho", PlayerOName = "Antigo", WinnerName = "Velho", PlayedAt = Now.AddDays(-30) });
        data.Add(new MatchResult { Id = HistoryData.GameId(101), PlayerXName = "A", PlayerOName = "B", PlayerXId = Guid.NewGuid(), PlayerOId = Guid.NewGuid(), WinnerName = "A", WinnerSide = "X", PlayedAt = Now.AddDays(-1) });
        await HistoryData.Seed(options, data);
        await using var db = new GameplayDbContext(options);
        var service = HistoryData.Service(db);

        var mine = await service.GetHistoryAsync(HistoryData.Query(pageSize: 100));
        Assert.Equal(25, mine.TotalItems);
        Assert.DoesNotContain(mine.Items, i => i.Id == HistoryData.GameId(100) || i.Id == HistoryData.GameId(101));

        var all = await service.GetHistoryAsync(HistoryData.Query(scope: HistoryScope.All, pageSize: 100));
        Assert.Equal(27, all.TotalItems);
        Assert.All(all.Items, i => { Assert.Null(i.Outcome); Assert.Null(i.IAmX); });
        var legacy = all.Items.Single(i => i.Id == HistoryData.GameId(100));
        Assert.Equal("Velho", legacy.WinnerName);
        Assert.Equal("X", legacy.WinnerSide); // deduzido pelos nomes em partidas antigas
        Assert.Null(legacy.DurationSeconds);
        Assert.Equal("—", legacy.Reason);
        Assert.Equal(HistoryData.GameId(0), all.Items[0].Id); // mais recente primeiro

        var wins = await service.GetHistoryAsync(HistoryData.Query(HistoryFilter.Wins, scope: HistoryScope.All, pageSize: 100));
        Assert.Equal(27, wins.TotalItems); // Vitórias não se aplica a Todos: vale como Todas
        Assert.Equal(0, wins.Counts.Wins);
        Assert.Equal(0, wins.Counts.Losses);
    }

    [Fact(DisplayName = "SPEC-0038:IT-02b — Escopo pessoal sem PlayerId devolve página vazia e homônimos usam o lado")]
    [Trait("Category", "SPEC-0038:IT-02")]
    public async Task Mine_WithoutPlayerId_ShouldBeEmpty_AndHomonymsUseSide()
    {
        var options = HistoryData.NewOptions();
        var me = Guid.NewGuid();
        await HistoryData.Seed(options,
        [
            new MatchResult { PlayerXName = "Thomas", PlayerOName = "Thomas", PlayerXId = Guid.NewGuid(), PlayerOId = me, WinnerName = "Thomas", WinnerSide = "X", PlayedAt = Now },
        ]);
        await using var db = new GameplayDbContext(options);
        var service = HistoryData.Service(db);

        var none = await service.GetHistoryAsync(new HistoryQuery(null, HistoryScope.Mine, HistoryFilter.All, null, HistorySort.Recent, 1));
        Assert.Empty(none.Items);
        Assert.Equal(0, none.TotalItems);
        Assert.Equal(1, none.PageCount);

        var mine = await service.GetHistoryAsync(HistoryData.Query(player: me));
        Assert.Equal(HistoryOutcome.Loss, mine.Items.Single().Outcome); // o outro Thomas (X) venceu
    }

    [Fact(DisplayName = "SPEC-0038:IT-03 — Resumo persistido bate com o cálculo puro")]
    [Trait("Category", "SPEC-0038:IT-03")]
    public async Task PlayerSummary_ShouldMatchPureCalculation()
    {
        var options = HistoryData.NewOptions();
        var data = HistoryData.TwentyFive(Now);
        await HistoryData.Seed(options, data);
        await using var db = new GameplayDbContext(options);

        var summary = await HistoryData.Service(db).GetPlayerSummaryAsync(HistoryData.Me);

        var games = data.Select(m => new SummaryGame(m.PlayerXId == HistoryData.Me, m.WinnerSide, m.EndReason, m.DurationSeconds, m.MoveCount, m.PlayedAt));
        Assert.Equal(HistoryAnalysis.Summarize(games), summary);
        Assert.Equal(25, summary.Total);
        Assert.Equal(10, summary.Wins);
        Assert.Equal(2, summary.CurrentWinStreak);

        var stranger = await HistoryData.Service(db).GetPlayerSummaryAsync(Guid.NewGuid());
        Assert.Equal(0, stranger.Total);
    }

    [Fact(DisplayName = "SPEC-0038:IT-04 — Consulta paginada com 1.000 partidas responde em menos de 200 ms")]
    [Trait("Category", "SPEC-0038:IT-04")]
    public async Task Query_ShouldBeFastWithThousandGames()
    {
        var options = HistoryData.NewOptions();
        var data = Enumerable.Range(0, 1000).Select(i => new MatchResult
        {
            PlayerXName = "Eu",
            PlayerOName = $"R{i % 7}",
            PlayerXId = HistoryData.Me,
            PlayerOId = Guid.NewGuid(),
            WinnerSide = i % 3 == 0 ? "X" : i % 3 == 1 ? "O" : null,
            EndReason = i % 3 == 2 ? EndReason.Draw : EndReason.Line,
            DurationSeconds = 30 + i % 50,
            MoveCount = 5 + i % 4,
            PlayedAt = Now.AddMinutes(-i),
        }).ToList();
        await HistoryData.Seed(options, data);
        await using var db = new GameplayDbContext(options);
        var service = HistoryData.Service(db);
        await service.GetHistoryAsync(HistoryData.Query()); // aquecimento

        var sw = Stopwatch.StartNew();
        var page = await service.GetHistoryAsync(HistoryData.Query(HistoryFilter.Wins, "r3", HistorySort.ShortestDuration, 2));
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 200, $"consulta levou {sw.ElapsedMilliseconds} ms");
        Assert.True(page.Counts.All > 0);
        Assert.True(page.Items.Count > 0);
    }
}

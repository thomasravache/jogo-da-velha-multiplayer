using System;
using System.Collections.Generic;
using System.Linq;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0039: ranking avançado — regras puras (LeaderboardAnalysis)

public class LeaderboardAnalysisTests
{
    private static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Opp = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static Guid Id(int n) => new(n, 0, 0, new byte[8]);

    /// <summary>Partida com identidade: <paramref name="winner"/> = "X", "O" ou null (empate).</summary>
    private static LeaderboardGame G(string x, Guid? xId, string o, Guid? oId, string? winner, int minutesAgo, GameMode? mode = GameMode.Online) =>
        new(x, o, xId, oId, winner switch { "X" => x, "O" => o, _ => null }, winner, mode, Now.AddMinutes(-minutesAgo));

    /// <summary>Partida antiga: sem ids, sem lado vencedor, sem modo.</summary>
    private static LeaderboardGame Legacy(string x, string o, string? winnerName, int minutesAgo) =>
        new(x, o, null, null, winnerName, null, null, Now.AddMinutes(-minutesAgo));

    [Fact(DisplayName = "SPEC-0039:UT-01 — Mesmo apelido com PlayerId diferentes gera entradas separadas")]
    [Trait("Category", "SPEC-0039:UT-01")]
    public void Rank_ShouldSeparateSameNicknameByPlayerId()
    {
        var ranked = LeaderboardAnalysis.Rank(
        [
            G("Alice", Id(1), "Bob", Opp, "X", 5),
            G("Alice", Id(2), "Bob", Opp, "X", 3),
            G("Alice", Id(2), "Bob", Opp, "X", 1),
        ], null);

        Assert.Equal(2, ranked.Count);
        Assert.All(ranked, e => Assert.Equal("Alice", e.DisplayName));
        Assert.Equal([2, 1], ranked.Select(e => e.Wins).ToArray());
    }

    [Fact(DisplayName = "SPEC-0039:UT-01 — Partidas antigas agrupam por apelido; derrota do legado conta")]
    [Trait("Category", "SPEC-0039:UT-01")]
    public void Rank_ShouldGroupLegacyByNickname()
    {
        var ranked = LeaderboardAnalysis.Rank(
        [
            Legacy("Zé", "Rival", "Zé", 30),
            Legacy("Rival", "Zé", "Zé", 20),
            Legacy("Rival", "Zé", "Rival", 10),
        ], null);

        var ze = Assert.Single(ranked);
        Assert.Equal("Zé", ze.DisplayName);
        Assert.Equal((2, 1, 0), (ze.Wins, ze.Losses, ze.Draws));
    }

    [Fact(DisplayName = "SPEC-0039:UT-01 — Solo e partidas antigas contra robô não contam; homônimos antigos não atribuem vitória")]
    [Trait("Category", "SPEC-0039:UT-01")]
    public void Rank_ShouldExcludeSoloBotAndAmbiguousLegacy()
    {
        var bot = AiPlayer.GetBotName(AiDifficulty.Easy);
        var games = new[]
        {
            G("Thomas", Id(1), bot, null, "X", 9, GameMode.Solo),
            Legacy("Ana", bot, "Ana", 8),
            Legacy(bot, "Ana", bot, 7),
            Legacy("Thomas", "Thomas", "Thomas", 6),
            G("Caio", Id(3), "Dora", Id(4), "X", 5),
        };

        Assert.False(LeaderboardAnalysis.Counts(games[0]));
        Assert.False(LeaderboardAnalysis.Counts(games[1]));
        Assert.True(LeaderboardAnalysis.Counts(games[4]));

        var ranked = LeaderboardAnalysis.Rank(games, null);
        Assert.Equal("Caio", Assert.Single(ranked).DisplayName);
    }

    [Fact(DisplayName = "SPEC-0039:UT-01 — Estatísticas: 5 V, 2 D, 1 E, aproveitamento 62,5% e sequência atual")]
    [Trait("Category", "SPEC-0039:UT-01")]
    public void Rank_ShouldComputeStats()
    {
        var me = Id(7);
        // Do mais recente ao mais antigo: V V D V E V D V (eu jogo de X nas pares e de O nas ímpares)
        var pattern = "WWLWEWLW";
        var games = pattern.Select((c, i) =>
        {
            var iAmX = i % 2 == 0;
            var mySide = iAmX ? "X" : "O";
            var other = iAmX ? "O" : "X";
            var winner = c switch { 'W' => mySide, 'L' => other, _ => null };
            return iAmX ? G("Eu", me, "Opp", Opp, winner, i) : G("Opp", Opp, "Eu", me, winner, i);
        }).ToList();

        var entry = LeaderboardAnalysis.Rank(games, me).Single(e => e.IsMe);

        Assert.Equal((5, 2, 1), (entry.Wins, entry.Losses, entry.Draws));
        Assert.Equal(62.5, entry.WinRatePercent);
        Assert.Equal(2, entry.WinStreak);
        Assert.Equal(Now, entry.LastWinAtUtc);
        Assert.Equal(1, entry.Position);
    }

    [Fact(DisplayName = "SPEC-0039:UT-01 — Ordem por vitórias, desempate pelo último triunfo mais recente; sem vitória não entra")]
    [Trait("Category", "SPEC-0039:UT-01")]
    public void Rank_ShouldOrderByWinsThenMostRecentWin()
    {
        var ranked = LeaderboardAnalysis.Rank(
        [
            G("Antigo", Id(1), "Feeder", Opp, "X", 100),
            G("Antigo", Id(1), "Feeder", Opp, "X", 90),
            G("Recente", Id(2), "Feeder", Opp, "X", 20),
            G("Recente", Id(2), "Feeder", Opp, "X", 10),
            G("Lider", Id(3), "Feeder", Opp, "X", 50),
            G("Lider", Id(3), "Feeder", Opp, "X", 40),
            G("Lider", Id(3), "Feeder", Opp, "X", 30),
            G("Azarado", Id(4), "Feeder", Opp, null, 5),
        ], null);

        Assert.Equal(["Lider", "Recente", "Antigo"], ranked.Select(e => e.DisplayName).ToArray());
        Assert.Equal([1, 2, 3], ranked.Select(e => e.Position).ToArray());
    }

    [Fact(DisplayName = "SPEC-0039:UT-01 — Nome exibido é o apelido da partida mais recente")]
    [Trait("Category", "SPEC-0039:UT-01")]
    public void Rank_ShouldUseMostRecentNickname()
    {
        var ranked = LeaderboardAnalysis.Rank(
        [
            G("Velho", Id(1), "Feeder", Opp, "X", 60),
            G("Novo", Id(1), "Feeder", Opp, "X", 1),
        ], null);

        Assert.Equal("Novo", Assert.Single(ranked).DisplayName);
    }

    [Fact(DisplayName = "SPEC-0039:UT-01 — Só o jogador atual (por PlayerId) recebe IsMe")]
    [Trait("Category", "SPEC-0039:UT-01")]
    public void Rank_ShouldFlagOnlyMyPlayerId()
    {
        var ranked = LeaderboardAnalysis.Rank(
        [
            G("Eu", Id(1), "Feeder", Opp, "X", 3),
            G("Outro", Id(2), "Feeder", Opp, "X", 2),
            G("Eu", Id(3), "Feeder", Opp, "X", 1), // mesmo apelido, outro PlayerId
        ], Id(1));

        Assert.Equal(1, ranked.Count(e => e.IsMe));
        Assert.True(ranked.Single(e => e.IsMe).Wins == 1);
    }

    private static List<LeaderboardEntry> Entries(int count, int meIndex = -1) =>
        Enumerable.Range(0, count)
            .Select(i => new LeaderboardEntry(i + 1, $"P{i}", count - i, 0, 0, 100, 1, Now, i == meIndex))
            .ToList();

    [Fact(DisplayName = "SPEC-0039:UT-02 — 25 jogadores em páginas de 10, 10 e 5 com posição contínua")]
    [Trait("Category", "SPEC-0039:UT-02")]
    public void Paginate_ShouldSliceWithContinuousPositions()
    {
        var ranked = Entries(25, meIndex: 22);

        var first = LeaderboardAnalysis.Paginate(ranked, 1, 10);
        var second = LeaderboardAnalysis.Paginate(ranked, 2, 10);
        var third = LeaderboardAnalysis.Paginate(ranked, 3, 10);

        Assert.Equal([10, 10, 5], new[] { first.Items.Count, second.Items.Count, third.Items.Count });
        Assert.Equal(Enumerable.Range(11, 10), second.Items.Select(e => e.Position));
        Assert.Equal(25, first.TotalPlayers);
        Assert.Equal(3, first.PageCount);
        Assert.Equal(23, first.Me!.Position); // Me vem mesmo fora da página
        Assert.DoesNotContain(first.Items, e => e.IsMe);
    }

    [Fact(DisplayName = "SPEC-0039:UT-02b — Página fora do intervalo é limitada; lista vazia tem uma página e Me nulo")]
    [Trait("Category", "SPEC-0039:UT-02")]
    public void Paginate_ShouldClampAndHandleEmpty()
    {
        var ranked = Entries(25);

        Assert.Equal(3, LeaderboardAnalysis.Paginate(ranked, 99, 10).Page);
        Assert.Equal(1, LeaderboardAnalysis.Paginate(ranked, 0, 10).Page);
        Assert.Equal(1, LeaderboardAnalysis.Paginate(ranked, -4, 10).Page);
        Assert.Null(LeaderboardAnalysis.Paginate(ranked, 1, 10).Me);

        var empty = LeaderboardAnalysis.Paginate([], 5, 10);
        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.TotalPlayers);
        Assert.Equal(1, empty.PageCount);
        Assert.Equal(1, empty.Page);
    }

    [Theory(DisplayName = "SPEC-0039:UT-01b — IsBotName reconhece os nomes dos robôs e nada mais")]
    [Trait("Category", "SPEC-0039:UT-01")]
    [InlineData(AiDifficulty.Easy, true)]
    [InlineData(AiDifficulty.Hard, true)]
    [InlineData((AiDifficulty)99, true)] // nome padrão (Minimax)
    public void IsBotName_ShouldRecognizeBotNames(AiDifficulty difficulty, bool expected) =>
        Assert.Equal(expected, AiPlayer.IsBotName(AiPlayer.GetBotName(difficulty)));

    [Theory(DisplayName = "SPEC-0039:UT-01c — IsBotName rejeita nomes de pessoas, vazio e nulo")]
    [Trait("Category", "SPEC-0039:UT-01")]
    [InlineData("Thomas")]
    [InlineData("Robô")]
    [InlineData("")]
    [InlineData(null)]
    public void IsBotName_ShouldRejectOthers(string? name) => Assert.False(AiPlayer.IsBotName(name));
}

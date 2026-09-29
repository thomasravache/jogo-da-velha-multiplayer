using System.Diagnostics;
using TicTacToe.Modules.Chess;

namespace TicTacToe.Tests;

public class ChessBotTests
{
    private const string KiwipeteFen = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
    private const string MiddlegameFen = "r1bq1rk1/pp2bppp/2n1pn2/2pp4/3P1B2/2PBPN2/PP1N1PPP/R2QK2R w KQ - 0 9";

    private static readonly int[] PieceValues = [100, 320, 330, 500, 900, 0];

    private static async Task<Move?> ChooseAsync(IChessBot bot, string fen) =>
        await bot.ChooseMoveAsync(Position.FromFen(fen), CancellationToken.None);

    private static int Material(Position position, PieceColor color)
    {
        var total = 0;
        for (var i = 0; i < 64; i++)
        {
            if (position.PieceAt(new Square(i)) is { } piece && piece.Color == color)
            {
                total += PieceValues[(int)piece.Type];
            }
        }

        return total;
    }

    private static async Task<Move?> RandomMoveAsync(Random random, Position position)
    {
        await Task.CompletedTask;
        var moves = position.LegalMoves();
        return moves.Count == 0 ? null : moves[random.Next(moves.Count)];
    }

    /// <summary>
    /// Joga uma partida (máx. de meios-lances; no teto, adjudica por material). Devolve +1 se as brancas vencem,
    /// -1 se as pretas vencem e 0 para empate. Todo lance é validado como legal.
    /// </summary>
    private static async Task<int> PlayAsync(
        Func<Position, Task<Move?>> white, Func<Position, Task<Move?>> black, int maxPlies)
    {
        var game = new ChessGame();
        for (var ply = 0; ply < maxPlies && !game.IsOver; ply++)
        {
            var chooser = game.Position.SideToMove == PieceColor.White ? white : black;
            var move = await chooser(game.Position);
            Assert.NotNull(move);
            Assert.Contains(move.Value, game.LegalMoves());
            Assert.True(game.TryPlay(move.Value.From, move.Value.To, move.Value.Promotion, out _));
        }

        if (game.Result is { } result)
        {
            return result.Outcome switch
            {
                ChessOutcome.WhiteWins => 1,
                ChessOutcome.BlackWins => -1,
                _ => 0,
            };
        }

        return Math.Sign(Material(game.Position, PieceColor.White) - Material(game.Position, PieceColor.Black));
    }

    private static Func<Position, Task<Move?>> Choose(IChessBot bot) =>
        position => bot.ChooseMoveAsync(position, CancellationToken.None);

    [Fact(DisplayName = "SPEC-0054:UT-02 — todo lance devolvido é legal ao longo de partidas de robôs")]
    [Trait("Category", "SPEC-0054:UT-02")]
    public async Task Bots_AlwaysReturnLegalMoves()
    {
        // PlayAsync valida a legalidade de cada lance escolhido.
        await PlayAsync(Choose(ChessBots.Create(ChessBotLevel.Easy, 1)), Choose(ChessBots.Create(ChessBotLevel.Easy, 2)), 80);
        await PlayAsync(
            Choose(ChessBots.Create(ChessBotLevel.Medium, 3, maxNodes: 4_000)),
            Choose(ChessBots.Create(ChessBotLevel.Easy, 4)),
            60);
        await PlayAsync(
            Choose(ChessBots.Create(ChessBotLevel.Easy, 5)),
            Choose(ChessBots.Create(ChessBotLevel.Medium, 6, maxNodes: 4_000)),
            60);
    }

    [Theory(DisplayName = "SPEC-0054:UT-02 — retorno nulo somente sem lances legais")]
    [Trait("Category", "SPEC-0054:UT-02")]
    [InlineData(ChessBotLevel.Easy)]
    [InlineData(ChessBotLevel.Medium)]
    public async Task Bots_ReturnNull_OnlyWhenNoLegalMoves(ChessBotLevel level)
    {
        var bot = ChessBots.Create(level, 7);

        var mated = await ChooseAsync(bot, "rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3");
        var stalemated = await ChooseAsync(bot, "7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");
        var normal = await ChooseAsync(bot, KiwipeteFen);

        Assert.Null(mated);
        Assert.Null(stalemated);
        Assert.NotNull(normal);
    }

    [Fact(DisplayName = "SPEC-0054:UT-02 — nomes e fábrica dos níveis")]
    [Trait("Category", "SPEC-0054:UT-02")]
    public void Factory_CreatesBotsWithNames()
    {
        Assert.Equal("Robô Fácil 🤖", ChessBots.Create(ChessBotLevel.Easy).Name);
        Assert.Equal("Robô Médio 🤖", ChessBots.Create(ChessBotLevel.Medium).Name);
        Assert.Equal("Robô Fácil 🤖", ChessBots.NameOf(ChessBotLevel.Easy));
        Assert.Equal("Robô Médio 🤖", ChessBots.NameOf(ChessBotLevel.Medium));
    }

    [Fact(DisplayName = "SPEC-0054:UT-03 — Médio dá o mate em 1")]
    [Trait("Category", "SPEC-0054:UT-03")]
    public async Task Medium_PlaysMateInOne()
    {
        var position = Position.FromFen("6k1/5ppp/8/8/8/8/5PPP/R5K1 w - - 0 1");

        var move = await ChessBots.Create(ChessBotLevel.Medium, 1).ChooseMoveAsync(position, CancellationToken.None);

        Assert.NotNull(move);
        var next = position.Apply(move.Value);
        Assert.True(next.IsInCheck(PieceColor.Black));
        Assert.Empty(next.LegalMoves());
    }

    [Fact(DisplayName = "SPEC-0054:UT-03 — Médio captura a dama desprotegida")]
    [Trait("Category", "SPEC-0054:UT-03")]
    public async Task Medium_CapturesHangingQueen()
    {
        var move = await ChooseAsync(ChessBots.Create(ChessBotLevel.Medium, 1), "4k3/8/8/3q4/8/8/8/3RK3 w - - 0 1");

        Assert.Equal(new Move(Square.Parse("d1"), Square.Parse("d5")), move);
    }

    [Fact(DisplayName = "SPEC-0054:UT-03 — Médio não deixa a própria dama ser capturada")]
    [Trait("Category", "SPEC-0054:UT-03")]
    public async Task Medium_SavesThreatenedQueen()
    {
        // Qxe5+ perderia a dama para dxe5; a dama está atacada pelo peão e precisa sair (ou ficar protegida).
        var position = Position.FromFen("4k3/8/3p4/4p3/3Q4/8/8/4K3 w - - 0 1");

        var move = await ChessBots.Create(ChessBotLevel.Medium, 1).ChooseMoveAsync(position, CancellationToken.None);

        Assert.NotNull(move);
        var next = position.Apply(move.Value);
        var queenCaptured = next.LegalMoves().Any(m => next.PieceAt(m.To) is { Type: PieceType.Queen, Color: PieceColor.White });
        Assert.False(queenCaptured);
    }

    [Theory(DisplayName = "SPEC-0054:UT-04 — mesma semente e posição dão o mesmo lance")]
    [Trait("Category", "SPEC-0054:UT-04")]
    [InlineData(ChessBotLevel.Easy)]
    [InlineData(ChessBotLevel.Medium)]
    public async Task SameSeed_SamePosition_SameMove(ChessBotLevel level)
    {
        foreach (var fen in new[] { Position.Start.ToFen(), MiddlegameFen })
        {
            var first = await ChooseAsync(ChessBots.Create(level, 42, maxNodes: 20_000), fen);
            var second = await ChooseAsync(ChessBots.Create(level, 42, maxNodes: 20_000), fen);

            Assert.Equal(first, second);
        }
    }

    [Fact(DisplayName = "SPEC-0054:UT-04 — Fácil com sementes diferentes varia na abertura")]
    [Trait("Category", "SPEC-0054:UT-04")]
    public async Task Easy_DifferentSeeds_ProduceDifferentMoves()
    {
        var moves = new HashSet<Move?>();
        for (var seed = 0; seed < 30; seed++)
        {
            moves.Add(await ChessBots.Create(ChessBotLevel.Easy, seed).ChooseMoveAsync(Position.Start, CancellationToken.None));
        }

        Assert.True(moves.Count >= 2);
    }

    [Fact(DisplayName = "SPEC-0054:UT-05 — maxNodes pequeno limita a busca e ainda devolve lance legal")]
    [Trait("Category", "SPEC-0054:UT-05")]
    public async Task Medium_RespectsNodeCap()
    {
        var position = Position.FromFen(KiwipeteFen);
        var bot = new MediumChessBot(seed: 1, maxNodes: 500);

        var move = await bot.ChooseMoveAsync(position, CancellationToken.None);

        Assert.NotNull(move);
        Assert.Contains(move.Value, position.LegalMoves());
        Assert.InRange(bot.LastNodeCount, 1, 500);
    }

    [Fact(DisplayName = "SPEC-0054:UT-05 — teto padrão de 200 mil nós é respeitado")]
    [Trait("Category", "SPEC-0054:UT-05")]
    public async Task Medium_DefaultCap_IsRespected()
    {
        var bot = new MediumChessBot(seed: 1);

        await bot.ChooseMoveAsync(Position.FromFen(KiwipeteFen), CancellationToken.None);

        Assert.InRange(bot.LastNodeCount, 1, MediumChessBot.DefaultMaxNodes);
    }

    [Fact(DisplayName = "SPEC-0054:UT-05 — cancelar no meio da busca devolve lance legal em menos de 100 ms")]
    [Trait("Category", "SPEC-0054:UT-05")]
    public async Task Medium_Cancellation_ReturnsLegalMoveQuickly()
    {
        var position = Position.FromFen(KiwipeteFen);
        var bot = new MediumChessBot(seed: 1, maxNodes: int.MaxValue);
        using var cts = new CancellationTokenSource();

        var search = bot.ChooseMoveAsync(position, cts.Token);
        await Task.Delay(30);
        await cts.CancelAsync();
        var sinceCancel = Stopwatch.StartNew();
        var move = await search;
        sinceCancel.Stop();

        Assert.NotNull(move);
        Assert.Contains(move.Value, position.LegalMoves());
        Assert.True(sinceCancel.ElapsedMilliseconds < 100, $"Levou {sinceCancel.ElapsedMilliseconds} ms após cancelar");
    }

    [Fact(DisplayName = "SPEC-0054:UT-05 — token já cancelado ainda devolve lance legal")]
    [Trait("Category", "SPEC-0054:UT-05")]
    public async Task Bots_AlreadyCancelled_StillReturnLegalMove()
    {
        var position = Position.FromFen(KiwipeteFen);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        foreach (var level in new[] { ChessBotLevel.Easy, ChessBotLevel.Medium })
        {
            var move = await ChessBots.Create(level, 1).ChooseMoveAsync(position, cts.Token);

            Assert.NotNull(move);
            Assert.Contains(move.Value, position.LegalMoves());
        }
    }

    [Fact(DisplayName = "SPEC-0054:IT-01 — Médio vence o Fácil e o Fácil vence o aleatório no agregado")]
    [Trait("Category", "SPEC-0054:IT-01")]
    public async Task RelativeStrength_MediumBeatsEasy_EasyBeatsRandom()
    {
        const int gamesPerMatch = 6;
        const int maxPlies = 160;
        var clock = Stopwatch.StartNew();

        var mediumVsEasy = await Task.WhenAll(Enumerable.Range(0, gamesPerMatch).Select(async i =>
        {
            var medium = Choose(ChessBots.Create(ChessBotLevel.Medium, 100 + i));
            var easy = Choose(ChessBots.Create(ChessBotLevel.Easy, 200 + i));
            var mediumIsWhite = i % 2 == 0;
            var score = await Task.Run(() => mediumIsWhite
                ? PlayAsync(medium, easy, maxPlies)
                : PlayAsync(easy, medium, maxPlies));
            return mediumIsWhite ? score : -score;
        }));

        var easyVsRandom = await Task.WhenAll(Enumerable.Range(0, gamesPerMatch).Select(async i =>
        {
            var easy = Choose(ChessBots.Create(ChessBotLevel.Easy, 300 + i));
            var random = new Random(400 + i);
            Func<Position, Task<Move?>> randomBot = position => RandomMoveAsync(random, position);
            var easyIsWhite = i % 2 == 0;
            var score = await Task.Run(() => easyIsWhite
                ? PlayAsync(easy, randomBot, maxPlies)
                : PlayAsync(randomBot, easy, maxPlies));
            return easyIsWhite ? score : -score;
        }));

        clock.Stop();
        var mediumWins = mediumVsEasy.Count(s => s > 0);
        var easyWins = easyVsRandom.Count(s => s > 0);

        Assert.True(mediumWins >= 5, $"Médio venceu {mediumWins}/{gamesPerMatch} contra o Fácil ({string.Join(',', mediumVsEasy)})");
        Assert.True(easyWins >= 4, $"Fácil venceu {easyWins}/{gamesPerMatch} contra o aleatório ({string.Join(',', easyVsRandom)})");
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(60), $"Tempo total {clock.Elapsed.TotalSeconds:F1}s");
    }

    [Fact(DisplayName = "SPEC-0054:IT-02 — Médio responde em menos de 2 s numa posição de meio de jogo")]
    [Trait("Category", "SPEC-0054:IT-02")]
    public async Task Medium_RespondsWithinTwoSeconds()
    {
        var bot = ChessBots.Create(ChessBotLevel.Medium, 1);
        var watch = Stopwatch.StartNew();

        var move = await bot.ChooseMoveAsync(Position.FromFen(MiddlegameFen), CancellationToken.None);
        watch.Stop();

        Assert.NotNull(move);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"Levou {watch.ElapsedMilliseconds} ms");
    }
}

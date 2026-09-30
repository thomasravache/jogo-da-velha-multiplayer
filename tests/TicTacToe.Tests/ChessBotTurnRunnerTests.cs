using TicTacToe.Modules.Chess;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0058:UT-01 — executor do lance do robô (atraso injetável, vez, fim, troca de cores e cancelamento)

public sealed class ChessBotTurnRunnerTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(600);

    private sealed class ScriptedBot(string? from, string? to) : IChessBot
    {
        public string Name => "Roteiro";

        public string? SeenFen { get; private set; }

        public int Calls { get; private set; }

        public Task<Move?> ChooseMoveAsync(Position position, CancellationToken ct)
        {
            Calls++;
            SeenFen = position.ToFen();
            Move? move = from is null ? null : new Move(Square.Parse(from), Square.Parse(to!));
            return Task.FromResult(move);
        }
    }

    private static ChessSession NewSession(ManualTime time, int botSeat)
    {
        var session = new ChessSession(TimeControl.Blitz, time, enableBackgroundTimer: false) { Mode = ChessMode.Solo };
        var humanSeat = 1 - botSeat;
        session.SetSeat(humanSeat, "Ana", Guid.NewGuid(), humanSeat == 0 ? PieceColor.White : PieceColor.Black);
        session.SetSeat(botSeat, "Robô", null, botSeat == 0 ? PieceColor.White : PieceColor.Black);
        return session;
    }

    [Fact(DisplayName = "SPEC-0058:UT-01 — joga só depois do atraso, sobre a posição atual")]
    [Trait("Category", "SPEC-0058:UT-01")]
    public async Task Run_ShouldMoveOnlyAfterDelay()
    {
        var time = new ManualTime();
        using var session = NewSession(time, botSeat: 1);
        ChessSessionTests.Line(session, "e2e4");
        var bot = new ScriptedBot("e7", "e5");

        var run = new ChessBotTurnRunner(time).RunAsync(session, 1, bot, Delay, CancellationToken.None);
        Assert.False(run.IsCompleted);
        Assert.Single(session.Snapshot().Moves);

        time.Advance(Delay);
        await run;

        Assert.Equal(2, session.Snapshot().Moves.Count);
        Assert.Equal(PieceColor.White, session.Snapshot().SideToMove);
        Assert.Equal("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", bot.SeenFen);
    }

    [Fact(DisplayName = "SPEC-0058:UT-01 — não joga se a vez não é do robô")]
    [Trait("Category", "SPEC-0058:UT-01")]
    public async Task Run_ShouldSkipWhenNotBotTurn()
    {
        var time = new ManualTime();
        using var session = NewSession(time, botSeat: 1); // brancas (humano) abrem
        var bot = new ScriptedBot("e7", "e5");

        var run = new ChessBotTurnRunner(time).RunAsync(session, 1, bot, Delay, CancellationToken.None);
        time.Advance(Delay);
        await run;

        Assert.Empty(session.Snapshot().Moves);
        Assert.Equal(0, bot.Calls);
    }

    [Fact(DisplayName = "SPEC-0058:UT-01 — não joga com a partida encerrada")]
    [Trait("Category", "SPEC-0058:UT-01")]
    public async Task Run_ShouldSkipWhenGameOver()
    {
        var time = new ManualTime();
        using var session = NewSession(time, botSeat: 1);
        ChessSessionTests.Line(session, "e2e4");
        var bot = new ScriptedBot("e7", "e5");

        var run = new ChessBotTurnRunner(time).RunAsync(session, 1, bot, Delay, CancellationToken.None);
        Assert.True(session.Forfeit(PieceColor.White, ChessEndReason.Resignation));
        time.Advance(Delay);
        await run;

        Assert.Single(session.Snapshot().Moves);
        Assert.Equal(0, bot.Calls);
    }

    [Fact(DisplayName = "SPEC-0058:UT-01 — a cor do robô é relida na hora: troca de cores na revanche não faz jogar fora de hora")]
    [Trait("Category", "SPEC-0058:UT-01")]
    public async Task Run_ShouldRereadColorAfterRematchSwap()
    {
        var time = new ManualTime();
        using var session = NewSession(time, botSeat: 0); // robô de brancas abriria
        var bot = new ScriptedBot("e2", "e4");

        var run = new ChessBotTurnRunner(time).RunAsync(session, 0, bot, Delay, CancellationToken.None);
        Assert.True(session.Forfeit(PieceColor.Black, ChessEndReason.Resignation));
        Assert.True(session.RequestRematch(PieceColor.White)); // solo: reinicia na hora, trocando as cores
        Assert.Equal(PieceColor.Black, session.ColorOf(0));
        time.Advance(Delay);
        await run;

        Assert.Empty(session.Snapshot().Moves);
        Assert.Equal(0, bot.Calls);
    }

    [Fact(DisplayName = "SPEC-0058:UT-01 — token cancelado durante o atraso não joga")]
    [Trait("Category", "SPEC-0058:UT-01")]
    public async Task Run_ShouldNotMoveWhenCancelled()
    {
        var time = new ManualTime();
        using var session = NewSession(time, botSeat: 0);
        var bot = new ScriptedBot("e2", "e4");
        using var cts = new CancellationTokenSource();

        var run = new ChessBotTurnRunner(time).RunAsync(session, 0, bot, Delay, cts.Token);
        await cts.CancelAsync();
        time.Advance(Delay);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Empty(session.Snapshot().Moves);
        Assert.Equal(0, bot.Calls);
    }

    [Fact(DisplayName = "SPEC-0058:UT-01 — robô sem lance (nulo) não altera a partida")]
    [Trait("Category", "SPEC-0058:UT-01")]
    public async Task Run_ShouldIgnoreNullMove()
    {
        var time = new ManualTime();
        using var session = NewSession(time, botSeat: 0);
        var bot = new ScriptedBot(null, null);

        var run = new ChessBotTurnRunner(time).RunAsync(session, 0, bot, Delay, CancellationToken.None);
        time.Advance(Delay);
        await run;

        Assert.Empty(session.Snapshot().Moves);
        Assert.Equal(1, bot.Calls);
    }
}

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Modules.Chess;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Services.Presence;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0060: presença genérica (relator de conexão) e W.O. por desconexão do xadrez em duas telas

public sealed class ChessDisconnectTests : IDisposable
{
    private readonly ChessDuoHarness _h = new();

    public void Dispose() => _h.Dispose();

    // ---------- UT-04 ----------

    [Fact(DisplayName = "SPEC-0060:UT-04 — O handler chama o relator na queda, no retorno e no fechamento, e solta o contexto")]
    [Trait("Category", "SPEC-0060:UT-04")]
    public async Task Handler_ShouldCallGenericReporter()
    {
        var context = new MatchPresenceContext();
        var handler = new MatchPresenceCircuitHandler(context);
        var reports = new List<bool>();

        await handler.OnConnectionDownAsync(null!, CancellationToken.None); // sem nada anexado: sem efeito
        Assert.Empty(reports);

        context.Attach(reports.Add);
        await handler.OnConnectionDownAsync(null!, CancellationToken.None);
        await handler.OnConnectionUpAsync(null!, CancellationToken.None);
        Assert.Equal([false, true], reports);

        await handler.OnCircuitClosedAsync(null!, CancellationToken.None);
        Assert.Equal([false, true, false], reports);

        await handler.OnConnectionDownAsync(null!, CancellationToken.None); // contexto solto: nada mais é relatado
        Assert.Equal(3, reports.Count);
    }

    [Fact(DisplayName = "SPEC-0060:UT-04 — Detach limpa relator e partida; Attach(GameSession, Player) segue igual")]
    [Trait("Category", "SPEC-0060:UT-04")]
    public async Task Context_ShouldKeepLegacyAttachAndDetachBoth()
    {
        using var game = new GameSession(enableBackgroundTimer: false, timeProvider: new ManualTime()) { Mode = GameMode.Online };
        var context = new MatchPresenceContext();
        var handler = new MatchPresenceCircuitHandler(context);
        var reports = new List<bool>();

        context.Attach(game, Player.O);
        Assert.Same(game, context.Session);
        Assert.Equal(Player.O, context.Player);
        await handler.OnConnectionDownAsync(null!, CancellationToken.None);
        Assert.NotNull(game.DisconnectSecondsLeft(Player.O));

        context.Attach(reports.Add); // troca a associação: a partida antiga deixa de receber
        await handler.OnConnectionUpAsync(null!, CancellationToken.None);
        Assert.Equal([true], reports);
        Assert.NotNull(game.DisconnectSecondsLeft(Player.O));

        context.Detach();
        Assert.Null(context.Session);
        await handler.OnConnectionDownAsync(null!, CancellationToken.None);
        Assert.Equal([true], reports);
    }

    // ---------- IT-03 ----------

    [Fact(DisplayName = "SPEC-0060:IT-03 — Circuito cai e o tempo passa: aviso e depois W.O. com uma única linha Disconnect")]
    [Trait("Category", "SPEC-0060:IT-03")]
    public async Task Disconnect_ShouldWarnThenWalkOverAndRecordOnce()
    {
        var (a, b) = _h.Pair();
        var session = ChessDuoHarness.SessionOf(a);

        await ChessDuoHarness.CircuitAsync(_h.Contexts[1], up: false); // o circuito da Bia cai

        ChessDuoHarness.Wait(a, () => Assert.Contains("Oponente desconectado. Aguardando reconexão…", ChessDuoHarness.Text(a), StringComparison.Ordinal));
        Assert.DoesNotContain("Oponente desconectado", ChessDuoHarness.Text(b), StringComparison.Ordinal);
        _h.Time.Advance(TimeSpan.FromSeconds(15));

        ChessDuoHarness.Wait(a, () => Assert.Contains("Oponente desconectou. Vitória por W.O.", ChessDuoHarness.Text(a), StringComparison.Ordinal), TimeSpan.FromSeconds(5));
        ChessDuoHarness.Wait(b, () => Assert.Contains("Você foi desconectado. Derrota por W.O.", ChessDuoHarness.Text(b), StringComparison.Ordinal), TimeSpan.FromSeconds(5));
        Assert.Equal(ChessEndReason.Disconnect, session.Result!.Reason);
        Assert.DoesNotContain("Aguardando reconexão", ChessDuoHarness.Text(a), StringComparison.Ordinal);
        await _h.WaitForRowsAsync(1);
        await Task.Delay(100);
        var row = Assert.Single(await _h.RowsAsync());
        Assert.Equal(EndReason.Disconnect, row.EndReason);
        Assert.Equal("Ana", row.WinnerName);
    }

    [Fact(DisplayName = "SPEC-0060:IT-03 — Circuito volta em 5 s: o aviso some e a partida continua")]
    [Trait("Category", "SPEC-0060:IT-03")]
    public async Task Reconnect_ShouldCancelWarning()
    {
        var (a, b) = _h.Pair();
        var session = ChessDuoHarness.SessionOf(a);

        await ChessDuoHarness.CircuitAsync(_h.Contexts[1], up: false);
        ChessDuoHarness.Wait(a, () => Assert.Contains("Aguardando reconexão", ChessDuoHarness.Text(a), StringComparison.Ordinal));
        _h.Time.Advance(TimeSpan.FromSeconds(5));
        await ChessDuoHarness.CircuitAsync(_h.Contexts[1], up: true);

        ChessDuoHarness.Wait(a, () => Assert.DoesNotContain("Aguardando reconexão", ChessDuoHarness.Text(a), StringComparison.Ordinal));
        _h.Time.Advance(TimeSpan.FromSeconds(30));
        session.Tick();
        Assert.False(session.IsOver);
        Assert.True(ChessSessionTests.Play(session, PieceColor.White, "e2", "e4"));
        ChessDuoHarness.Wait(b, () => Assert.Contains("Sua vez", ChessDuoHarness.Text(b), StringComparison.Ordinal));
        Assert.Empty(await _h.RowsAsync());
    }

    [Fact(DisplayName = "SPEC-0060:IT-03 — Sair da tela conta como queda e o outro ganha por W.O.; no fim da partida não vira derrota")]
    [Trait("Category", "SPEC-0060:IT-03")]
    public async Task LeavingPage_ShouldCountAsDisconnectOnlyWhileInProgress()
    {
        var (a, _) = _h.Pair();
        var session = ChessDuoHarness.SessionOf(a);

        await _h.Contexts[1].DisposeComponentsAsync(); // Bia fecha a aba no meio da partida
        Assert.NotNull(session.DisconnectSecondsLeft(PieceColor.Black));
        ChessDuoHarness.Wait(a, () => Assert.Contains("Aguardando reconexão", ChessDuoHarness.Text(a), StringComparison.Ordinal));
        _h.Time.Advance(TimeSpan.FromSeconds(15));
        ChessDuoHarness.Wait(a, () => Assert.Contains("Oponente desconectou. Vitória por W.O.", ChessDuoHarness.Text(a), StringComparison.Ordinal), TimeSpan.FromSeconds(5));

        var (c, _) = _h.Pair();
        var second = ChessDuoHarness.SessionOf(c);
        ChessDuoHarness.FoolsMate(second);
        await _h.Contexts[^1].DisposeComponentsAsync(); // sair depois do fim não altera o resultado
        Assert.Equal(ChessEndReason.Checkmate, second.Result!.Reason);
        Assert.Null(second.DisconnectSecondsLeft(PieceColor.Black));
    }
}

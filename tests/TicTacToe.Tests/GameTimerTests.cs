using System;
using TicTacToe.Modules.Gameplay;
using Xunit;

namespace TicTacToe.Tests;

public class GameTimerTests
{
    [Fact(DisplayName = "SPEC-0027:UT-01 — Inicialização do timer em 15s e IsTimedOut falso")]
    [Trait("Category", "SPEC-0027:UT-01")]
    public void GameSession_ShouldInitializeTimerWith15Seconds()
    {
        using var session = new GameSession();
        Assert.Equal(15, session.RemainingSeconds);
        Assert.False(session.IsTimedOut);
    }

    [Fact(DisplayName = "SPEC-0027:UT-02 — Tick decrementa RemainingSeconds e dispara OnStateChanged")]
    [Trait("Category", "SPEC-0027:UT-02")]
    public void GameSession_Tick_ShouldDecrementRemainingSeconds_AndTriggerEvent()
    {
        using var session = new GameSession();
        bool eventFired = false;
        session.OnStateChanged += () => eventFired = true;

        session.Tick();

        Assert.Equal(14, session.RemainingSeconds);
        Assert.True(eventFired);
    }

    [Fact(DisplayName = "SPEC-0027:UT-03 — MakeMove válido reseta RemainingSeconds para 15")]
    [Trait("Category", "SPEC-0027:UT-03")]
    public void MakeMove_ShouldResetTimerTo15Seconds()
    {
        using var session = new GameSession();
        session.Tick();
        session.Tick();
        Assert.Equal(13, session.RemainingSeconds);

        var moved = session.MakeMove(0, Player.X);

        Assert.True(moved);
        Assert.Equal(15, session.RemainingSeconds);
        Assert.Equal(Player.O, session.CurrentTurn);
    }

    [Fact(DisplayName = "SPEC-0027:UT-04 — Timeout atribui vitória por W.O. ao oponente e incrementa score")]
    [Trait("Category", "SPEC-0027:UT-04")]
    public void Timeout_ShouldDeclareOpponentWinnerByWO_AndIncrementScore()
    {
        using var session = new GameSession();
        session.SetPlayerName(Player.X, "PlayerX");
        session.SetPlayerName(Player.O, "PlayerO");

        for (int i = 0; i < 15; i++)
        {
            session.Tick();
        }

        Assert.Equal(0, session.RemainingSeconds);
        Assert.True(session.IsTimedOut);
        Assert.Equal(Player.O, session.Winner);
        Assert.Equal(1, session.GetScore(Player.O));
        Assert.Equal(0, session.GetScore(Player.X));
    }

    [Fact(DisplayName = "SPEC-0027:UT-05 — Restart reseta RemainingSeconds para 15 e IsTimedOut para false")]
    [Trait("Category", "SPEC-0027:UT-05")]
    public void Restart_ShouldResetTimerAndTimeoutFlag()
    {
        using var session = new GameSession();
        for (int i = 0; i < 15; i++)
        {
            session.Tick();
        }
        Assert.True(session.IsTimedOut);

        session.Restart();

        Assert.Equal(15, session.RemainingSeconds);
        Assert.False(session.IsTimedOut);
        Assert.Equal(Player.None, session.Winner);
        Assert.Equal(Player.X, session.CurrentTurn);
    }

    [Fact(DisplayName = "SPEC-0027:UT-06 — Fim de jogo normal interrompe decremento do timer")]
    [Trait("Category", "SPEC-0027:UT-06")]
    public void NormalGameEnd_ShouldStopTimer()
    {
        using var session = new GameSession();
        session.MakeMove(0, Player.X);
        session.MakeMove(3, Player.O);
        session.MakeMove(1, Player.X);
        session.MakeMove(4, Player.O);
        session.MakeMove(2, Player.X);

        Assert.Equal(Player.X, session.Winner);
        Assert.False(session.IsTimedOut);

        session.Tick();

        Assert.False(session.IsTimedOut);
        Assert.Equal(Player.X, session.Winner);
    }
}

using System;
using Xunit;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;

namespace TicTacToe.Tests;

public class EventSynchronizationTests
{
    [Fact(DisplayName = "SPEC-0018:UT-01 — MakeMove dispara evento OnStateChanged")]
    [Trait("Category", "SPEC-0018:UT-01")]
    public void MakeMove_ShouldTriggerOnStateChanged()
    {
        var game = new GameSession();
        bool eventFired = false;
        game.OnStateChanged += () => eventFired = true;

        game.MakeMove(0, Player.X);

        Assert.True(eventFired);
    }

    [Fact(DisplayName = "SPEC-0018:UT-02 — Restart dispara evento OnStateChanged")]
    [Trait("Category", "SPEC-0018:UT-02")]
    public void Restart_ShouldTriggerOnStateChanged()
    {
        var game = new GameSession();
        game.MakeMove(0, Player.X);

        bool eventFired = false;
        game.OnStateChanged += () => eventFired = true;

        game.Restart();

        Assert.True(eventFired);
    }

    [Fact(DisplayName = "SPEC-0018:IT-01 — Múltiplos ouvintes recebem evento de jogada")]
    [Trait("Category", "SPEC-0018:IT-01")]
    public void MultipleListeners_ShouldAllReceiveOnStateChanged()
    {
        var game = new GameSession();
        int listener1Calls = 0;
        int listener2Calls = 0;

        game.OnStateChanged += () => listener1Calls++;
        game.OnStateChanged += () => listener2Calls++;

        game.MakeMove(0, Player.X);
        game.MakeMove(1, Player.O);

        Assert.Equal(2, listener1Calls);
        Assert.Equal(2, listener2Calls);
    }

    [Fact(DisplayName = "SPEC-0018:UT-03 — MatchmakingService notifica evento OnPlayerMatched")]
    [Trait("Category", "SPEC-0018:UT-03")]
    public void Matchmaking_ShouldTriggerOnPlayerMatched()
    {
        var service = new MatchmakingService();
        string p1 = Guid.NewGuid().ToString();
        string p2 = Guid.NewGuid().ToString();

        Guid matchedId = Guid.Empty;
        service.OnPlayerMatched += (conn, id) =>
        {
            if (conn == p1) matchedId = id;
        };

        service.JoinQueue(p1, "Player1");
        service.JoinQueue(p2, "Player2");

        Assert.NotEqual(Guid.Empty, matchedId);
    }
}

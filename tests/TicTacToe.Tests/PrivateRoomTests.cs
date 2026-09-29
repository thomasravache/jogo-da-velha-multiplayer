using System;
using Xunit;
using TicTacToe.Modules.Matchmaking;

namespace TicTacToe.Tests;

public class PrivateRoomTests
{
    [Fact(DisplayName = "SPEC-0016:UT-01 — CreatePrivateRoom gera código não vazio e armazena sala")]
    [Trait("Category", "SPEC-0016:UT-01")]
    public void CreatePrivateRoom_ShouldReturnValidCode()
    {
        var service = new MatchmakingService();
        string conn = Guid.NewGuid().ToString();

        string code = service.CreatePrivateRoom(conn, "Thomas");

        Assert.False(string.IsNullOrWhiteSpace(code));
        Assert.True(code.Length >= 4);
    }

    [Fact(DisplayName = "SPEC-0016:UT-02 — JoinPrivateRoom com código válido une os dois jogadores")]
    [Trait("Category", "SPEC-0016:UT-02")]
    public void JoinPrivateRoom_WithValidCode_ShouldPairPlayers()
    {
        var service = new MatchmakingService();
        string hostConn = Guid.NewGuid().ToString();
        string guestConn = Guid.NewGuid().ToString();

        string code = service.CreatePrivateRoom(hostConn, "Host");
        Guid? matchId = service.JoinPrivateRoom(code, guestConn, "Guest");

        Assert.NotNull(matchId);
        Assert.True(service.ActiveMatches.TryGetValue(hostConn, out var hostMatch));
        Assert.True(service.ActiveMatches.TryGetValue(guestConn, out var guestMatch));
        Assert.Equal(matchId, hostMatch);
        Assert.Equal(matchId, guestMatch);
        Assert.Equal("Host", service.GetPlayerName(hostConn));
        Assert.Equal("Guest", service.GetPlayerName(guestConn));
    }

    [Fact(DisplayName = "SPEC-0016:UT-03 — JoinPrivateRoom com código inexistente retorna null")]
    [Trait("Category", "SPEC-0016:UT-03")]
    public void JoinPrivateRoom_WithInvalidCode_ShouldReturnNull()
    {
        var service = new MatchmakingService();
        string conn = Guid.NewGuid().ToString();

        Guid? matchId = service.JoinPrivateRoom("CODIGO-INEXISTENTE", conn, "Alice");

        Assert.Null(matchId);
    }

    [Fact(DisplayName = "SPEC-0016:IT-01 — Sala cheia rejeita tentativa de terceiro jogador")]
    [Trait("Category", "SPEC-0016:IT-01")]
    public void JoinPrivateRoom_WhenRoomAlreadyUsed_ShouldRejectThirdPlayer()
    {
        var service = new MatchmakingService();
        string host = Guid.NewGuid().ToString();
        string guest1 = Guid.NewGuid().ToString();
        string guest2 = Guid.NewGuid().ToString();

        string code = service.CreatePrivateRoom(host, "Host");
        Guid? match1 = service.JoinPrivateRoom(code, guest1, "Guest1");
        Guid? match2 = service.JoinPrivateRoom(code, guest2, "Guest2");

        Assert.NotNull(match1);
        Assert.Null(match2); // Já foi consumida pela partida
    }
}

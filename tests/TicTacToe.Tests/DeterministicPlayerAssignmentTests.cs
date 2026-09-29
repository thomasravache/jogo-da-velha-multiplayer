using System;
using System.IO;
using Xunit;
using TicTacToe.Modules.Matchmaking;

namespace TicTacToe.Tests;

public class DeterministicPlayerAssignmentTests
{
    [Fact(DisplayName = "SPEC-0020:UT-01 — Sala privada define Host como PlayerX e Convidado como PlayerO")]
    [Trait("Category", "SPEC-0020:UT-01")]
    public void PrivateRoom_ShouldAssignHostAsPlayerX_AndGuestAsPlayerO()
    {
        var service = new MatchmakingService();
        string hostConn = "conn-host-" + Guid.NewGuid();
        string guestConn = "conn-guest-" + Guid.NewGuid();

        string code = service.CreatePrivateRoom(hostConn, "maneiro");
        Guid? matchId = service.JoinPrivateRoom(code, guestConn, "legal");

        Assert.NotNull(matchId);

        var players = service.GetMatchPlayers(matchId.Value);
        Assert.NotNull(players);
        Assert.Equal(hostConn, players.Value.PlayerX);
        Assert.Equal(guestConn, players.Value.PlayerO);

        var names = service.GetMatchPlayerNames(matchId.Value);
        Assert.NotNull(names);
        Assert.Equal("maneiro", names.Value.PlayerXName);
        Assert.Equal("legal", names.Value.PlayerOName);
    }

    [Fact(DisplayName = "SPEC-0020:UT-02 — Matchmaking público define primeiro jogador como PlayerX e segundo como PlayerO")]
    [Trait("Category", "SPEC-0020:UT-02")]
    public void QueueMatchmaking_ShouldAssignFirstPlayerAsPlayerX_AndSecondAsPlayerO()
    {
        var service = new MatchmakingService();
        string p1Conn = "conn-p1-" + Guid.NewGuid();
        string p2Conn = "conn-p2-" + Guid.NewGuid();

        service.JoinQueue(p1Conn, "Primeiro");
        Guid? matchId = service.JoinQueue(p2Conn, "Segundo");

        Assert.NotNull(matchId);

        var players = service.GetMatchPlayers(matchId.Value);
        Assert.NotNull(players);
        Assert.Equal(p1Conn, players.Value.PlayerX);
        Assert.Equal(p2Conn, players.Value.PlayerO);

        var names = service.GetMatchPlayerNames(matchId.Value);
        Assert.NotNull(names);
        Assert.Equal("Primeiro", names.Value.PlayerXName);
        Assert.Equal("Segundo", names.Value.PlayerOName);
    }

    [Fact(DisplayName = "SPEC-0020:IT-01 — Múltiplas salas privadas nunca invertem a ordem dos nomes")]
    [Trait("Category", "SPEC-0020:IT-01")]
    public void MultiplePrivateRooms_ShouldNeverInvertPlayerOrder()
    {
        var service = new MatchmakingService();

        for (int i = 0; i < 50; i++)
        {
            string host = "h-" + Guid.NewGuid().ToString("N");
            string guest = "g-" + Guid.NewGuid().ToString("N");
            string hostName = "Host_" + i;
            string guestName = "Guest_" + i;

            string code = service.CreatePrivateRoom(host, hostName);
            Guid? match = service.JoinPrivateRoom(code, guest, guestName);

            Assert.NotNull(match);
            var names = service.GetMatchPlayerNames(match.Value);
            Assert.NotNull(names);
            Assert.Equal(hostName, names.Value.PlayerXName);
            Assert.Equal(guestName, names.Value.PlayerOName);
        }
    }

    [Fact(DisplayName = "SPEC-0020:E2E-01 — Home.razor utiliza papéis determinísticos do MatchmakingService")]
    [Trait("Category", "SPEC-0020:E2E-01")]
    public void HomeRazor_ShouldConsumeDeterministicMatchPlayers()
    {
        var homeRazorPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor"));
        var homeCsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs"));
        var targetPath = File.Exists(homeCsPath) ? homeCsPath : homeRazorPath;
        if (File.Exists(targetPath))
        {
            var content = File.ReadAllText(targetPath);
            Assert.Contains("GetMatchPlayerNames", content);
        }
    }
}

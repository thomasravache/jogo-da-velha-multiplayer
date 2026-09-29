using System;
using TicTacToe.Modules.Matchmaking;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0040: série melhor de 5 — pareamento por formato

public class SeriesMatchmakingTests
{
    [Fact(DisplayName = "SPEC-0040:UT-09 — Fila separada por formato: só pares do mesmo formato se juntam")]
    [Trait("Category", "SPEC-0040:UT-09")]
    public void Queue_ShouldPairOnlySameFormat()
    {
        var mm = new MatchmakingService();

        Assert.Null(mm.JoinQueue("a", "A", bestOf: 5));
        Assert.Null(mm.JoinQueue("b", "B", bestOf: 1));
        var series = mm.JoinQueue("c", "C", bestOf: 5);
        var single = mm.JoinQueue("d", "D", bestOf: 1);

        Assert.NotNull(series);
        Assert.NotNull(single);
        Assert.Equal(5, mm.GetMatchBestOf(series!.Value));
        Assert.Equal(1, mm.GetMatchBestOf(single!.Value));
        Assert.Equal(("a", "c"), mm.GetMatchPlayers(series.Value));
        Assert.Equal(("b", "d"), mm.GetMatchPlayers(single.Value));
    }

    [Fact(DisplayName = "SPEC-0040:UT-09b — Formatos diferentes ficam esperando e o padrão continua sendo partida única")]
    [Trait("Category", "SPEC-0040:UT-09")]
    public void Queue_DifferentFormatsShouldWait_AndDefaultIsSingle()
    {
        var mm = new MatchmakingService();

        Assert.Null(mm.JoinQueue("a", "A", bestOf: 5));
        Assert.Null(mm.JoinQueue("b", "B")); // padrão: 1
        var match = mm.JoinQueue("c", "C");
        Assert.NotNull(match);
        Assert.Equal(1, mm.GetMatchBestOf(match!.Value));
        Assert.Equal(1, mm.GetMatchBestOf(Guid.NewGuid())); // desconhecida: partida única
    }

    [Fact(DisplayName = "SPEC-0040:UT-09c — Sala privada herda o formato de quem cria")]
    [Trait("Category", "SPEC-0040:UT-09")]
    public void PrivateRoom_ShouldInheritFormat()
    {
        var mm = new MatchmakingService();
        var five = mm.CreatePrivateRoom("host5", "H5", bestOf: 5);
        var one = mm.CreatePrivateRoom("host1", "H1");

        var m5 = mm.JoinPrivateRoom(five, "g5", "G5");
        var m1 = mm.JoinPrivateRoom(one, "g1", "G1");

        Assert.Equal(5, mm.GetMatchBestOf(m5!.Value));
        Assert.Equal(1, mm.GetMatchBestOf(m1!.Value));
    }
}

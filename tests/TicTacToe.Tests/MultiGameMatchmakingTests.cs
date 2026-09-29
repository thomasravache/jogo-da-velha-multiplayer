using TicTacToe.Modules.Matchmaking;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0047: fila do matchmaking por chave, sala do jogo certo e cancelamento de busca

public class MultiGameMatchmakingTests
{
    [Fact(DisplayName = "SPEC-0047:UT-02 — Mesma chave pareia e devolve a chave; chaves diferentes esperam")]
    [Trait("Category", "SPEC-0047:UT-02")]
    public void Queue_ShouldPairOnlySameKey()
    {
        var mm = new MatchmakingService();

        Assert.Null(mm.JoinQueue("a", "A", queueKey: "xadrez:blitz5+0"));
        Assert.Null(mm.JoinQueue("b", "B", queueKey: "xadrez:rapid10+0"));
        var match = mm.JoinQueue("c", "C", queueKey: "xadrez:blitz5+0");

        Assert.NotNull(match);
        Assert.Equal("xadrez:blitz5+0", mm.GetMatchQueueKey(match!.Value));
        Assert.Equal(("a", "c"), mm.GetMatchPlayers(match.Value));
        Assert.Null(mm.GetMatchPlayers(System.Guid.NewGuid()));

        // "b" continua esperando a sua chave.
        var second = mm.JoinQueue("d", "D", queueKey: "xadrez:rapid10+0");
        Assert.Equal(("b", "d"), mm.GetMatchPlayers(second!.Value));
    }

    [Fact(DisplayName = "SPEC-0047:UT-02b — Sem chave o par se forma por bestOf e a chave efetiva é velha:{bestOf}")]
    [Trait("Category", "SPEC-0047:UT-02")]
    public void Queue_WithoutKey_ShouldUseBestOf()
    {
        var mm = new MatchmakingService();

        Assert.Null(mm.JoinQueue("a", "A", bestOf: 5));
        Assert.Null(mm.JoinQueue("b", "B"));
        var series = mm.JoinQueue("c", "C", bestOf: 5);
        var single = mm.JoinQueue("d", "D");

        Assert.Equal("velha:5", mm.GetMatchQueueKey(series!.Value));
        Assert.Equal("velha:1", mm.GetMatchQueueKey(single!.Value));
        Assert.Equal(5, mm.GetMatchBestOf(series.Value));
        Assert.Equal(1, mm.GetMatchBestOf(single.Value));
    }

    [Fact(DisplayName = "SPEC-0047:UT-02c — Sala privada criada com chave entrega a chave a quem entra")]
    [Trait("Category", "SPEC-0047:UT-02")]
    public void PrivateRoom_ShouldCarryKey()
    {
        var mm = new MatchmakingService();
        var code = mm.CreatePrivateRoom("host", "Host", queueKey: "xadrez:blitz5+0");

        var match = mm.JoinPrivateRoom(code, "guest", "Guest", game: "xadrez");

        Assert.NotNull(match);
        Assert.Equal("xadrez:blitz5+0", mm.GetMatchQueueKey(match!.Value));
        Assert.True(mm.IsPrivateMatch(match.Value));

        var velhaCode = mm.CreatePrivateRoom("h2", "H2", bestOf: 5);
        var velha = mm.JoinPrivateRoom(velhaCode, "g2", "G2");
        Assert.Equal("velha:5", mm.GetMatchQueueKey(velha!.Value));
        Assert.Equal(5, mm.GetMatchBestOf(velha.Value));
    }

    [Fact(DisplayName = "SPEC-0047:UT-03 — Sala do jogo errado é recusada e continua esperando")]
    [Trait("Category", "SPEC-0047:UT-03")]
    public void JoinPrivateRoom_ShouldRejectRoomOfAnotherGame()
    {
        var mm = new MatchmakingService();
        var chessRoom = mm.CreatePrivateRoom("hx", "HX", queueKey: "xadrez:blitz5+0");
        var velhaRoom = mm.CreatePrivateRoom("hv", "HV");

        Assert.Null(mm.JoinPrivateRoom(chessRoom, "g1", "G1")); // padrão: velha
        Assert.Null(mm.JoinPrivateRoom(velhaRoom, "g2", "G2", game: "xadrez"));

        Assert.NotNull(mm.JoinPrivateRoom(chessRoom, "g3", "G3", game: "xadrez"));
        Assert.NotNull(mm.JoinPrivateRoom(velhaRoom, "g4", "G4"));
    }

    [Fact(DisplayName = "SPEC-0047:UT-03b — LeaveQueue tira a conexão da fila, é idempotente e não afeta quem já pareou")]
    [Trait("Category", "SPEC-0047:UT-03")]
    public void LeaveQueue_ShouldRemoveWaitingConnection()
    {
        var mm = new MatchmakingService();
        Assert.Null(mm.JoinQueue("a", "A", queueKey: "xadrez:blitz5+0"));

        mm.LeaveQueue("a");
        mm.LeaveQueue("a");
        mm.LeaveQueue("desconhecida");

        Assert.Null(mm.JoinQueue("b", "B", queueKey: "xadrez:blitz5+0"));
        var match = mm.JoinQueue("c", "C", queueKey: "xadrez:blitz5+0");
        Assert.Equal(("b", "c"), mm.GetMatchPlayers(match!.Value));

        mm.LeaveQueue("b");
        Assert.Equal(("b", "c"), mm.GetMatchPlayers(match.Value));
        Assert.Equal(match, mm.ActiveMatches["b"]);
    }

    [Fact(DisplayName = "SPEC-0047:UT-03c — CancelPrivateRoom remove a sala que espera e preserva partidas já formadas")]
    [Trait("Category", "SPEC-0047:UT-03")]
    public void CancelPrivateRoom_ShouldRemoveWaitingRooms()
    {
        var mm = new MatchmakingService();
        var waiting = mm.CreatePrivateRoom("host", "Host");
        var other = mm.CreatePrivateRoom("outro", "Outro");
        var started = mm.CreatePrivateRoom("h3", "H3");
        var match = mm.JoinPrivateRoom(started, "g3", "G3");

        mm.CancelPrivateRoom("host");
        mm.CancelPrivateRoom("host");
        mm.CancelPrivateRoom("h3");

        Assert.Null(mm.JoinPrivateRoom(waiting, "g1", "G1"));
        Assert.NotNull(mm.JoinPrivateRoom(other, "g2", "G2"));
        Assert.Equal(("h3", "g3"), mm.GetMatchPlayers(match!.Value));
    }
}

using TicTacToe.Modules.Chess;
using TicTacToe.Modules.Matchmaking;

namespace TicTacToe.Tests;

// SPEC-0053: pareamento por controle de tempo, preferência de cor e atribuição de cores

public class ChessMatchmakingTests
{
    [Fact(DisplayName = "SPEC-0053:UT-01 — Só se pareiam jogadores com o mesmo controle; sala privada herda a chave")]
    [Trait("Category", "SPEC-0053:UT-01")]
    public void Queue_ShouldPairOnlySameTimeControl()
    {
        var mm = new MatchmakingService();

        Assert.Null(mm.JoinQueue("a", "A", queueKey: "xadrez:blitz5+0"));
        Assert.Null(mm.JoinQueue("b", "B", queueKey: "xadrez:bullet1+0"));
        Assert.Null(mm.JoinQueue("v", "V", queueKey: "velha:1"));
        var match = mm.JoinQueue("c", "C", queueKey: "xadrez:blitz5+0");

        Assert.NotNull(match);
        Assert.Equal(("a", "c"), mm.GetMatchPlayers(match!.Value));
        Assert.Equal("xadrez:blitz5+0", mm.GetMatchQueueKey(match.Value));
        var bullet = mm.JoinQueue("d", "D", queueKey: "xadrez:bullet1+0");
        Assert.Equal(("b", "d"), mm.GetMatchPlayers(bullet!.Value));

        var code = mm.CreatePrivateRoom("h", "H", queueKey: "xadrez:rapida10+5");
        var privateMatch = mm.JoinPrivateRoom(code, "g", "G", game: "xadrez");

        Assert.NotNull(privateMatch);
        Assert.Equal("xadrez:rapida10+5", mm.GetMatchQueueKey(privateMatch!.Value));
        Assert.True(mm.IsPrivateMatch(privateMatch.Value));
    }

    [Theory(DisplayName = "SPEC-0053:UT-02 — Preferências compatíveis são respeitadas sem consultar o sorteio")]
    [Trait("Category", "SPEC-0053:UT-02")]
    [InlineData(ColorPreference.White, ColorPreference.Black, PieceColor.White)]
    [InlineData(ColorPreference.White, ColorPreference.Random, PieceColor.White)]
    [InlineData(ColorPreference.Black, ColorPreference.White, PieceColor.Black)]
    [InlineData(ColorPreference.Black, ColorPreference.Random, PieceColor.Black)]
    [InlineData(ColorPreference.Random, ColorPreference.White, PieceColor.Black)]
    [InlineData(ColorPreference.Random, ColorPreference.Black, PieceColor.White)]
    public void AssignFirst_ShouldRespectCompatiblePreferences(ColorPreference first, ColorPreference second, PieceColor expected)
    {
        var color = ColorAssignment.AssignFirst(first, second, () => throw new InvalidOperationException("sorteio não devia ser consultado"));

        Assert.Equal(expected, color);
    }

    [Theory(DisplayName = "SPEC-0053:UT-02 — Empates de preferência decidem pelo sorteio")]
    [Trait("Category", "SPEC-0053:UT-02")]
    [InlineData(ColorPreference.White, ColorPreference.White, true, PieceColor.White)]
    [InlineData(ColorPreference.White, ColorPreference.White, false, PieceColor.Black)]
    [InlineData(ColorPreference.Black, ColorPreference.Black, true, PieceColor.White)]
    [InlineData(ColorPreference.Black, ColorPreference.Black, false, PieceColor.Black)]
    [InlineData(ColorPreference.Random, ColorPreference.Random, true, PieceColor.White)]
    [InlineData(ColorPreference.Random, ColorPreference.Random, false, PieceColor.Black)]
    public void AssignFirst_ShouldUseCoinFlipOnTies(ColorPreference first, ColorPreference second, bool coin, PieceColor expected)
    {
        var flips = 0;

        var color = ColorAssignment.AssignFirst(first, second, () =>
        {
            flips++;
            return coin;
        });

        Assert.Equal(expected, color);
        Assert.Equal(1, flips);
    }

    [Fact(DisplayName = "SPEC-0053:UT-03 — Preferência é guardada por conexão como texto opaco; ausente é nula")]
    [Trait("Category", "SPEC-0053:UT-03")]
    public void Preference_ShouldBeStoredPerConnection()
    {
        var mm = new MatchmakingService();

        Assert.Null(mm.GetPreference("a"));
        mm.SetMatchPreference("a", "White");
        mm.SetMatchPreference("b", "algo-opaco");
        mm.SetMatchPreference("a", "Black");

        Assert.Equal("Black", mm.GetPreference("a"));
        Assert.Equal("algo-opaco", mm.GetPreference("b"));
        Assert.Null(mm.GetPreference("c"));
    }

    [Fact(DisplayName = "SPEC-0053:UT-03 — Preferência definida antes de JoinQueue já está disponível no evento de pareamento")]
    [Trait("Category", "SPEC-0053:UT-03")]
    public void Preference_ShouldBeAvailableWhenMatchedEventFires()
    {
        var mm = new MatchmakingService();
        var seen = new List<string?>();
        mm.OnPlayerMatched += (connectionId, _) => seen.Add(mm.GetPreference(connectionId));

        mm.SetMatchPreference("a", "White");
        mm.SetMatchPreference("b", "Random");
        mm.JoinQueue("a", "A", queueKey: "xadrez:blitz5+0");
        mm.JoinQueue("b", "B", queueKey: "xadrez:blitz5+0");

        Assert.Equal(["White", "Random"], seen);
    }

    [Fact(DisplayName = "SPEC-0053:CH-01 — Fila do jogo da velha por bestOf continua igual")]
    [Trait("Category", "SPEC-0053:CH-01")]
    public void TicTacToeQueue_ShouldKeepPairingByBestOf()
    {
        var mm = new MatchmakingService();

        Assert.Null(mm.JoinQueue("a", "A", bestOf: 5));
        Assert.Null(mm.JoinQueue("b", "B"));
        var series = mm.JoinQueue("c", "C", bestOf: 5);

        Assert.NotNull(series);
        Assert.Equal("velha:5", mm.GetMatchQueueKey(series!.Value));
        Assert.Equal(5, mm.GetMatchBestOf(series.Value));
        Assert.Equal(("a", "c"), mm.GetMatchPlayers(series.Value));
    }
}

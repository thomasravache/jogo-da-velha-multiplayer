namespace TicTacToe.Modules.Gameplay;

/// <summary>Regras puras da classificação: exclusões, estatísticas por jogador, ordem e paginação.</summary>
public static class LeaderboardAnalysis
{
    public static bool Counts(LeaderboardGame game) => throw new NotImplementedException();

    public static IReadOnlyList<LeaderboardEntry> Rank(IEnumerable<LeaderboardGame> games, Guid? myPlayerId) => throw new NotImplementedException();

    public static LeaderboardPage Paginate(IReadOnlyList<LeaderboardEntry> ranked, int page, int pageSize) => throw new NotImplementedException();
}

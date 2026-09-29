namespace TicTacToe.Modules.Gameplay;

/// <summary>Regras puras da classificação: exclusões, estatísticas por jogador, ordem e paginação.</summary>
public static class LeaderboardAnalysis
{
    public static bool Counts(LeaderboardGame game) =>
        game.Mode != GameMode.Solo
        && !(game.Mode is null && (AiPlayer.IsBotName(game.PlayerXName) || AiPlayer.IsBotName(game.PlayerOName)));

    public static IReadOnlyList<LeaderboardEntry> Rank(IEnumerable<LeaderboardGame> games, Guid? myPlayerId)
    {
        // Por jogador: (chave, resultado, nome, data) de cada partida, da mais recente à mais antiga.
        var perPlayer = new Dictionary<string, List<(char Result, string Name, DateTime At)>>();
        var myKey = myPlayerId is { } id ? Key(id, null) : null;

        foreach (var g in games.Where(Counts))
        {
            var (x, o) = Results(g);
            Add(perPlayer, Key(g.PlayerXId, g.PlayerXName), x, g.PlayerXName, g.PlayedAtUtc);
            Add(perPlayer, Key(g.PlayerOId, g.PlayerOName), o, g.PlayerOName, g.PlayedAtUtc);
        }

        var stats = perPlayer
            .Select(kv =>
            {
                var list = kv.Value.Where(r => r.Result != '?').OrderByDescending(r => r.At).ToList();
                var wins = list.Count(r => r.Result == 'W');
                var losses = list.Count(r => r.Result == 'L');
                var draws = list.Count(r => r.Result == 'D');
                var total = wins + losses + draws;
                return (Key: kv.Key, Wins: wins, Losses: losses, Draws: draws, Total: total,
                    Name: kv.Value.OrderByDescending(r => r.At).First().Name,
                    Streak: list.TakeWhile(r => r.Result == 'W').Count(),
                    LastWin: wins > 0 ? list.First(r => r.Result == 'W').At : default);
            })
            .Where(s => s.Wins > 0)
            .OrderByDescending(s => s.Wins).ThenByDescending(s => s.LastWin).ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();

        return stats.Select((s, i) => new LeaderboardEntry(
            i + 1, s.Name, s.Wins, s.Losses, s.Draws,
            Math.Round(100.0 * s.Wins / s.Total, 1, MidpointRounding.AwayFromZero),
            s.Streak, s.LastWin, s.Key == myKey)).ToList();
    }

    public static LeaderboardPage Paginate(IReadOnlyList<LeaderboardEntry> ranked, int page, int pageSize)
    {
        pageSize = Math.Max(1, pageSize);
        var pageCount = Math.Max(1, (int)Math.Ceiling(ranked.Count / (double)pageSize));
        page = Math.Clamp(page, 1, pageCount);
        var items = ranked.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new LeaderboardPage(items, ranked.Count, page, pageCount, ranked.FirstOrDefault(e => e.IsMe));
    }

    private static string Key(Guid? id, string? name) => id is { } g ? $"id:{g}" : $"name:{name}";

    private static void Add(Dictionary<string, List<(char, string, DateTime)>> map, string key, char result, string name, DateTime at)
    {
        if (!map.TryGetValue(key, out var list)) map[key] = list = [];
        list.Add((result, name, at));
    }

    // W/L/D por lado; '?' = partida antiga homônima com vencedor (não atribui a ninguém).
    private static (char X, char O) Results(LeaderboardGame g)
    {
        if (g.WinnerSide is { } side)
        {
            return side == "X" ? ('W', 'L') : ('L', 'W');
        }

        if (g.WinnerName is null) return ('D', 'D');
        if (g.PlayerXName == g.PlayerOName) return ('?', '?');
        return g.WinnerName == g.PlayerXName ? ('W', 'L') : g.WinnerName == g.PlayerOName ? ('L', 'W') : ('?', '?');
    }
}

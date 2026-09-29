using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TicTacToe.Modules.Gameplay;

public class GameResultService(GameplayDbContext db, ILogger<GameResultService> logger)
{
    public async Task SaveResultAsync(GameSession game)
    {
        try
        {
            var result = new MatchResult
            {
                PlayerXName = game.GetPlayerName(Player.X),
                PlayerOName = game.GetPlayerName(Player.O),
                WinnerName = game.Winner != Player.None
                    ? game.GetPlayerName(game.Winner)
                    : null
            };

            db.MatchResults.Add(result);
            await db.SaveChangesAsync();
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Falha ao salvar resultado da partida. A partida continuou normalmente.");
        }
    }

    public async Task<List<MatchResult>> GetRecentAsync(int count = 10) =>
        await db.MatchResults
            .OrderByDescending(m => m.PlayedAt)
            .Take(count)
            .ToListAsync();
}

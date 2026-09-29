using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Game;
using Xunit;

namespace TicTacToe.Tests;

public class GameBoardTimerTests
{
    [Fact(DisplayName = "SPEC-0027:IT-01 — GameBoard exibe componente de timer e segundos restantes")]
    [Trait("Category", "SPEC-0027:IT-01")]
    public async Task GameBoard_ShouldRenderTimerChipAndProgress()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();

        using var session = new GameSession();
        var cut = ctx.Render<GameBoard>(parameters => parameters
            .Add(p => p.Game, session)
            .Add(p => p.MyPlayer, Player.X));

        var markup = cut.Markup;
        Assert.Contains("15s", markup);
        Assert.Contains("mud-progress-linear", markup);
    }

    [Fact(DisplayName = "SPEC-0027:E2E-01 — GameBoard exibe mensagem de W.O. quando partida encerra por timeout")]
    [Trait("Category", "SPEC-0027:E2E-01")]
    public async Task GameBoard_ShouldRenderWOMessage_WhenTimedOut()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();

        using var session = new GameSession();
        session.SetPlayerName(Player.X, "Thomas");
        session.SetPlayerName(Player.O, "Ana");

        for (int i = 0; i < 15; i++)
        {
            session.Tick();
        }

        var cut = ctx.Render<GameBoard>(parameters => parameters
            .Add(p => p.Game, session)
            .Add(p => p.MyPlayer, Player.X));

        var markup = cut.Markup;
        Assert.Contains("W.O.", markup);
    }
}

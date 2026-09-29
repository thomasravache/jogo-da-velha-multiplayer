using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Modules.Matchmaking;
using TicTacToe.Web.Components.Game;
using TicTacToe.Web.Components.Ui;
using TicTacToe.Web.Services.PlayerIdentity;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0037: identidade anônima do jogador — serviço, pareamento e sessão

internal sealed class InMemoryPlayerStorage : IPlayerStorage
{
    public ConcurrentDictionary<string, string> Data { get; } = new();

    public bool ThrowOnAccess { get; set; }

    public ValueTask<string?> GetAsync(string key)
    {
        if (ThrowOnAccess) throw new InvalidOperationException("storage indisponível");
        return ValueTask.FromResult(Data.TryGetValue(key, out var v) ? v : null);
    }

    public ValueTask SetAsync(string key, string value)
    {
        if (ThrowOnAccess) throw new InvalidOperationException("storage indisponível");
        Data[key] = value;
        return ValueTask.CompletedTask;
    }
}

public class PlayerIdentityTests
{
    private static string Json(Guid id, string? nick) => JsonSerializer.Serialize(new { id, nick });

    [Fact(DisplayName = "SPEC-0037:CH-01 — Pareamento sem playerId mantém nomes e ordem como hoje")]
    [Trait("Category", "SPEC-0037:CH-01")]
    public void Matchmaking_WithoutPlayerIds_ShouldKeepCurrentBehavior()
    {
        var service = new MatchmakingService();
        Assert.Null(service.JoinQueue("a", "Ana"));
        var match = service.JoinQueue("b", "Bia");

        Assert.NotNull(match);
        Assert.Equal(("Ana", "Bia"), service.GetMatchPlayerNames(match.Value));

        var code = service.CreatePrivateRoom("h", "Host");
        var room = service.JoinPrivateRoom(code, "g", "Guest");
        Assert.Equal(("Host", "Guest"), service.GetMatchPlayerNames(room!.Value));
    }

    [Fact(DisplayName = "SPEC-0037:UT-01 — Primeira visita cria o GUID e as seguintes devolvem o mesmo com o apelido")]
    [Trait("Category", "SPEC-0037:UT-01")]
    public async Task LoadAsync_ShouldCreateThenReuseIdentity()
    {
        var storage = new InMemoryPlayerStorage();
        var first = await new PlayerIdentityService(storage).LoadAsync();

        Assert.NotEqual(Guid.Empty, first.PlayerId);
        Assert.Null(first.Nickname);
        Assert.True(storage.Data.ContainsKey("xo.player"));

        await new PlayerIdentityService(storage).SaveNicknameAsync("Thomas");
        var second = await new PlayerIdentityService(storage).LoadAsync();

        Assert.Equal(first.PlayerId, second.PlayerId);
        Assert.Equal("Thomas", second.Nickname);
    }

    [Fact(DisplayName = "SPEC-0037:UT-02 — Valor inválido gera novo PlayerId sem lançar exceção")]
    [Trait("Category", "SPEC-0037:UT-02")]
    public async Task LoadAsync_ShouldReplaceInvalidStoredValues()
    {
        foreach (var raw in new[] { "{", "", "{\"id\":\"nao-e-guid\"}", "{\"nick\":\"x\"}", "null", "[1,2]" })
        {
            var storage = new InMemoryPlayerStorage();
            storage.Data["xo.player"] = raw;

            var profile = await new PlayerIdentityService(storage).LoadAsync();

            Assert.NotEqual(Guid.Empty, profile.PlayerId);
            Assert.True(Guid.TryParse(JsonDocument.Parse(storage.Data["xo.player"]).RootElement.GetProperty("id").GetString(), out _));
        }

        var broken = new InMemoryPlayerStorage { ThrowOnAccess = true };
        var session = await new PlayerIdentityService(broken).LoadAsync();
        Assert.NotEqual(Guid.Empty, session.PlayerId);
    }

    [Fact(DisplayName = "SPEC-0037:UT-03 — Apelido é aparado, limitado a 20 e vazio é ignorado")]
    [Trait("Category", "SPEC-0037:UT-03")]
    public async Task SaveNicknameAsync_ShouldTrimLimitAndIgnoreEmpty()
    {
        var storage = new InMemoryPlayerStorage();
        var service = new PlayerIdentityService(storage);

        await service.SaveNicknameAsync("  Thomas  ");
        Assert.Equal("Thomas", (await service.LoadAsync()).Nickname);

        await service.SaveNicknameAsync("   ");
        Assert.Equal("Thomas", (await service.LoadAsync()).Nickname);

        await service.SaveNicknameAsync(new string('a', 30));
        Assert.Equal(20, (await service.LoadAsync()).Nickname!.Length);
    }

    [Fact(DisplayName = "SPEC-0037:UT-04 — GetMatchPlayerIds devolve o par X/O na fila e na sala privada")]
    [Trait("Category", "SPEC-0037:UT-04")]
    public void GetMatchPlayerIds_ShouldReturnPairInOrder()
    {
        var service = new MatchmakingService();
        var (a, b, host, guest) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        service.JoinQueue("a", "Ana", a);
        var queued = service.JoinQueue("b", "Bia", b)!.Value;
        Assert.Equal((a, b), service.GetMatchPlayerIds(queued));

        var code = service.CreatePrivateRoom("h", "Host", host);
        var room = service.JoinPrivateRoom(code, "g", "Guest", guest)!.Value;
        Assert.Equal((host, guest), service.GetMatchPlayerIds(room));

        service.JoinQueue("c", "Caio");
        var noIds = service.JoinQueue("d", "Duda")!.Value;
        Assert.Equal(((Guid?)null, (Guid?)null), service.GetMatchPlayerIds(noIds));
    }

    [Fact(DisplayName = "SPEC-0037:UT-05 — GameSession guarda o PlayerId por lado")]
    [Trait("Category", "SPEC-0037:UT-05")]
    public void GameSession_ShouldStorePlayerIds()
    {
        using var game = new GameSession(enableBackgroundTimer: false);
        var id = Guid.NewGuid();

        Assert.Null(game.GetPlayerId(Player.X));
        game.SetPlayerId(Player.X, id);

        Assert.Equal(id, game.GetPlayerId(Player.X));
        Assert.Null(game.GetPlayerId(Player.O));
    }

    [Fact(DisplayName = "SPEC-0037:UT-06 — O PlayerId nunca aparece no markup")]
    [Trait("Category", "SPEC-0037:UT-06")]
    public async Task PlayerId_ShouldNeverAppearInMarkup()
    {
        var id = Guid.NewGuid();
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        using var game = new GameSession(enableBackgroundTimer: false);
        game.SetPlayerName(Player.X, "Ana");
        game.SetPlayerName(Player.O, "Bia");
        game.SetPlayerId(Player.X, id);

        var markups = new[]
        {
            ctx.Render<Lobby>(p => p.Add(l => l.ReturningPlayerName, "Ana").Add(l => l.PlayerName, "Ana")).Markup,
            ctx.Render<Scoreboard>(p => p.Add(s => s.Game, game).Add(s => s.MyPlayer, Player.X)).Markup,
            ctx.Render<GameBoard>(p => p.Add(b => b.Game, game).Add(b => b.MyPlayer, Player.X)).Markup,
        };

        Assert.All(markups, m => Assert.DoesNotContain(id.ToString(), m, StringComparison.OrdinalIgnoreCase));
    }
}

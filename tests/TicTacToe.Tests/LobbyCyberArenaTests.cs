using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TicTacToe.Modules.Gameplay;
using TicTacToe.Web.Components.Game;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0030: Lobby Cyber Arena

public class LobbyCyberArenaTests
{
    private static readonly string[] ExpectedParameters = new[]
        {
            "CreatedRoomCode:String", "DifficultyChanged:EventCallback`1", "InputRoomCode:String",
            "InputRoomCodeChanged:EventCallback`1", "IsWaiting:Boolean", "OnCreateRoom:EventCallback",
            "OnJoinRoom:EventCallback", "OnPlayOnline:EventCallback", "OnPlaySolo:EventCallback",
            "PlayerName:String", "PlayerNameChanged:EventCallback`1", "RoomErrorMessage:String",
            "SelectedDifficulty:AiDifficulty",
        }.OrderBy(x => x, StringComparer.Ordinal).ToArray();

    [Fact(DisplayName = "SPEC-0030:CH-01 — API pública do Lobby (parâmetros) permanece a mesma")]
    [Trait("Category", "SPEC-0030:CH-01")]
    public void Lobby_PublicParameters_ShouldRemainUnchanged()
    {
        var actual = typeof(Lobby)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
            .Select(p => $"{p.Name}:{p.PropertyType.Name}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();


        Assert.Equal(ExpectedParameters, actual);
    }

    private static IRenderedComponent<Lobby> RenderLobby(BunitContext ctx, Action<ComponentParameterCollectionBuilder<Lobby>>? configure = null) =>
        ctx.Render<Lobby>(p =>
        {
            p.Add(l => l.SelectedDifficulty, AiDifficulty.Hard);
            configure?.Invoke(p);
        });

    private static IElement Button(IRenderedComponent<Lobby> cut, string text) =>
        cut.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains(text));

    private static IElement Radio(IRenderedComponent<Lobby> cut, string text) =>
        cut.FindAll("[role='radio']").First(r => r.TextContent.Contains(text));

    [Fact(DisplayName = "SPEC-0030:UT-01 — Apelido: contador, limite de 20 e chip de prontidão")]
    [Trait("Category", "SPEC-0030:UT-01")]
    public async Task Nickname_ShouldShowCounterLimitAndReadyChip()
    {
        await using var ctx = new BunitContext();
        string? received = null;
        var cut = RenderLobby(ctx, p => p
            .Add(l => l.PlayerName, "")
            .Add(l => l.PlayerNameChanged, EventCallback.Factory.Create<string>(this, v => received = v)));

        Assert.Contains("Informe seu apelido", cut.Markup);
        Assert.Equal("20", cut.Find("input#playerName").GetAttribute("maxlength"));

        cut.Find("input#playerName").Input("Ana");
        Assert.Equal("Ana", received);

        cut.Render(p => p.Add(l => l.PlayerName, "Thomas"));
        Assert.Contains("Pronto para jogar", cut.Markup);
        Assert.Contains("6/20", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0030:UT-02 — Jogar online: desabilitado sem nome, dispara callback e mostra o tempo por turno")]
    [Trait("Category", "SPEC-0030:UT-02")]
    public async Task PlayOnline_ShouldRequireNameAndShowTurnTime()
    {
        await using var ctx = new BunitContext();
        var clicks = 0;
        var cut = RenderLobby(ctx, p => p
            .Add(l => l.PlayerName, "")
            .Add(l => l.OnPlayOnline, EventCallback.Factory.Create(this, () => clicks++)));

        Assert.True(Button(cut, "Procurar oponente").HasAttribute("disabled"));

        cut.Render(p => p.Add(l => l.PlayerName, "Thomas"));
        Button(cut, "Procurar oponente").Click();

        Assert.Equal(1, clicks);
        Assert.Contains($"{GameSession.DefaultTurnTimeSeconds} segundos", cut.Markup);
    }

    [Fact(DisplayName = "SPEC-0030:UT-03 — Jogar solo: dificuldade, descrição por nível e callback")]
    [Trait("Category", "SPEC-0030:UT-03")]
    public async Task PlaySolo_ShouldHandleDifficultyAndCallback()
    {
        await using var ctx = new BunitContext();
        AiDifficulty? changed = null;
        var solo = 0;
        var cut = RenderLobby(ctx, p => p
            .Add(l => l.PlayerName, "")
            .Add(l => l.DifficultyChanged, EventCallback.Factory.Create<AiDifficulty>(this, d => changed = d))
            .Add(l => l.OnPlaySolo, EventCallback.Factory.Create(this, () => solo++)));

        Assert.Equal("true", Radio(cut, "Impossível 🔴").GetAttribute("aria-checked"));
        Assert.Contains("Minimax", cut.Markup);
        Assert.True(Button(cut, "Iniciar partida solo").HasAttribute("disabled"));

        Radio(cut, "Fácil 🟢").Click();
        Assert.Equal(AiDifficulty.Easy, changed);
        Assert.Contains("aleatórias", cut.Markup);

        cut.Render(p => p.Add(l => l.PlayerName, "Thomas"));
        Button(cut, "Iniciar partida solo").Click();
        Assert.Equal(1, solo);
    }

    [Fact(DisplayName = "SPEC-0030:UT-04 — Sala privada: criar, entrar com código e alerta de erro")]
    [Trait("Category", "SPEC-0030:UT-04")]
    public async Task PrivateRoom_ShouldCreateJoinAndShowError()
    {
        await using var ctx = new BunitContext();
        var created = 0;
        var joined = 0;
        var cut = RenderLobby(ctx, p => p
            .Add(l => l.PlayerName, "")
            .Add(l => l.InputRoomCode, "")
            .Add(l => l.OnCreateRoom, EventCallback.Factory.Create(this, () => created++))
            .Add(l => l.OnJoinRoom, EventCallback.Factory.Create(this, () => joined++)));

        Assert.True(Button(cut, "Criar sala").HasAttribute("disabled"));
        cut.Render(p => p.Add(l => l.PlayerName, "Thomas"));
        Button(cut, "Criar sala").Click();
        Assert.Equal(1, created);

        Radio(cut, "Entrar com código").Click();
        Assert.True(Button(cut, "Entrar 🔑").HasAttribute("disabled"));

        cut.Render(p => p.Add(l => l.InputRoomCode, "SALA-ABCD"));
        Button(cut, "Entrar 🔑").Click();
        Assert.Equal(1, joined);

        cut.Render(p => p.Add(l => l.RoomErrorMessage, "Sala inválida ou já iniciada!"));
        Assert.Contains("Sala inválida ou já iniciada!", cut.Find("[role='alert']").TextContent);
    }

    [Fact(DisplayName = "SPEC-0030:UT-05 — Estados de espera e de sala criada ocultam os cartões de modo")]
    [Trait("Category", "SPEC-0030:UT-05")]
    public async Task WaitingAndRoomReady_ShouldReplaceModeCards()
    {
        await using var ctx = new BunitContext();

        var waiting = RenderLobby(ctx, p => p.Add(l => l.PlayerName, "Thomas").Add(l => l.IsWaiting, true));
        Assert.Contains("Procurando oponente para Thomas", waiting.Markup);
        Assert.DoesNotContain("Procurar oponente", waiting.Markup);

        var ready = RenderLobby(ctx, p => p.Add(l => l.PlayerName, "Thomas").Add(l => l.CreatedRoomCode, "SALA-ABCD"));
        Assert.Contains("SALA-ABCD", ready.Markup);
        Assert.Contains("Copiar", ready.Markup);
        Assert.DoesNotContain("Procurar oponente", ready.Markup);
    }

    [Fact(DisplayName = "SPEC-0030:UT-06 — Sem resquício de MudBlazor e com primitivos Cyber Arena")]
    [Trait("Category", "SPEC-0030:UT-06")]
    public async Task Lobby_ShouldNotUseMudBlazor_AndUseCyberArenaPrimitives()
    {
        await using var ctx = new BunitContext();

        var idle = RenderLobby(ctx, p => p.Add(l => l.PlayerName, "Thomas"));
        var waiting = RenderLobby(ctx, p => p.Add(l => l.PlayerName, "Thomas").Add(l => l.IsWaiting, true));
        var ready = RenderLobby(ctx, p => p.Add(l => l.PlayerName, "Thomas").Add(l => l.CreatedRoomCode, "SALA-ABCD"));

        foreach (var cut in new[] { idle, waiting, ready })
            Assert.DoesNotContain("mud-", cut.Markup);
        Assert.Contains("backdrop-blur", idle.Markup);
        Assert.Contains("rounded-pill", idle.Markup);
    }

    [Fact(DisplayName = "SPEC-0030:IT-01 — Copiar código usa a área de transferência e trata falhas")]
    [Trait("Category", "SPEC-0030:IT-01")]
    public async Task CopyCode_ShouldUseClipboard_AndHandleFailure()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.SetupVoid("navigator.clipboard.writeText", "SALA-ABCD").SetVoidResult();
        var cut = RenderLobby(ctx, p => p.Add(l => l.PlayerName, "Thomas").Add(l => l.CreatedRoomCode, "SALA-ABCD"));

        await cut.InvokeAsync(() => Button(cut, "Copiar").Click());
        cut.WaitForAssertion(() => Assert.Contains("Código copiado!", cut.Markup));
        ctx.JSInterop.VerifyInvoke("navigator.clipboard.writeText");

        await using var failing = new BunitContext();
        failing.JSInterop.SetupVoid("navigator.clipboard.writeText", "SALA-ABCD").SetException(new JSException("negado"));
        var cut2 = RenderLobby(failing, p => p.Add(l => l.PlayerName, "Thomas").Add(l => l.CreatedRoomCode, "SALA-ABCD"));

        await cut2.InvokeAsync(() => Button(cut2, "Copiar").Click());
        cut2.WaitForAssertion(() => Assert.Contains("Não foi possível copiar", cut2.Markup));
        Assert.Contains("SALA-ABCD", cut2.Markup);
    }

    [Fact(DisplayName = "SPEC-0030:E2E-01 — Jornada do lobby: apelido, dificuldade, solo, entrar com código e copiar")]
    [Trait("Category", "SPEC-0030:E2E-01")]
    public async Task LobbyJourney_ShouldWorkEndToEnd()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.SetupVoid("navigator.clipboard.writeText", "SALA-ABCD").SetVoidResult();
        var solo = 0;
        var joined = 0;
        var difficulty = AiDifficulty.Hard;
        var name = "";
        var code = "";
        var cut = RenderLobby(ctx, p => p
            .Add(l => l.PlayerName, name)
            .Add(l => l.InputRoomCode, code)
            .Add(l => l.PlayerNameChanged, EventCallback.Factory.Create<string>(this, v => name = v))
            .Add(l => l.InputRoomCodeChanged, EventCallback.Factory.Create<string>(this, v => code = v))
            .Add(l => l.DifficultyChanged, EventCallback.Factory.Create<AiDifficulty>(this, d => difficulty = d))
            .Add(l => l.OnPlaySolo, EventCallback.Factory.Create(this, () => solo++))
            .Add(l => l.OnJoinRoom, EventCallback.Factory.Create(this, () => joined++)));

        cut.Find("input#playerName").Input("Thomas");
        Radio(cut, "Fácil 🟢").Click();
        cut.Render(p => p.Add(l => l.PlayerName, name));
        Button(cut, "Iniciar partida solo").Click();

        Radio(cut, "Entrar com código").Click();
        cut.Find("input#roomCode").Input("SALA-ABCD");
        cut.Render(p => p.Add(l => l.InputRoomCode, code));
        Button(cut, "Entrar 🔑").Click();

        Assert.Equal("Thomas", name);
        Assert.Equal(AiDifficulty.Easy, difficulty);
        Assert.Equal(1, solo);
        Assert.Equal(1, joined);

        cut.Render(p => p.Add(l => l.CreatedRoomCode, "SALA-ABCD"));
        await cut.InvokeAsync(() => Button(cut, "Copiar").Click());
        cut.WaitForAssertion(() => Assert.Contains("Código copiado!", cut.Markup));
    }

    [Fact(DisplayName = "SPEC-0030:IT-01b — Aviso de cópia é zerado quando o código da sala muda")]
    [Trait("Category", "SPEC-0030:IT-01")]
    public async Task CopyNotice_ShouldReset_WhenRoomCodeChanges()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.SetupVoid("navigator.clipboard.writeText", "SALA-ABCD").SetVoidResult();
        var cut = RenderLobby(ctx, p => p.Add(l => l.PlayerName, "Thomas").Add(l => l.CreatedRoomCode, "SALA-ABCD"));

        await cut.InvokeAsync(() => Button(cut, "Copiar").Click());
        cut.WaitForAssertion(() => Assert.Contains("Código copiado!", cut.Markup));

        cut.Render(p => p.Add(l => l.CreatedRoomCode, "SALA-WXYZ"));
        Assert.DoesNotContain("Código copiado!", cut.Markup);
    }
}

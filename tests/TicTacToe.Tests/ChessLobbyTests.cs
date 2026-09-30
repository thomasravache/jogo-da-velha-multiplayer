using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TicTacToe.Modules.Chess;
using TicTacToe.Web.Components.Chess;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0056: lobby de xadrez (apresentação)

public class ChessLobbyTests
{
    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private static IRenderedComponent<ChessLobby> Render(BunitContext ctx, Action<ComponentParameterCollectionBuilder<ChessLobby>>? configure = null) =>
        ctx.Render<ChessLobby>(p => configure?.Invoke(p));

    private static IElement Button(IRenderedComponent<ChessLobby> cut, string text) =>
        cut.FindAll("button").First(b => !b.HasAttribute("role") && b.TextContent.Contains(text, StringComparison.Ordinal));

    private static IElement Radio(IRenderedComponent<ChessLobby> cut, string group, string text) =>
        cut.Find($"[role='radiogroup'][aria-label='{group}']").QuerySelectorAll("[role='radio']")
            .First(r => r.TextContent.Contains(text, StringComparison.Ordinal));

    [Fact(DisplayName = "SPEC-0056:UT-01 — Apelido: campo preenchido, contador de 20 e chip de prontidão")]
    [Trait("Category", "SPEC-0056:UT-01")]
    public void Nickname_ShouldShowCounterLimitAndReadyChip()
    {
        using var ctx = NewContext();
        string? received = null;
        var cut = Render(ctx, p => p
            .Add(l => l.PlayerName, "")
            .Add(l => l.PlayerNameChanged, EventCallback.Factory.Create<string>(this, v => received = v)));

        Assert.Contains("Informe seu apelido", cut.Markup, StringComparison.Ordinal);
        Assert.Equal("20", cut.Find("input#playerName").GetAttribute("maxlength"));
        Assert.Contains("0/20", cut.Markup, StringComparison.Ordinal);

        cut.Find("input#playerName").Input("Ana");
        Assert.Equal("Ana", received);

        cut.Render(p => p.Add(l => l.PlayerName, "Thomas").Add(l => l.ReturningPlayerName, "Thomas"));
        Assert.Equal("Thomas", cut.Find("input#playerName").GetAttribute("value"));
        Assert.Contains("Pronto para jogar", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("6/20", cut.Markup, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0056:UT-02 — Controle de tempo: marca o atual, dispara ControlChanged e descreve o escolhido")]
    [Trait("Category", "SPEC-0056:UT-02")]
    public void TimeControl_ShouldMarkCurrentNotifyAndDescribe()
    {
        using var ctx = NewContext();
        TimeControl? received = null;
        var cut = Render(ctx, p => p
            .Add(l => l.SelectedControl, TimeControl.Blitz)
            .Add(l => l.ControlChanged, EventCallback.Factory.Create<TimeControl>(this, v => received = v)));

        Assert.Equal(3, cut.Find("[role='radiogroup'][aria-label='Controle de tempo']").QuerySelectorAll("[role='radio']").Length);
        Assert.Equal("true", Radio(cut, "Controle de tempo", "Blitz").GetAttribute("aria-checked"));
        Assert.Contains("5 minutos para cada jogador, sem acréscimo de tempo por lance", cut.Markup, StringComparison.Ordinal);

        Radio(cut, "Controle de tempo", "Rápida").Click();
        Assert.Equal(TimeControl.Rapid, received);

        cut.Render(p => p.Add(l => l.SelectedControl, TimeControl.Rapid));
        Assert.Equal("true", Radio(cut, "Controle de tempo", "Rápida").GetAttribute("aria-checked"));
        Assert.Contains("10 minutos para cada jogador, com 5 segundos por lance", cut.Markup, StringComparison.Ordinal);

        cut.Render(p => p.Add(l => l.SelectedControl, TimeControl.Bullet));
        Assert.Contains("1 minuto para cada jogador, sem acréscimo", cut.Markup, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0056:UT-03 — Cor: marca a atual, dispara ColorChanged e descreve Aleatória")]
    [Trait("Category", "SPEC-0056:UT-03")]
    public void Color_ShouldMarkCurrentNotifyAndDescribe()
    {
        using var ctx = NewContext();
        ColorPreference? received = null;
        var cut = Render(ctx, p => p
            .Add(l => l.SelectedColor, ColorPreference.White)
            .Add(l => l.ColorChanged, EventCallback.Factory.Create<ColorPreference>(this, v => received = v)));

        Assert.Equal(3, cut.Find("[role='radiogroup'][aria-label='Sua cor']").QuerySelectorAll("[role='radio']").Length);
        Assert.Equal("true", Radio(cut, "Sua cor", "Brancas").GetAttribute("aria-checked"));
        Assert.Contains("Sua preferência é atendida quando possível; se os dois pedirem a mesma cor, as cores são sorteadas", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("O sorteio de cores ocorrerá automaticamente no início da partida", cut.Markup, StringComparison.Ordinal);

        Radio(cut, "Sua cor", "Pretas").Click();
        Assert.Equal(ColorPreference.Black, received);

        cut.Render(p => p.Add(l => l.SelectedColor, ColorPreference.Random));
        Assert.Equal("true", Radio(cut, "Sua cor", "Aleatória").GetAttribute("aria-checked"));
        Assert.Contains("O sorteio de cores ocorrerá automaticamente no início da partida", cut.Markup, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0056:UT-04 — Procurar oponente: desabilitado sem apelido, dispara com apelido, mostra 'Na fila' e cancela")]
    [Trait("Category", "SPEC-0056:UT-04")]
    public void PlayOnline_ShouldRequireNameAndShowQueueWithCancel()
    {
        using var ctx = NewContext();
        var played = 0;
        var cancelled = 0;
        var cut = Render(ctx, p => p
            .Add(l => l.PlayerName, "")
            .Add(l => l.OnPlayOnline, EventCallback.Factory.Create(this, () => played++))
            .Add(l => l.OnCancelSearch, EventCallback.Factory.Create(this, () => cancelled++)));

        Assert.True(Button(cut, "Procurar oponente").HasAttribute("disabled"));

        cut.Render(p => p.Add(l => l.PlayerName, "Ana"));
        var play = Button(cut, "Procurar oponente");
        Assert.False(play.HasAttribute("disabled"));
        play.Click();
        Assert.Equal(1, played);

        cut.Render(p => p.Add(l => l.IsWaiting, true));
        Assert.Contains("Na fila", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Procurando oponente", cut.Find("[role='status']").TextContent, StringComparison.Ordinal);
        Button(cut, "Cancelar busca").Click();
        Assert.Equal(1, cancelled);

        cut.Render(p => p.Add(l => l.IsWaiting, false));
        Assert.DoesNotContain("Na fila", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Procurar oponente", cut.Markup, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0056:UT-05 — Sala privada: código com Copiar e 'Código copiado!', entrada e erro em role=alert")]
    [Trait("Category", "SPEC-0056:UT-05")]
    public void PrivateRoom_ShouldShowCodeCopyAndAnnounceError()
    {
        using var ctx = NewContext();
        var created = 0;
        var joined = 0;
        string? typed = null;
        var cut = Render(ctx, p => p
            .Add(l => l.PlayerName, "Ana")
            .Add(l => l.OnCreateRoom, EventCallback.Factory.Create(this, () => created++))
            .Add(l => l.OnJoinRoom, EventCallback.Factory.Create(this, () => joined++))
            .Add(l => l.InputRoomCodeChanged, EventCallback.Factory.Create<string>(this, v => typed = v)));

        Button(cut, "Criar sala").Click();
        Assert.Equal(1, created);

        cut.Render(p => p.Add(l => l.CreatedRoomCode, "SALA-X7K2"));
        Assert.Contains("SALA-X7K2", cut.Find("[aria-label='Código da sala']").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Código copiado!", cut.Markup, StringComparison.Ordinal);
        Button(cut, "Copiar").Click();
        Assert.Contains("Código copiado!", cut.Find("[data-copy-status]").TextContent, StringComparison.Ordinal);
        Assert.Contains(ctx.JSInterop.Invocations, i => i.Identifier == "navigator.clipboard.writeText");

        cut.Render(p => p.Add(l => l.CreatedRoomCode, (string?)null));
        Radio(cut, "Ação da sala privada", "Entrar com código").Click();
        cut.Find("input#roomCode").Input("sala-zzzz");
        Assert.Equal("sala-zzzz", typed);

        cut.Render(p => p.Add(l => l.InputRoomCode, "SALA-ZZZZ").Add(l => l.RoomErrorMessage, "Sala inválida ou já iniciada!"));
        Button(cut, "Entrar").Click();
        Assert.Equal(1, joined);
        var alert = cut.Find("[role='alert']");
        Assert.Contains("Sala inválida ou já iniciada!", alert.TextContent, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0056:UT-05 — Falha ao copiar orienta a copiar manualmente")]
    [Trait("Category", "SPEC-0056:UT-05")]
    public void CopyFailure_ShouldSuggestManualCopy()
    {
        using var ctx = NewContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        ctx.JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true).SetException(new JSException("negado"));
        var cut = Render(ctx, p => p.Add(l => l.PlayerName, "Ana").Add(l => l.CreatedRoomCode, "SALA-X7K2"));

        Button(cut, "Copiar").Click();

        Assert.Contains("Não foi possível copiar", cut.Find("[data-copy-status]").TextContent, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0056:UT-07 — Acessibilidade: rótulos, radiogrupos, sem <style> inline nem mud-, regiões vivas")]
    [Trait("Category", "SPEC-0056:UT-07")]
    public void Lobby_ShouldBeAccessible()
    {
        using var ctx = NewContext();
        var cut = Render(ctx, p => p.Add(l => l.PlayerName, "Ana").Add(l => l.RoomErrorMessage, "Sala inválida ou já iniciada!"));

        Assert.Equal(4, cut.FindAll("[role='radiogroup']").Count); // controle, cor, dificuldade do robô, ação da sala
        Assert.All(cut.FindAll("[role='radiogroup']"), g => Assert.False(string.IsNullOrWhiteSpace(g.GetAttribute("aria-label"))));
        Assert.NotNull(cut.Find("label[for='playerName']"));
        Assert.Empty(cut.FindAll("style"));
        Assert.DoesNotContain("mud-", cut.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("[aria-live]"));
        Assert.Equal("alert", cut.Find("[role='alert']").GetAttribute("role"));
    }

    [Fact(DisplayName = "SPEC-0056:UT-04 — Na fila, criar/entrar em sala fica desabilitado; com sala criada, procurar oponente fica desabilitado")]
    [Trait("Category", "SPEC-0056:UT-04")]
    public void QueueAndRoom_ShouldBeMutuallyExclusive()
    {
        using var ctx = NewContext();
        var cut = Render(ctx, p => p.Add(l => l.PlayerName, "Ana").Add(l => l.IsWaiting, true).Add(l => l.InputRoomCode, "SALA-ABCD"));

        Assert.True(Button(cut, "Criar sala").HasAttribute("disabled"));
        Radio(cut, "Ação da sala privada", "Entrar com código").Click();
        Assert.True(Button(cut, "Entrar").HasAttribute("disabled"));

        cut.Render(p => p.Add(l => l.IsWaiting, false).Add(l => l.CreatedRoomCode, "SALA-X7K2"));
        Assert.True(Button(cut, "Procurar oponente").HasAttribute("disabled"));
    }

    [Fact(DisplayName = "SPEC-0056:UT-05 — Entrar com código informa que o controle de tempo é o do anfitrião")]
    [Trait("Category", "SPEC-0056:UT-05")]
    public void JoinTab_ShouldHintHostControl()
    {
        using var ctx = NewContext();
        var cut = Render(ctx, p => p.Add(l => l.PlayerName, "Ana"));

        Radio(cut, "Ação da sala privada", "Entrar com código").Click();

        Assert.Contains("controle de tempo da sala é o do anfitrião", cut.Markup, StringComparison.Ordinal);
    }
}

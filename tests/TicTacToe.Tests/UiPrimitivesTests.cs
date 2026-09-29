using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using TicTacToe.Web.Components.Ui;

namespace TicTacToe.Tests;

// SPEC-0043: shell e primitivos de UI Cyber Arena — primitivos

public class UiPrimitivesTests
{
    private static RenderFragment Text(string text) => b => b.AddContent(0, text);

    [Fact(DisplayName = "SPEC-0043:UT-05 — NeonCard aplica acento distinto e renderiza o conteúdo")]
    [Trait("Category", "SPEC-0043:UT-05")]
    public void NeonCard_ShouldApplyDistinctAccentClasses()
    {
        using var ctx = new BunitContext();
        var classes = new Dictionary<CardAccent, string>();
        foreach (var accent in Enum.GetValues<CardAccent>())
        {
            var cut = ctx.Render<NeonCard>(p => p.Add(c => c.Accent, accent).Add(c => c.ChildContent, Text("conteudo")));
            Assert.Contains("conteudo", cut.Markup);
            classes[accent] = cut.Find("div").GetAttribute("class") ?? "";
        }

        Assert.Equal(classes.Count, classes.Values.Distinct().Count());
        Assert.All(classes.Values, c => Assert.Contains("backdrop-blur", c));
    }

    [Fact(DisplayName = "SPEC-0043:UT-06 — PillButton: variantes, desabilitado, carregando e link")]
    [Trait("Category", "SPEC-0043:UT-06")]
    public void PillButton_ShouldHandleVariantsDisabledLoadingAndHref()
    {
        using var ctx = new BunitContext();

        var variants = Enum.GetValues<PillVariant>().Select(v =>
            ctx.Render<PillButton>(p => p.Add(c => c.Variant, v).Add(c => c.ChildContent, Text("ok")))
               .Find("button").GetAttribute("class")).ToList();
        Assert.Equal(variants.Count, variants.Distinct().Count());

        var clicks = 0;
        var enabled = ctx.Render<PillButton>(p => p
            .Add(c => c.OnClick, EventCallback.Factory.Create(this, () => clicks++))
            .Add(c => c.ChildContent, Text("ok")));
        enabled.Find("button").Click();
        Assert.Equal(1, clicks);

        var disabled = ctx.Render<PillButton>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, EventCallback.Factory.Create(this, () => clicks++))
            .Add(c => c.ChildContent, Text("ok")));
        Assert.True(disabled.Find("button").HasAttribute("disabled"));
        Assert.Equal(1, clicks);

        var loading = ctx.Render<PillButton>(p => p.Add(c => c.Loading, true).Add(c => c.ChildContent, Text("ok")));
        Assert.True(loading.Find("button").HasAttribute("disabled"));
        Assert.NotNull(loading.Find("button").QuerySelector("[data-loading]"));

        var link = ctx.Render<PillButton>(p => p.Add(c => c.Href, "/history").Add(c => c.ChildContent, Text("ir")));
        Assert.Equal("/history", link.Find("a").GetAttribute("href"));
        Assert.Empty(link.FindAll("button"));
    }

    [Fact(DisplayName = "SPEC-0043:UT-07 — StatusChip varia por tom e só Waiting tem ponto animado")]
    [Trait("Category", "SPEC-0043:UT-07")]
    public void StatusChip_ShouldVaryByTone_AndAnimateDotOnlyOnWaiting()
    {
        using var ctx = new BunitContext();
        var byTone = Enum.GetValues<ChipTone>().ToDictionary(t => t, t =>
            ctx.Render<StatusChip>(p => p.Add(c => c.Tone, t).Add(c => c.Dot, true).Add(c => c.ChildContent, Text("x"))));

        Assert.Equal(byTone.Count, byTone.Values.Select(c => c.Find("span").GetAttribute("class")).Distinct().Count());
        foreach (var (tone, cut) in byTone)
        {
            var animated = cut.Markup.Contains("animate-pulse");
            Assert.Equal(tone == ChipTone.Waiting, animated);
        }
    }

    [Fact(DisplayName = "SPEC-0043:UT-08 — NeonInput faz bind, mostra contador e associa o rótulo")]
    [Trait("Category", "SPEC-0043:UT-08")]
    public void NeonInput_ShouldBindShowCounterAndLabel()
    {
        using var ctx = new BunitContext();
        string? received = null;
        var cut = ctx.Render<NeonInput>(p => p
            .Add(c => c.Id, "apelido")
            .Add(c => c.Label, "Seu apelido")
            .Add(c => c.MaxLength, 20)
            .Add(c => c.ShowCounter, true)
            .Add(c => c.Value, "")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string>(this, v => received = v)));

        Assert.Contains("0/20", cut.Markup);
        cut.Find("input").Input("Thomas");
        Assert.Equal("Thomas", received);
        Assert.Equal("20", cut.Find("input").GetAttribute("maxlength"));
        Assert.Equal("apelido", cut.Find("label").GetAttribute("for"));
        Assert.Equal("apelido", cut.Find("input").GetAttribute("id"));

        cut.Render(p => p.Add(c => c.Value, "Thomas"));
        Assert.Contains("6/20", cut.Markup);
    }

    private static IRenderedComponent<SegmentedControl<string>> Segmented(BunitContext ctx, string value, Action<string> onChange) =>
        ctx.Render<SegmentedControl<string>>(p => p
            .Add(c => c.AriaLabel, "Dificuldade")
            .Add(c => c.Items, new List<(string, string)> { ("easy", "Fácil"), ("hard", "Impossível") })
            .Add(c => c.Value, value)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string>(ctx, onChange)));

    [Fact(DisplayName = "SPEC-0043:UT-09 — SegmentedControl usa radiogroup, aria-checked, clique e setas")]
    [Trait("Category", "SPEC-0043:UT-09")]
    public void SegmentedControl_ShouldExposeRadioSemantics_AndHandleInput()
    {
        using var ctx = new BunitContext();
        string? changed = null;
        var cut = Segmented(ctx, "hard", v => changed = v);

        Assert.Equal("radiogroup", cut.Find("[role='radiogroup']").GetAttribute("role"));
        var radios = cut.FindAll("[role='radio']");
        Assert.Equal(2, radios.Count);
        Assert.Equal("false", radios[0].GetAttribute("aria-checked"));
        Assert.Equal("true", radios[1].GetAttribute("aria-checked"));

        radios[0].Click();
        Assert.Equal("easy", changed);

        changed = null;
        cut.FindAll("[role='radio']")[1].KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("easy", changed);
    }

    [Fact(DisplayName = "SPEC-0043:UT-10 — Icon: decorativo, com rótulo e nome inválido")]
    [Trait("Category", "SPEC-0043:UT-10")]
    public void Icon_ShouldBeDecorativeOrLabeled_AndRejectUnknownNames()
    {
        using var ctx = new BunitContext();

        var decorative = ctx.Render<Icon>(p => p.Add(c => c.Name, "play"));
        Assert.Equal("true", decorative.Find("svg").GetAttribute("aria-hidden"));

        var labeled = ctx.Render<Icon>(p => p.Add(c => c.Name, "trophy").Add(c => c.Label, "Vitória"));
        Assert.Equal("img", labeled.Find("svg").GetAttribute("role"));
        Assert.Equal("Vitória", labeled.Find("svg").GetAttribute("aria-label"));

        var names = new[] { "play", "history", "leaderboard", "globe", "robot", "lock", "key", "copy", "check", "timer",
            "hourglass", "close", "handshake", "timer-off", "trophy", "medal", "refresh", "warning", "person" };
        foreach (var name in names)
            Assert.NotNull(ctx.Render<Icon>(p => p.Add(c => c.Name, name)).Find("svg"));

        Assert.ThrowsAny<Exception>(() => ctx.Render<Icon>(p => p.Add(c => c.Name, "nao-existe")));
    }

    [Fact(DisplayName = "SPEC-0043:UT-05b — PageHeader mostra eyebrow, título, subtítulo e ações")]
    [Trait("Category", "SPEC-0043:UT-05")]
    public void PageHeader_ShouldRenderAllParts()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<PageHeader>(p => p
            .Add(c => c.Eyebrow, "Partidas")
            .Add(c => c.Title, "Histórico")
            .Add(c => c.Subtitle, "Últimas 10")
            .Add(c => c.ChildContent, Text("acao")));

        Assert.Equal("Histórico", cut.Find("h1").TextContent.Trim());
        Assert.Contains("Partidas", cut.Markup);
        Assert.Contains("Últimas 10", cut.Markup);
        Assert.Contains("acao", cut.Markup);
    }
}

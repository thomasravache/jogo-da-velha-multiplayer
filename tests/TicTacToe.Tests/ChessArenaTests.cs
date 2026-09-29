using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Modules.Chess;
using TicTacToe.Web.Components.Chess;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0057: arena de xadrez (composição, lances, relógios, avisos, fim de partida)

public class ChessArenaTests
{
    private const string PromotionFen = "k7/4P3/8/8/8/8/8/K7 w - - 0 1";
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private static IRenderedComponent<ChessArena> Render(BunitContext ctx, ChessSession session, int seat = 0) =>
        ctx.Render<ChessArena>(p => p.Add(c => c.Session, session).Add(c => c.MySeat, seat));

    private static void Refresh(IRenderedComponent<ChessArena> cut) =>
        cut.Render(p => p.Add(c => c.MySeat, cut.Instance.MySeat));

    private static void Click(IRenderedComponent<ChessArena> cut, string square) =>
        cut.Find($"button[data-square='{square}']").Click();

    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static string Notices(IRenderedComponent<ChessArena> cut) =>
        Normalize(cut.Find("[data-arena-notices]").TextContent);

    private static IElement Card(IRenderedComponent<ChessArena> cut, string color) =>
        cut.Find($"[data-player-card='{color}']");

    private static string Clock(IRenderedComponent<ChessArena> cut, string color) =>
        Card(cut, color).QuerySelector("[data-clock]")!.TextContent.Trim();

    private static string[] Order(IRenderedComponent<ChessArena> cut) =>
        [.. cut.FindAll("button[data-square]").Select(b => b.GetAttribute("data-square")!)];

    // ---------- UT-01 ----------

    [Fact(DisplayName = "SPEC-0057:UT-01 — sessão nova: tabuleiro das brancas, dois cartões com relógios iguais, lista vazia e 'Sua vez'")]
    [Trait("Category", "SPEC-0057:UT-01")]
    public void Arena_ShouldComposeNewSession()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();

        var cut = Render(ctx, session);

        var order = Order(cut);
        Assert.Equal(64, order.Length);
        Assert.Equal("a8", order[0]);
        Assert.Equal(32, cut.FindAll("button[data-square] svg[data-piece]").Count);
        Assert.Contains("Ana", Card(cut, "white").TextContent, StringComparison.Ordinal);
        Assert.Contains("Bia", Card(cut, "black").TextContent, StringComparison.Ordinal);
        Assert.Contains("Você", Card(cut, "white").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Você", Card(cut, "black").TextContent, StringComparison.Ordinal);
        Assert.Equal("05:00", Clock(cut, "white"));
        Assert.Equal("05:00", Clock(cut, "black"));
        Assert.Empty(cut.FindAll("[data-move-list] li"));
        Assert.Contains("Sua vez, Ana!", Notices(cut), StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-end-card]"));
    }

    // ---------- UT-02 ----------

    [Fact(DisplayName = "SPEC-0057:UT-02 — peça própria e destino legal jogam o lance na sessão")]
    [Trait("Category", "SPEC-0057:UT-02")]
    public void Arena_ShouldPlayLegalMoveByClick()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var cut = Render(ctx, session);

        Click(cut, "e2");
        Assert.Equal("true", cut.Find("button[data-square='e2']").GetAttribute("data-selected"));
        Assert.Equal(2, cut.FindAll("[data-target]").Count);
        Click(cut, "e4");

        var moves = session.Snapshot().Moves;
        Assert.Single(moves);
        Assert.Equal("e4", moves[0].San);
        Assert.Empty(cut.FindAll("[data-selected]"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-02 — destino ilegal limpa a seleção sem lance")]
    [Trait("Category", "SPEC-0057:UT-02")]
    public void Arena_ShouldClearSelectionOnIllegalDestination()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var cut = Render(ctx, session);

        Click(cut, "e2");
        Click(cut, "e5");

        Assert.Empty(session.Snapshot().Moves);
        Assert.Empty(cut.FindAll("[data-selected]"));
        Assert.Empty(cut.FindAll("[data-target]"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-02 — peça do oponente não é selecionada")]
    [Trait("Category", "SPEC-0057:UT-02")]
    public void Arena_ShouldIgnoreOpponentPiece()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var cut = Render(ctx, session);

        Click(cut, "e7");

        Assert.Empty(session.Snapshot().Moves);
        Assert.Empty(cut.FindAll("[data-selected]"));
        Assert.Empty(cut.FindAll("[data-target]"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-02 — fora da vez nada é selecionado nem jogado")]
    [Trait("Category", "SPEC-0057:UT-02")]
    public void Arena_ShouldIgnoreClicksOutOfTurn()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        ChessSessionTests.Line(session, "e2e4");
        var cut = Render(ctx, session);
        var fenBefore = session.Snapshot().Position.ToFen();

        Click(cut, "d2");
        Click(cut, "d4");

        Assert.Single(session.Snapshot().Moves);
        Assert.Equal(fenBefore, session.Snapshot().Position.ToFen());
        Assert.Empty(cut.FindAll("[data-selected]"));
        Assert.Equal("true", cut.Find("[role=grid]").GetAttribute("aria-disabled"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-02 — arrastar peça própria para destino legal joga o lance")]
    [Trait("Category", "SPEC-0057:UT-02")]
    public void Arena_ShouldPlayLegalMoveByDrag()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var cut = Render(ctx, session);

        cut.Find("[data-drag='g1']").TriggerEvent("ondragstart", new DragEventArgs());
        cut.Find("button[data-square='f3']").TriggerEvent("ondrop", new DragEventArgs());

        Assert.Equal("Nf3", Assert.Single(session.Snapshot().Moves).San);
    }

    // ---------- UT-03 ----------

    private static IRenderedComponent<ChessArena> ArriveAtPromotion(BunitContext ctx, out ChessSession session)
    {
        session = ChessSessionTests.New(start: Position.FromFen(PromotionFen));
        var cut = Render(ctx, session);
        Click(cut, "e7");
        Click(cut, "e8");
        return cut;
    }

    [Fact(DisplayName = "SPEC-0057:UT-03 — peão à oitava abre o seletor; escolher a dama aplica e7→e8=Q")]
    [Trait("Category", "SPEC-0057:UT-03")]
    public void Arena_ShouldPromoteWithPicker()
    {
        using var ctx = NewContext();
        var cut = ArriveAtPromotion(ctx, out var session);
        using var _ = session;

        Assert.NotEmpty(cut.FindAll("[role=dialog]"));
        Assert.Empty(session.Snapshot().Moves);

        cut.Find("[data-promotion='Q']").Click();

        var played = Assert.Single(session.Snapshot().Moves);
        Assert.Equal(new Move(Square.Parse("e7"), Square.Parse("e8"), PieceType.Queen), played.Move);
        Assert.Empty(cut.FindAll("[role=dialog]"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-03 — cancelar o seletor não aplica nenhum lance")]
    [Trait("Category", "SPEC-0057:UT-03")]
    public void Arena_ShouldNotMoveWhenPromotionCancelled()
    {
        using var ctx = NewContext();
        var cut = ArriveAtPromotion(ctx, out var session);
        using var _ = session;

        cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.Empty(session.Snapshot().Moves);
        Assert.Equal(PromotionFen, session.Snapshot().Position.ToFen());
    }

    // ---------- UT-04 ----------

    [Fact(DisplayName = "SPEC-0057:UT-04 — relógios mm:ss, alerta abaixo de 10 s e destaque de quem joga")]
    [Trait("Category", "SPEC-0057:UT-04")]
    public void Arena_ShouldShowClocksAndLowTimeAlert()
    {
        using var ctx = NewContext();
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        var cut = Render(ctx, session);

        ChessSessionTests.Line(session, "e2e4");
        time.Advance(48 * Second);
        ChessSessionTests.Line(session, "e7e5");
        time.Advance(291 * Second);
        Refresh(cut);

        Assert.Equal("04:12", Clock(cut, "black"));
        Assert.Equal("00:09", Clock(cut, "white"));
        Assert.Equal("true", Card(cut, "white").QuerySelector("[data-clock]")!.GetAttribute("data-low"));
        Assert.NotEqual("true", Card(cut, "black").QuerySelector("[data-clock]")!.GetAttribute("data-low"));
        Assert.Equal("timer", Card(cut, "black").QuerySelector("[data-clock]")!.GetAttribute("role"));
        Assert.Equal("true", Card(cut, "white").GetAttribute("data-active"));
        Assert.NotEqual("true", Card(cut, "black").GetAttribute("data-active"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-04 — relógio de cartão isolado formata mm:ss e marca tempo baixo")]
    [Trait("Category", "SPEC-0057:UT-04")]
    public void PlayerCard_ShouldFormatClock()
    {
        using var ctx = NewContext();

        var cut = ctx.Render<ChessPlayerCard>(p => p
            .Add(c => c.Name, "Ana")
            .Add(c => c.Color, PieceColor.White)
            .Add(c => c.Remaining, TimeSpan.FromSeconds(9.9))
            .Add(c => c.IsActive, true));

        var clock = cut.Find("[data-clock]");
        Assert.Equal("00:09", clock.TextContent.Trim());
        Assert.Equal("true", clock.GetAttribute("data-low"));
        Assert.Contains("Ana", clock.GetAttribute("aria-label"), StringComparison.Ordinal);

        cut.Render(p => p.Add(c => c.Remaining, TimeSpan.FromSeconds(600)));
        Assert.Equal("10:00", cut.Find("[data-clock]").TextContent.Trim());
        Assert.NotEqual("true", cut.Find("[data-clock]").GetAttribute("data-low"));
    }

    // ---------- UT-06 ----------

    [Fact(DisplayName = "SPEC-0057:UT-06 — cada cartão lista as peças que o jogador capturou")]
    [Trait("Category", "SPEC-0057:UT-06")]
    public void Arena_ShouldListCapturedPiecesPerPlayer()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        ChessSessionTests.Line(session, "e2e4", "d7d5", "e4d5", "d8d5", "b1c3", "d5g2", "f1g2");

        var cut = Render(ctx, session);

        string[] Captured(string color) =>
            [.. Card(cut, color).QuerySelectorAll("[data-captured] svg[data-piece]").Select(s => s.GetAttribute("data-piece")!)];
        Assert.Equal(["bP", "bQ"], Captured("white").Order());
        Assert.Equal(["wP", "wP"], Captured("black"));
    }

    // ---------- UT-07 ----------

    [Fact(DisplayName = "SPEC-0057:UT-07 — região aria-live única traz vez própria, vez do oponente e xeque")]
    [Trait("Category", "SPEC-0057:UT-07")]
    public void Arena_ShouldAnnounceTurnAndCheck()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var white = Render(ctx, session, 0);
        var black = Render(ctx, session, 1);

        Assert.Single(white.FindAll("[aria-live]"));
        Assert.Equal("polite", white.Find("[data-arena-notices]").GetAttribute("aria-live"));
        Assert.Contains("Sua vez, Ana!", Notices(white), StringComparison.Ordinal);
        Assert.Contains("Vez de Ana…", Notices(black), StringComparison.Ordinal);

        ChessSessionTests.Line(session, "e2e4");
        Assert.Contains("Vez de Bia…", Notices(white), StringComparison.Ordinal);
        Assert.Contains("Sua vez, Bia!", Notices(black), StringComparison.Ordinal);
        Assert.DoesNotContain("Xeque!", Notices(black), StringComparison.Ordinal);

        ChessSessionTests.Line(session, "f7f6", "d1h5");
        Assert.Contains("Xeque!", Notices(white), StringComparison.Ordinal);
        Assert.Contains("Xeque!", Notices(black), StringComparison.Ordinal);
        Assert.Contains("Sua vez, Bia!", Notices(black), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0057:UT-07 — o rei em xeque é marcado no tabuleiro")]
    [Trait("Category", "SPEC-0057:UT-07")]
    public void Arena_ShouldMarkKingInCheck()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        ChessSessionTests.Line(session, "e2e4", "f7f6", "d1h5");

        var cut = Render(ctx, session);

        Assert.Equal("e8", Assert.Single(cut.FindAll("[data-check]")).GetAttribute("data-square"));
    }

    // ---------- UT-08 ----------

    [Fact(DisplayName = "SPEC-0057:UT-08 — mate mostra o cartão final e deixa o tabuleiro sem interação")]
    [Trait("Category", "SPEC-0057:UT-08")]
    public void Arena_ShouldShowEndCardAfterCheckmate()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        ChessSessionTests.Line(session, "f2f3", "e7e5", "g2g4", "d8h4");

        var cut = Render(ctx, session);

        Assert.Equal("Xeque-mate — Bia venceu", cut.Find("[data-end-title]").TextContent.Trim());
        Assert.Contains("2 lances", cut.Find("[data-end-summary]").TextContent, StringComparison.Ordinal);
        Assert.Equal("true", cut.Find("[role=grid]").GetAttribute("aria-disabled"));
        Click(cut, "a2");
        Assert.Empty(cut.FindAll("[data-selected]"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-08 — bandeira caída mostra vitória por tempo")]
    [Trait("Category", "SPEC-0057:UT-08")]
    public void Arena_ShouldShowEndCardAfterTimeout()
    {
        using var ctx = NewContext();
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        var cut = Render(ctx, session);
        ChessSessionTests.Line(session, "e2e4");

        time.Advance(301 * Second);
        session.Tick();
        Refresh(cut);

        Assert.Equal("Vitória por tempo — Ana venceu", cut.Find("[data-end-title]").TextContent.Trim());
        Assert.Equal("true", cut.Find("[role=grid]").GetAttribute("aria-disabled"));
    }

    // ---------- UT-09 ----------

    [Fact(DisplayName = "SPEC-0057:UT-09 — após o descarte, mudar a sessão não re-renderiza nem lança")]
    [Trait("Category", "SPEC-0057:UT-09")]
    public async Task Arena_ShouldReleaseSubscriptionOnDispose()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var cut = Render(ctx, session);
        var renders = cut.RenderCount;

        await ctx.DisposeComponentsAsync();
        var error = Record.Exception(() =>
        {
            ChessSessionTests.Line(session, "e2e4");
            session.SetSeat(0, "Ana2", Guid.NewGuid(), PieceColor.White);
        });

        Assert.Null(error);
        Assert.Equal(renders, cut.RenderCount);
    }

    // ---------- UT-10 ----------

    [Fact(DisplayName = "SPEC-0057:UT-10 — troca de cores dos assentos reorienta o tabuleiro e os textos")]
    [Trait("Category", "SPEC-0057:UT-10")]
    public void Arena_ShouldFollowSeatColorAfterSwap()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var seat0 = Render(ctx, session, 0);
        var seat1 = Render(ctx, session, 1);
        Assert.Equal("a8", Order(seat0)[0]);
        Assert.Equal("h1", Order(seat1)[0]);

        session.RestartCore(swapColors: true);

        Assert.Equal("h1", Order(seat0)[0]);
        Assert.Equal("a8", Order(seat1)[0]);
        Assert.Contains("Vez de Bia…", Notices(seat0), StringComparison.Ordinal);
        Assert.Contains("Sua vez, Bia!", Notices(seat1), StringComparison.Ordinal);
        Assert.Contains("Você", Card(seat0, "black").TextContent, StringComparison.Ordinal);
        Assert.Contains("Você", Card(seat1, "white").TextContent, StringComparison.Ordinal);
    }

    // ---------- IT-01 ----------

    [Fact(DisplayName = "SPEC-0057:IT-01 — lance de uma arena aparece na outra, que passa a aceitar a resposta")]
    [Trait("Category", "SPEC-0057:IT-01")]
    public void TwoArenas_ShouldShareSession()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var white = Render(ctx, session, 0);
        var black = Render(ctx, session, 1);

        Click(white, "e2");
        Click(white, "e4");

        black.WaitForAssertion(() =>
        {
            Assert.Equal("e4", Normalize(black.Find("[data-move-list] li").TextContent).Replace("1. ", "", StringComparison.Ordinal));
            Assert.Equal("from", black.Find("button[data-square='e2']").GetAttribute("data-last"));
            Assert.Equal("to", black.Find("button[data-square='e4']").GetAttribute("data-last"));
        });

        Click(black, "e7");
        Click(black, "e5");

        Assert.Equal(2, session.Snapshot().Moves.Count);
    }

    // ---------- E2E-01 ----------

    [Fact(DisplayName = "SPEC-0057:E2E-01 — mate do pastor por cliques: lista em SAN, relógio alterna e cartão final")]
    [Trait("Category", "SPEC-0057:E2E-01")]
    public void Journey_ShouldPlayScholarsMate()
    {
        using var ctx = NewContext();
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        var white = Render(ctx, session, 0);
        var black = Render(ctx, session, 1);

        (IRenderedComponent<ChessArena> Arena, string From, string To)[] script =
        [
            (white, "e2", "e4"), (black, "e7", "e5"), (white, "f1", "c4"), (black, "b8", "c6"),
            (white, "d1", "h5"), (black, "g8", "f6"), (white, "h5", "f7"),
        ];

        for (var i = 0; i < script.Length; i++)
        {
            var (arena, from, to) = script[i];
            Click(arena, from);
            Click(arena, to);
            time.Advance(5 * Second);
            if (i == 0)
            {
                Assert.Equal("true", Card(white, "black").GetAttribute("data-active"));
            }
            else if (i == 1)
            {
                Assert.Equal("true", Card(white, "white").GetAttribute("data-active"));
            }
        }

        var san = white.FindAll("[data-move-list] li").Select(li => Normalize(li.TextContent)).ToArray();
        Assert.Equal(["1. e4 e5", "2. Bc4 Nc6", "3. Qh5 Nf6", "4. Qxf7#"], san);
        foreach (var arena in new[] { white, black })
        {
            Assert.Equal("Xeque-mate — Ana venceu", arena.Find("[data-end-title]").TextContent.Trim());
            Assert.Contains("4 lances", arena.Find("[data-end-summary]").TextContent, StringComparison.Ordinal);
            Assert.Equal("true", arena.Find("[role=grid]").GetAttribute("aria-disabled"));
        }
    }

    // ---------- Ajustes da review G4 ----------

    [Fact(DisplayName = "SPEC-0057:UT-04 — o pulso de 1 s usa o TimeProvider do DI e atualiza o mm:ss sem re-render forçado")]
    [Trait("Category", "SPEC-0057:UT-04")]
    public void Arena_ShouldTickClockWithInjectedTimeProvider()
    {
        using var ctx = NewContext();
        var time = new ManualTime();
        ctx.Services.AddSingleton<TimeProvider>(time);
        using var session = ChessSessionTests.New(time);
        var cut = Render(ctx, session);
        ChessSessionTests.Line(session, "e2e4");
        Assert.Equal("05:00", Clock(cut, "black"));

        time.Advance(Second);

        Assert.Equal("04:59", Clock(cut, "black"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-04 — sem relógio correndo (após a bandeira) não há alerta de tempo baixo")]
    [Trait("Category", "SPEC-0057:UT-04")]
    public void Arena_ShouldNotAlertWhenClockIsNotRunning()
    {
        using var ctx = NewContext();
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        var cut = Render(ctx, session);
        ChessSessionTests.Line(session, "e2e4");
        time.Advance(301 * Second);
        session.Tick();
        Refresh(cut);

        Assert.Equal("00:00", Clock(cut, "black"));
        Assert.NotEqual("true", Card(cut, "black").QuerySelector("[data-clock]")!.GetAttribute("data-low"));
        Assert.Empty(cut.FindAll("[data-notice='low-time']"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-07 — o fim da partida é anunciado na única região viva, com o título do resultado")]
    [Trait("Category", "SPEC-0057:UT-07")]
    public void Arena_ShouldAnnounceEndInSingleLiveRegion()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var cut = Render(ctx, session);

        ChessSessionTests.Line(session, "f2f3", "e7e5", "g2g4", "d8h4");

        Assert.Single(cut.FindAll("[aria-live]"));
        Assert.Contains("Xeque-mate — Bia venceu", Notices(cut), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0057:UT-02 — 'Lance recusado.' some quando o estado da sessão muda")]
    [Trait("Category", "SPEC-0057:UT-02")]
    public void Arena_ShouldClearRejectedMessageOnStateChange()
    {
        using var ctx = NewContext();
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        var black = Render(ctx, session, 1);
        ChessSessionTests.Line(session, "e2e4");
        Click(black, "e7");
        time.Advance(301 * Second);

        Click(black, "e5");

        Assert.Contains("Lance recusado.", Notices(black), StringComparison.Ordinal);

        session.RestartCore(swapColors: false);

        Assert.DoesNotContain("Lance recusado.", Notices(black), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "SPEC-0057:UT-08 — cartão final: verde só para quem venceu, vermelho para quem perdeu")]
    [Trait("Category", "SPEC-0057:UT-08")]
    public void Arena_ShouldColorEndCardByViewerOutcome()
    {
        using var ctx = NewContext();
        using var session = ChessSessionTests.New();
        var white = Render(ctx, session, 0);
        var black = Render(ctx, session, 1);

        ChessSessionTests.Line(session, "f2f3", "e7e5", "g2g4", "d8h4");

        Assert.Equal("loss", white.Find("[data-end-card]").GetAttribute("data-result"));
        Assert.Equal("win", black.Find("[data-end-card]").GetAttribute("data-result"));
    }

    [Fact(DisplayName = "SPEC-0057:UT-09 — trocar o parâmetro Session resubscreve: a sessão antiga deixa de renderizar")]
    [Trait("Category", "SPEC-0057:UT-09")]
    public void Arena_ShouldResubscribeWhenSessionChanges()
    {
        using var ctx = NewContext();
        using var first = ChessSessionTests.New();
        using var second = ChessSessionTests.New();
        var cut = Render(ctx, first);

        cut.Render(p => p.Add(c => c.Session, second));
        var renders = cut.RenderCount;
        ChessSessionTests.Line(first, "e2e4");
        Assert.Equal(renders, cut.RenderCount);
        Assert.Empty(cut.FindAll("[data-move-list] li"));

        ChessSessionTests.Line(second, "d2d4");

        Assert.Equal("d4", Normalize(cut.Find("[data-move-list] li").TextContent).Replace("1. ", "", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SPEC-0057:UT-09 — 'Pouco tempo' aparece na região viva só com o relógio correndo abaixo de 10 s")]
    [Trait("Category", "SPEC-0057:UT-09")]
    public void Arena_ShouldAnnounceLowTimeOnce()
    {
        using var ctx = NewContext();
        var time = new ManualTime();
        using var session = ChessSessionTests.New(time);
        var cut = Render(ctx, session);
        ChessSessionTests.Line(session, "e2e4");
        Assert.Empty(cut.FindAll("[data-notice='low-time']"));

        time.Advance(295 * Second);
        Refresh(cut);

        Assert.Single(cut.FindAll("[aria-live]"));
        Assert.Contains("Pouco tempo, Bia!", Notices(cut), StringComparison.Ordinal);
    }
}

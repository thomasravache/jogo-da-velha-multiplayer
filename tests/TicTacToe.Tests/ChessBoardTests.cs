using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TicTacToe.Modules.Chess;
using TicTacToe.Web.Components.Chess;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0055: tabuleiro interativo e seletor de promoção

public class ChessBoardTests
{
    private static readonly string[] FilesWhite = ["a", "b", "c", "d", "e", "f", "g", "h"];
    private static readonly string[] FilesBlack = ["h", "g", "f", "e", "d", "c", "b", "a"];
    private static readonly string[] RanksWhite = ["8", "7", "6", "5", "4", "3", "2", "1"];
    private static readonly string[] RanksBlack = ["1", "2", "3", "4", "5", "6", "7", "8"];
    private const string PromotionFen = "k7/4P3/8/8/8/8/8/K7 w - - 0 1";

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    private static IRenderedComponent<ChessBoard> RenderBoard(
        BunitContext ctx,
        Position? position = null,
        PieceColor orientation = PieceColor.White,
        Action<ComponentParameterCollectionBuilder<ChessBoard>>? extra = null) =>
        ctx.Render<ChessBoard>(p =>
        {
            p.Add(c => c.Position, position ?? Position.Start).Add(c => c.Orientation, orientation);
            extra?.Invoke(p);
        });

    private static AngleSharp.Dom.IElement Sq(IRenderedComponent<ChessBoard> cut, string square) =>
        cut.Find($"button[data-square='{square}']");

    private static string Label(IRenderedComponent<ChessBoard> cut, string square) =>
        Sq(cut, square).GetAttribute("aria-label") ?? "";

    private static string[] SquareOrder(IRenderedComponent<ChessBoard> cut) =>
        [.. cut.FindAll("button[data-square]").Select(b => b.GetAttribute("data-square")!)];

    // ---------- UT-02 ----------

    [Theory(DisplayName = "SPEC-0055:UT-02 — Posição inicial: 64 casas, 32 peças, coordenadas e fileira 1 embaixo/em cima conforme a orientação")]
    [Trait("Category", "SPEC-0055:UT-02")]
    [InlineData(PieceColor.White)]
    [InlineData(PieceColor.Black)]
    public void Board_ShouldRenderStartPosition(PieceColor orientation)
    {
        using var ctx = NewContext();

        var cut = RenderBoard(ctx, orientation: orientation);

        Assert.Equal("grid", cut.Find("[role=grid]").GetAttribute("role"));
        var order = SquareOrder(cut);
        Assert.Equal(64, order.Length);
        Assert.Equal(64, order.Distinct().Count());
        Assert.Equal(32, cut.FindAll("button[data-square] svg[data-piece]").Count);

        var expectedFirst = orientation == PieceColor.White ? "a8" : "h1";
        var expectedLast = orientation == PieceColor.White ? "h1" : "a8";
        Assert.Equal(expectedFirst, order[0]);
        Assert.Equal(expectedLast, order[63]);

        var files = cut.FindAll("[data-coord='file']").Select(e => e.TextContent).ToArray();
        var ranks = cut.FindAll("[data-coord='rank']").Select(e => e.TextContent).ToArray();
        Assert.Equal(orientation == PieceColor.White ? FilesWhite : FilesBlack, files);
        Assert.Equal(orientation == PieceColor.White ? RanksWhite : RanksBlack, ranks);

        Assert.Equal("wK", Sq(cut, "e1").QuerySelector("svg")!.GetAttribute("data-piece"));
        Assert.Equal("bQ", Sq(cut, "d8").QuerySelector("svg")!.GetAttribute("data-piece"));
        Assert.Equal("wP", Sq(cut, "e2").QuerySelector("svg")!.GetAttribute("data-piece"));
        Assert.Null(Sq(cut, "e4").QuerySelector("svg"));
        Assert.Equal("e2, peão branco", Label(cut, "e2"));
        Assert.Equal("e4, vazia", Label(cut, "e4"));
        Assert.Equal("d8, dama preta", Label(cut, "d8"));
    }

    // ---------- UT-03 ----------

    [Fact(DisplayName = "SPEC-0055:UT-03 — Seleção, destinos (vazio e captura), último lance e xeque têm marcação própria além da cor")]
    [Trait("Category", "SPEC-0055:UT-03")]
    public void Board_ShouldMarkHighlightsBeyondColor()
    {
        using var ctx = NewContext();
        var position = Position.FromFen("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1");

        var cut = RenderBoard(ctx, position, extra: p => p
            .Add(c => c.Selected, Square.Parse("e4"))
            .Add(c => c.Destinations, [Square.Parse("e5"), Square.Parse("d5")])
            .Add(c => c.LastMove, new Move(Square.Parse("e2"), Square.Parse("e4")))
            .Add(c => c.CheckSquare, Square.Parse("e1")));

        var selected = Sq(cut, "e4");
        Assert.Equal("true", selected.GetAttribute("data-selected"));
        Assert.Equal("true", selected.GetAttribute("aria-pressed"));
        Assert.Contains("selecionada", Label(cut, "e4"));
        Assert.Contains("último lance", Label(cut, "e4"));
        Assert.Equal("to", selected.GetAttribute("data-last"));

        var move = Sq(cut, "e5");
        Assert.Equal("move", move.GetAttribute("data-target"));
        Assert.Contains("destino possível", Label(cut, "e5"));
        Assert.NotNull(move.QuerySelector("[data-marker='dot']"));

        var capture = Sq(cut, "d5");
        Assert.Equal("capture", capture.GetAttribute("data-target"));
        Assert.Contains("captura possível", Label(cut, "d5"));
        Assert.NotNull(capture.QuerySelector("[data-marker='ring']"));

        var from = Sq(cut, "e2");
        Assert.Equal("from", from.GetAttribute("data-last"));
        Assert.Contains("último lance", Label(cut, "e2"));

        var check = Sq(cut, "e1");
        Assert.Equal("true", check.GetAttribute("data-check"));
        Assert.Contains("rei em xeque", Label(cut, "e1"));

        var plain = Sq(cut, "a1");
        Assert.Null(plain.GetAttribute("data-selected"));
        Assert.Null(plain.GetAttribute("data-target"));
        Assert.Null(plain.GetAttribute("data-last"));
        Assert.Null(plain.GetAttribute("data-check"));
        Assert.Equal("false", plain.GetAttribute("aria-pressed"));
        Assert.Equal("a1, vazia", Label(cut, "a1"));
    }

    [Fact(DisplayName = "SPEC-0055:UT-03 — Captura en passant é marcada como captura mesmo com a casa vazia")]
    [Trait("Category", "SPEC-0055:UT-03")]
    public void Board_ShouldMarkEnPassantTargetAsCapture()
    {
        using var ctx = NewContext();
        var position = Position.FromFen("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 2");

        var cut = RenderBoard(ctx, position, extra: p => p
            .Add(c => c.Selected, Square.Parse("e5"))
            .Add(c => c.Destinations, [Square.Parse("e6"), Square.Parse("d6")]));

        Assert.Equal("capture", Sq(cut, "d6").GetAttribute("data-target"));
        Assert.Equal("move", Sq(cut, "e6").GetAttribute("data-target"));
    }

    // ---------- UT-04 ----------

    [Fact(DisplayName = "SPEC-0055:UT-04 — Cliques ativam as casas na ordem; tabuleiro não interativo ignora")]
    [Trait("Category", "SPEC-0055:UT-04")]
    public void Board_ShouldRaiseActivationsInOrder_AndIgnoreWhenNotInteractive()
    {
        using var ctx = NewContext();
        var activated = new List<Square>();

        var cut = RenderBoard(ctx, extra: p => p.Add(c => c.OnSquareActivated, EventCallback.Factory.Create<Square>(this, s => activated.Add(s))));
        Sq(cut, "e2").Click();
        Sq(cut, "e4").Click();
        Sq(cut, "e4").Click();
        Sq(cut, "d1").Click();

        Assert.Equal(["e2", "e4", "e4", "d1"], activated.Select(s => s.ToString()).ToArray());

        activated.Clear();
        cut.Render(p => p.Add(c => c.Interactive, false));
        Sq(cut, "e2").Click();
        Sq(cut, "e2").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Empty(activated);
        Assert.Equal("true", Sq(cut, "e2").GetAttribute("aria-disabled"));
    }

    // ---------- UT-05 ----------

    [Fact(DisplayName = "SPEC-0055:UT-05 — Setas movem o foco com roving tabindex sem sair da grade")]
    [Trait("Category", "SPEC-0055:UT-05")]
    public void Board_ArrowKeys_ShouldMoveRovingTabindex()
    {
        using var ctx = NewContext();
        var cut = RenderBoard(ctx);

        string Focused() => cut.Find("button[data-square][tabindex='0']").GetAttribute("data-square")!;

        Assert.Single(cut.FindAll("button[data-square][tabindex='0']"));
        Assert.Equal(63, cut.FindAll("button[data-square][tabindex='-1']").Count);
        Assert.Equal("a1", Focused());

        Sq(cut, "a1").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.Equal("a2", Focused());
        Sq(cut, "a2").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("b2", Focused());
        Sq(cut, "b2").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("b1", Focused());
        Sq(cut, "b1").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("b1", Focused());
        Sq(cut, "b1").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("a1", Focused());
        Sq(cut, "a1").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("a1", Focused());

        for (var i = 0; i < 10; i++)
        {
            Sq(cut, Focused()).KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        }

        Assert.Equal("a8", Focused());
        Assert.Single(cut.FindAll("button[data-square][tabindex='0']"));
    }

    [Fact(DisplayName = "SPEC-0055:UT-05 — Setas seguem a orientação das pretas")]
    [Trait("Category", "SPEC-0055:UT-05")]
    public void Board_ArrowKeys_ShouldFollowBlackOrientation()
    {
        using var ctx = NewContext();
        var cut = RenderBoard(ctx, orientation: PieceColor.Black);

        Assert.Equal("h8", cut.Find("button[data-square][tabindex='0']").GetAttribute("data-square"));
        Sq(cut, "h8").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.Equal("h7", cut.Find("button[data-square][tabindex='0']").GetAttribute("data-square"));
        Sq(cut, "h7").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("g7", cut.Find("button[data-square][tabindex='0']").GetAttribute("data-square"));
    }

    [Theory(DisplayName = "SPEC-0055:UT-05 — Enter e Espaço ativam a casa focada; Esc cancela a seleção")]
    [Trait("Category", "SPEC-0055:UT-05")]
    [InlineData("Enter")]
    [InlineData(" ")]
    public void Board_EnterSpaceAndEscape_ShouldActivateAndCancel(string key)
    {
        using var ctx = NewContext();
        var activated = new List<Square>();
        var cancels = 0;

        var cut = RenderBoard(ctx, extra: p => p
            .Add(c => c.OnSquareActivated, EventCallback.Factory.Create<Square>(this, s => activated.Add(s)))
            .Add(c => c.OnCancel, EventCallback.Factory.Create(this, () => cancels++)));

        Sq(cut, "e2").KeyDown(new KeyboardEventArgs { Key = key });
        Assert.Equal(["e2"], activated.Select(s => s.ToString()).ToArray());
        Assert.Equal("e2", cut.Find("button[data-square][tabindex='0']").GetAttribute("data-square"));

        Sq(cut, "e2").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, cancels);
        Assert.Single(activated);
    }

    [Fact(DisplayName = "SPEC-0055:UT-05 — O clique gerado pelo navegador após Enter não duplica a ativação")]
    [Trait("Category", "SPEC-0055:UT-05")]
    public void Board_KeyboardGeneratedClick_ShouldNotActivateTwice()
    {
        using var ctx = NewContext();
        var activated = new List<Square>();
        var cut = RenderBoard(ctx, extra: p => p.Add(c => c.OnSquareActivated, EventCallback.Factory.Create<Square>(this, s => activated.Add(s))));

        Sq(cut, "e2").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Sq(cut, "e2").Click();
        Assert.Single(activated);

        Sq(cut, "e2").Click();
        Assert.Equal(2, activated.Count);
    }

    // ---------- UT-06 ----------

    [Fact(DisplayName = "SPEC-0055:UT-06 — Arrastar e soltar em outra casa aciona OnDragMove; soltar fora ou na mesma casa é ignorado")]
    [Trait("Category", "SPEC-0055:UT-06")]
    public void Board_Drag_ShouldRaiseOnlyForValidDrop()
    {
        using var ctx = NewContext();
        var moves = new List<(Square From, Square To)>();
        var cut = RenderBoard(ctx, extra: p => p.Add(c => c.OnDragMove, EventCallback.Factory.Create<(Square From, Square To)>(this, m => moves.Add(m))));

        Assert.Equal("true", cut.Find("[data-drag='e2']").GetAttribute("draggable"));
        Assert.Empty(cut.FindAll("[data-drag='e4']"));

        // soltar na mesma casa
        cut.Find("[data-drag='e2']").DragStart(new DragEventArgs());
        Sq(cut, "e2").Drop(new DragEventArgs());
        Assert.Empty(moves);

        // soltar fora do tabuleiro (dragend sem drop) e depois soltar sem origem
        cut.Find("[data-drag='e2']").DragStart(new DragEventArgs());
        cut.Find("[data-drag='e2']").DragEnd(new DragEventArgs());
        Sq(cut, "e4").Drop(new DragEventArgs());
        Assert.Empty(moves);

        // soltar em destino
        cut.Find("[data-drag='e2']").DragStart(new DragEventArgs());
        Sq(cut, "e4").Drop(new DragEventArgs());
        Assert.Single(moves);
        Assert.Equal("e2", moves[0].From.ToString());
        Assert.Equal("e4", moves[0].To.ToString());

        // a origem é consumida pelo drop
        Sq(cut, "e5").Drop(new DragEventArgs());
        Assert.Single(moves);
    }

    [Fact(DisplayName = "SPEC-0055:UT-06 — Tabuleiro não interativo não permite arrastar")]
    [Trait("Category", "SPEC-0055:UT-06")]
    public void Board_NotInteractive_ShouldNotBeDraggable()
    {
        using var ctx = NewContext();
        var cut = RenderBoard(ctx, extra: p => p.Add(c => c.Interactive, false));

        Assert.Empty(cut.FindAll("[draggable='true']"));
    }

    // ---------- UT-07 ----------

    [Fact(DisplayName = "SPEC-0055:UT-07 — PromotionPicker: diálogo com quatro escolhas e foco inicial na dama")]
    [Trait("Category", "SPEC-0055:UT-07")]
    public void Picker_ShouldRenderFourChoicesInDialog()
    {
        using var ctx = NewContext();

        var cut = ctx.Render<PromotionPicker>(p => p.Add(c => c.Color, PieceColor.Black));

        var dialog = cut.Find("[role=dialog]");
        Assert.Equal("Promoção de peão", dialog.GetAttribute("aria-label"));
        var buttons = cut.FindAll("button");
        Assert.Equal(4, buttons.Count);
        Assert.Contains("Dama", buttons[0].TextContent);
        Assert.Contains("Torre", buttons[1].TextContent);
        Assert.Contains("Bispo", buttons[2].TextContent);
        Assert.Contains("Cavalo", buttons[3].TextContent);
        Assert.NotNull(buttons[0].GetAttribute("autofocus"));
        Assert.Equal("bQ", buttons[0].QuerySelector("svg")!.GetAttribute("data-piece"));
        ctx.JSInterop.VerifyFocusAsyncInvoke();
    }

    [Theory(DisplayName = "SPEC-0055:UT-07 — Clique e atalhos Q R B N escolhem a peça")]
    [Trait("Category", "SPEC-0055:UT-07")]
    [InlineData("Q", PieceType.Queen)]
    [InlineData("r", PieceType.Rook)]
    [InlineData("B", PieceType.Bishop)]
    [InlineData("n", PieceType.Knight)]
    public void Picker_ShortcutsAndClicks_ShouldChoose(string key, PieceType expected)
    {
        using var ctx = NewContext();
        var chosen = new List<PieceType>();
        var cut = ctx.Render<PromotionPicker>(p => p
            .Add(c => c.Color, PieceColor.White)
            .Add(c => c.OnChosen, EventCallback.Factory.Create<PieceType>(this, t => chosen.Add(t))));

        cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = key });
        Assert.Equal([expected], chosen);

        var index = new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight }.ToList().IndexOf(expected);
        cut.FindAll("button")[index].Click();
        Assert.Equal([expected, expected], chosen);
    }

    [Fact(DisplayName = "SPEC-0055:UT-07 — Esc cancela sem escolher; outras teclas são ignoradas")]
    [Trait("Category", "SPEC-0055:UT-07")]
    public void Picker_Escape_ShouldCancelWithoutChoosing()
    {
        using var ctx = NewContext();
        var chosen = new List<PieceType>();
        var cancels = 0;
        var cut = ctx.Render<PromotionPicker>(p => p
            .Add(c => c.Color, PieceColor.White)
            .Add(c => c.OnChosen, EventCallback.Factory.Create<PieceType>(this, t => chosen.Add(t)))
            .Add(c => c.OnCancel, EventCallback.Factory.Create(this, () => cancels++)));

        cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "x" });
        cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "K" });
        Assert.Empty(chosen);
        Assert.Equal(0, cancels);

        cut.Find("[role=dialog]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(chosen);
        Assert.Equal(1, cancels);
    }

    // ---------- IT-01 / E2E-01 ----------

    /// <summary>Anfitrião de teste: liga o ChessBoard a um ChessGame real, como a arena fará.</summary>
    private sealed class BoardHost(BunitContext ctx, ChessGame game, PieceColor orientation)
    {
        private Square? _selected;
        private Square? _pendingFrom;
        private Square? _pendingTo;
        private IRenderedComponent<PromotionPicker>? _picker;

        public ChessGame Game { get; } = game;

        public PieceColor Orientation { get; set; } = orientation;

        public Move? LastMove => Game.Moves.Count == 0 ? null : Game.Moves[^1].Move;

        public IRenderedComponent<ChessBoard> Board { get; private set; } = default!;

        public void Start() =>
            Board = ctx.Render<ChessBoard>(p => Fill(p));

        public void Click(string square)
        {
            Board.Find($"button[data-square='{square}']").Click();
        }

        public void ChoosePromotion(string label)
        {
            // O anfitrião real exibiria o seletor ao ver a promoção pendente.
            Assert.NotNull(_pendingTo);
            _picker = ctx.Render<PromotionPicker>(p => p
                .Add(c => c.Color, Game.Position.SideToMove)
                .Add(c => c.OnChosen, EventCallback.Factory.Create<PieceType>(this, OnPromotionChosen)));
            _picker.FindAll("button").Single(b => b.TextContent.Contains(label, StringComparison.Ordinal)).Click();
        }

        private void Refresh() => Board.Render(p => Fill(p));

        private void Fill(ComponentParameterCollectionBuilder<ChessBoard> p)
        {
            Square? check = Game.Position.IsInCheck(Game.Position.SideToMove) ? FindKing(Game.Position.SideToMove) : null;
            p.Add(c => c.Position, Game.Position)
                .Add(c => c.Orientation, Orientation)
                .Add(c => c.Selected, _selected)
                .Add(c => c.Destinations, _selected is { } s ? Game.DestinationsFrom(s) : [])
                .Add(c => c.LastMove, LastMove)
                .Add(c => c.CheckSquare, check)
                .Add(c => c.OnSquareActivated, EventCallback.Factory.Create<Square>(this, OnActivated))
                .Add(c => c.OnCancel, EventCallback.Factory.Create(this, () => { _selected = null; Refresh(); }));
        }

        private Square? FindKing(PieceColor color)
        {
            for (var i = 0; i < 64; i++)
            {
                if (Game.Position.PieceAt(new Square(i)) is { Type: PieceType.King } k && k.Color == color)
                {
                    return new Square(i);
                }
            }

            return null;
        }

        private void OnActivated(Square square)
        {
            var piece = Game.Position.PieceAt(square);
            if (_selected is { } from)
            {
                if (Game.DestinationsFrom(from).Contains(square))
                {
                    if (Game.NeedsPromotion(from, square))
                    {
                        _pendingFrom = from;
                        _pendingTo = square;
                        return;
                    }

                    Game.TryPlay(from, square, null, out _);
                    _selected = null;
                }
                else
                {
                    _selected = piece is { } p && p.Color == Game.Position.SideToMove && square != from ? square : null;
                }
            }
            else if (piece is { } own && own.Color == Game.Position.SideToMove)
            {
                _selected = square;
            }

            Refresh();
        }

        private void OnPromotionChosen(PieceType type)
        {
            Game.TryPlay(_pendingFrom!.Value, _pendingTo!.Value, type, out _);
            _pendingFrom = _pendingTo = null;
            _selected = null;
            Refresh();
        }
    }

    [Fact(DisplayName = "SPEC-0055:IT-01 — ChessBoard ligado a um ChessGame: e2-e4, e7-e5 e promoção acompanham o jogo e destacam o último lance")]
    [Trait("Category", "SPEC-0055:IT-01")]
    public void Board_ConnectedToGame_ShouldFollowMovesAndPromotion()
    {
        using var ctx = NewContext();
        var host = new BoardHost(ctx, new ChessGame(), PieceColor.White);
        host.Start();

        host.Click("e2");
        Assert.Equal("true", Sq(host.Board, "e2").GetAttribute("data-selected"));
        Assert.Equal("move", Sq(host.Board, "e4").GetAttribute("data-target"));
        Assert.Equal("move", Sq(host.Board, "e3").GetAttribute("data-target"));
        host.Click("e4");

        Assert.Equal("wP", Sq(host.Board, "e4").QuerySelector("svg")!.GetAttribute("data-piece"));
        Assert.Null(Sq(host.Board, "e2").QuerySelector("svg"));
        Assert.Equal("from", Sq(host.Board, "e2").GetAttribute("data-last"));
        Assert.Equal("to", Sq(host.Board, "e4").GetAttribute("data-last"));
        Assert.Empty(host.Board.FindAll("[data-target]"));

        host.Click("e7");
        host.Click("e5");
        Assert.Equal("bP", Sq(host.Board, "e5").QuerySelector("svg")!.GetAttribute("data-piece"));
        Assert.Equal("to", Sq(host.Board, "e5").GetAttribute("data-last"));
        Assert.Null(Sq(host.Board, "e4").GetAttribute("data-last"));
        Assert.Equal("e4 e5", string.Join(' ', host.Game.Moves.Select(m => m.Move.To.ToString())));

        var promo = new BoardHost(ctx, new ChessGame(Position.FromFen(PromotionFen)), PieceColor.White);
        promo.Start();
        promo.Click("e7");
        promo.Click("e8");
        promo.ChoosePromotion("Cavalo");

        Assert.Equal("wN", Sq(promo.Board, "e8").QuerySelector("svg")!.GetAttribute("data-piece"));
        Assert.Equal("to", Sq(promo.Board, "e8").GetAttribute("data-last"));
    }

    [Fact(DisplayName = "SPEC-0055:E2E-01 — Jornada: e4 por clique, e5 com orientação invertida e promoção com dama")]
    [Trait("Category", "SPEC-0055:E2E-01")]
    public void Journey_ShouldPlayByClicksAndPromote()
    {
        using var ctx = NewContext();
        var host = new BoardHost(ctx, new ChessGame(), PieceColor.White);
        host.Start();

        host.Click("e2");
        host.Click("e4");
        Assert.Equal("e4, peão branco, último lance", string.Join(", ", Label(host.Board, "e4").Split(", ").Take(3)));

        host.Orientation = PieceColor.Black;
        host.Board.Render(p => p.Add(c => c.Orientation, PieceColor.Black));
        Assert.Equal("h1", SquareOrder(host.Board)[0]);
        host.Click("e7");
        host.Click("e5");
        Assert.Equal("bP", Sq(host.Board, "e5").QuerySelector("svg")!.GetAttribute("data-piece"));
        Assert.Equal("to", Sq(host.Board, "e5").GetAttribute("data-last"));

        var promo = new BoardHost(ctx, new ChessGame(Position.FromFen(PromotionFen)), PieceColor.White);
        promo.Start();
        promo.Click("e7");
        promo.Click("e8");
        promo.ChoosePromotion("Dama");

        Assert.Equal("wQ", Sq(promo.Board, "e8").QuerySelector("svg")!.GetAttribute("data-piece"));
        Assert.Null(Sq(promo.Board, "e7").QuerySelector("svg"));
        Assert.Equal("from", Sq(promo.Board, "e7").GetAttribute("data-last"));
        Assert.Equal("to", Sq(promo.Board, "e8").GetAttribute("data-last"));
        Assert.Equal("e8, dama branca", string.Join(", ", Label(promo.Board, "e8").Split(", ").Take(2)));
    }
}

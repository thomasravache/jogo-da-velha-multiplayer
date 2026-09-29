using System.Text.RegularExpressions;
using Bunit;
using TicTacToe.Modules.Chess;
using TicTacToe.Web.Components.Chess;
using Xunit;

namespace TicTacToe.Tests;

// SPEC-0055: tokens de cor do tabuleiro e contraste

public class ChessDesignTokensTests
{
    private static readonly string RootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    private static string Css() =>
        File.ReadAllText(Path.Combine(RootDir, "src/TicTacToe/TicTacToe.Web/Styles/cyber-arena.input.css"));

    private static string Hex(string css, string token)
    {
        var m = Regex.Match(css, $@"--color-{token}:\s*(#[0-9a-fA-F]{{6}})");
        Assert.True(m.Success, $"token --color-{token} deve ser hexadecimal de 6 dígitos");
        return m.Groups[1].Value;
    }

    private static double Lum(string hex)
    {
        static double Ch(string hex, int i)
        {
            var c = Convert.ToInt32(hex.Substring(1 + i * 2, 2), 16) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Ch(hex, 0) + 0.7152 * Ch(hex, 1) + 0.0722 * Ch(hex, 2);
    }

    private static double Ratio(string a, string b)
    {
        var la = Lum(a);
        var lb = Lum(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    [Fact(DisplayName = "SPEC-0055:UT-08 — Contornos e destaques têm contraste >= 3:1 com as duas casas; casas distinguíveis (>= 1,2:1)")]
    [Trait("Category", "SPEC-0055:UT-08")]
    public void BoardTokens_ShouldMeetContrast()
    {
        var css = Css();
        var light = Hex(css, "board-light");
        var dark = Hex(css, "board-dark");
        Assert.Equal("#00d2d3", Hex(css, "piece-white-outline").ToLowerInvariant());
        Assert.Equal("#ff4757", Hex(css, "piece-black-outline").ToLowerInvariant());

        Assert.True(Ratio(light, dark) >= 1.2, $"casa clara x escura: {Ratio(light, dark):F2}");

        foreach (var token in new[] { "piece-white-outline", "piece-black-outline", "square-selected", "square-target", "square-last", "square-check" })
        {
            var fg = Hex(css, token);
            Assert.True(Ratio(fg, light) >= 3.0, $"{token} sobre casa clara: {Ratio(fg, light):F2}");
            Assert.True(Ratio(fg, dark) >= 3.0, $"{token} sobre casa escura: {Ratio(fg, dark):F2}");
        }

        // Informativo: preenchimentos existem e são hexadecimais (a legibilidade vem do contorno).
        Assert.NotEmpty(Hex(css, "piece-white-fill"));
        Assert.Equal("#1b2030", Hex(css, "piece-black-fill").ToLowerInvariant());
    }

    [Fact(DisplayName = "SPEC-0055:UT-08 — As casas do tabuleiro são tons escuros (luminância <= 0,055)")]
    [Trait("Category", "SPEC-0055:UT-08")]
    public void BoardSquares_ShouldBeDarkTones()
    {
        var css = Css();
        Assert.True(Lum(Hex(css, "board-light")) <= 0.055);
        Assert.True(Lum(Hex(css, "board-dark")) <= 0.055);
    }

    [Fact(DisplayName = "SPEC-0055:UT-09 — Contêiner do tabuleiro é quadrado e responsivo; sem <style> inline nem MudBlazor")]
    [Trait("Category", "SPEC-0055:UT-09")]
    public void Markup_ShouldBeResponsiveAndFreeOfLegacy()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var board = ctx.Render<ChessBoard>(p => p.Add(c => c.Position, Position.Start));
        var picker = ctx.Render<PromotionPicker>(p => p.Add(c => c.Color, PieceColor.White));
        var piece = ctx.Render<ChessPiece>(p => p.Add(c => c.Color, PieceColor.White).Add(c => c.Type, PieceType.Rook));

        var container = board.Find("[data-board]").GetAttribute("class") ?? "";
        Assert.Contains("w-full", container);
        Assert.Contains("max-w-", container);
        Assert.Contains("aspect-square", container);

        foreach (var markup in new[] { board.Markup, picker.Markup, piece.Markup })
        {
            Assert.DoesNotContain("<style", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("mud-", markup, StringComparison.Ordinal);
        }
    }
}

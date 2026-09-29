using System.Globalization;

namespace TicTacToe.Web.Components.Ui;

/// <summary>Rótulo de data do histórico: "Hoje, HH:mm", "Ontem, HH:mm" ou "dd/MM HH:mm" (fuso local).</summary>
public static class HistoryDateFormatter
{
    public static string Format(DateTime playedAtUtc, DateTime nowLocal)
    {
        var local = DateTime.SpecifyKind(playedAtUtc, DateTimeKind.Utc).ToLocalTime();
        var days = (nowLocal.Date - local.Date).Days;
        return days switch
        {
            0 => $"Hoje, {local.ToString("HH:mm", CultureInfo.InvariantCulture)}",
            1 => $"Ontem, {local.ToString("HH:mm", CultureInfo.InvariantCulture)}",
            _ => local.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture),
        };
    }
}

namespace TicTacToe.Modules.Chess;

public sealed record TimeControl(string Id, string Name, TimeSpan Initial, TimeSpan Increment)
{
    public static TimeControl Bullet { get; } = new("bullet1+0", "Bullet 1+0", TimeSpan.FromMinutes(1), TimeSpan.Zero);

    public static TimeControl Blitz { get; } = new("blitz5+0", "Blitz 5+0", TimeSpan.FromMinutes(5), TimeSpan.Zero);

    public static TimeControl Rapid { get; } = new("rapida10+5", "Rápida 10+5", TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(5));

    public static IReadOnlyList<TimeControl> All { get; } = [Bullet, Blitz, Rapid];

    public static TimeControl? FromId(string id) => id is null ? null : null;
}

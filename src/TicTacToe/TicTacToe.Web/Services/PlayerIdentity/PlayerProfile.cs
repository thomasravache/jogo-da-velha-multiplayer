namespace TicTacToe.Web.Services.PlayerIdentity;

/// <summary>Identidade anônima do jogador: <see cref="PlayerId"/> pseudônimo (não é credencial) e último apelido.</summary>
public sealed record PlayerProfile(Guid PlayerId, string? Nickname);

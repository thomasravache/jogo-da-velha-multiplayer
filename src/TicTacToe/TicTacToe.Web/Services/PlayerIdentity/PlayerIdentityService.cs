namespace TicTacToe.Web.Services.PlayerIdentity;

public sealed class PlayerIdentityService(IPlayerStorage storage)
{
    internal IPlayerStorage Storage { get; } = storage;

    public const string StorageKey = "xo.player";

    public const int MaxNicknameLength = 20;

    public ValueTask<PlayerProfile> LoadAsync() => throw new NotImplementedException();

    public ValueTask SaveNicknameAsync(string nickname) => throw new NotImplementedException();
}

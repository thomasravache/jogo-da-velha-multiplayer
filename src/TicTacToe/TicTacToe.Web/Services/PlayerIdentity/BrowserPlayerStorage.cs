using Microsoft.JSInterop;

namespace TicTacToe.Web.Services.PlayerIdentity;

public sealed class BrowserPlayerStorage(IJSRuntime js) : IPlayerStorage
{
    internal IJSRuntime Js { get; } = js;

    public ValueTask<string?> GetAsync(string key) => throw new NotImplementedException();

    public ValueTask SetAsync(string key, string value) => throw new NotImplementedException();
}

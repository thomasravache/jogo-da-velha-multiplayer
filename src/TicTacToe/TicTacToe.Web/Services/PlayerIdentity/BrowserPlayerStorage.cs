using Microsoft.JSInterop;

namespace TicTacToe.Web.Services.PlayerIdentity;

/// <summary>Armazenamento no <c>localStorage</c> do navegador (só disponível após a renderização interativa).</summary>
public sealed class BrowserPlayerStorage(IJSRuntime js) : IPlayerStorage
{
    public ValueTask<string?> GetAsync(string key) => js.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask SetAsync(string key, string value) => js.InvokeVoidAsync("localStorage.setItem", key, value);
}

namespace TicTacToe.Web.Services.PlayerIdentity;

/// <summary>Armazenamento de chave/valor do navegador (localStorage no app; em memória nos testes).</summary>
public interface IPlayerStorage
{
    ValueTask<string?> GetAsync(string key);

    ValueTask SetAsync(string key, string value);
}

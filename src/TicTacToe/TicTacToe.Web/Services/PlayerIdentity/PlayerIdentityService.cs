using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace TicTacToe.Web.Services.PlayerIdentity;

/// <summary>
/// Identidade anônima do jogador (ADR-0009): um <see cref="PlayerProfile.PlayerId"/> pseudônimo guardado no navegador.
/// O ID não é credencial: nenhuma ação o aceita como autorização.
/// </summary>
public sealed class PlayerIdentityService(IPlayerStorage storage, ILogger<PlayerIdentityService>? logger = null) : IDisposable
{
    public const string StorageKey = "xo.player";

    public const int MaxNicknameLength = 20;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private PlayerProfile? _profile;

    /// <summary>Carrega (ou cria) a identidade. Nunca lança: sem armazenamento, devolve um perfil só da sessão.</summary>
    public async ValueTask<PlayerProfile> LoadAsync()
    {
        if (_profile is not null) return _profile;

        await _gate.WaitAsync();
        try
        {
            return await LoadCoreAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Guarda o apelido (aparado e limitado a 20 caracteres); texto vazio é ignorado.</summary>
    public async ValueTask SaveNicknameAsync(string nickname)
    {
        var clamped = Clamp(nickname);
        if (clamped is null) return;

        await _gate.WaitAsync();
        try
        {
            var profile = await LoadCoreAsync();
            _profile = profile with { Nickname = clamped };
            await PersistAsync(_profile);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Chamado com o portão já adquirido.
    private async ValueTask<PlayerProfile> LoadCoreAsync()
    {
        if (_profile is not null) return _profile;

        PlayerProfile? stored = null;
        try
        {
            stored = Parse(await storage.GetAsync(StorageKey));
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            logger?.LogWarning(ex, "Armazenamento do navegador indisponível; usando identidade apenas desta sessão.");
        }

        _profile = stored ?? new PlayerProfile(Guid.NewGuid(), null);
        if (stored is null)
        {
            await PersistAsync(_profile);
        }

        return _profile;
    }

    public void Dispose() => _gate.Dispose();

    private static string? Clamp(string? nickname)
    {
        var trimmed = nickname?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed[..Math.Min(MaxNicknameLength, trimmed.Length)];
    }

    private static PlayerProfile? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty("id", out var idProp) || idProp.ValueKind != JsonValueKind.String) return null;
            if (!Guid.TryParse(idProp.GetString(), out var id) || id == Guid.Empty) return null;

            string? nick = root.TryGetProperty("nick", out var nickProp) && nickProp.ValueKind == JsonValueKind.String
                ? Clamp(nickProp.GetString())
                : null;
            return new PlayerProfile(id, nick);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async ValueTask PersistAsync(PlayerProfile profile)
    {
        try
        {
            await storage.SetAsync(StorageKey, JsonSerializer.Serialize(new { id = profile.PlayerId, nick = profile.Nickname }));
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            logger?.LogWarning(ex, "Não foi possível gravar a identidade no navegador; ela vale apenas nesta sessão.");
        }
    }

    private static bool IsStorageFailure(Exception ex) =>
        ex is JSException or JSDisconnectedException or InvalidOperationException or OperationCanceledException or TimeoutException;
}

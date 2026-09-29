using System.Text.Json;
using Microsoft.JSInterop;

namespace TicTacToe.Web.Services.PlayerIdentity;

/// <summary>
/// Identidade anônima do jogador (ADR-0009): um <see cref="PlayerProfile.PlayerId"/> pseudônimo guardado no navegador.
/// O ID não é credencial: nenhuma ação o aceita como autorização.
/// </summary>
public sealed class PlayerIdentityService(IPlayerStorage storage)
{
    public const string StorageKey = "xo.player";

    public const int MaxNicknameLength = 20;

    private PlayerProfile? _profile;

    /// <summary>Carrega (ou cria) a identidade. Nunca lança: sem armazenamento, devolve um perfil só da sessão.</summary>
    public async ValueTask<PlayerProfile> LoadAsync()
    {
        if (_profile is not null) return _profile;

        PlayerProfile? stored = null;
        try
        {
            stored = Parse(await storage.GetAsync(StorageKey));
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            // Armazenamento indisponível (modo privado, circuito caído): segue com perfil de sessão.
        }

        _profile = stored ?? new PlayerProfile(Guid.NewGuid(), null);
        if (stored is null)
        {
            await PersistAsync(_profile);
        }

        return _profile;
    }

    /// <summary>Guarda o apelido (aparado e limitado a 20 caracteres); texto vazio é ignorado.</summary>
    public async ValueTask SaveNicknameAsync(string nickname)
    {
        var trimmed = nickname?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        var profile = await LoadAsync();
        _profile = profile with { Nickname = trimmed[..Math.Min(MaxNicknameLength, trimmed.Length)] };
        await PersistAsync(_profile);
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
                ? nickProp.GetString()
                : null;
            return new PlayerProfile(id, string.IsNullOrWhiteSpace(nick) ? null : nick);
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
            // Sem persistência: a identidade vale apenas para esta sessão.
        }
    }

    private static bool IsStorageFailure(Exception ex) => ex is JSException or JSDisconnectedException or InvalidOperationException;
}

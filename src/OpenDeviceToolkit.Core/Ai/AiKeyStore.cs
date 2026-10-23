using System.Text;
using System.Text.Json;

namespace OpenDeviceToolkit.Core.Ai;

/// <summary>
/// Stores AI provider API keys at rest, DPAPI-encrypted (CurrentUser scope).
/// Keys live in a per-user file, never in appsettings.json and never in the
/// repository. A corrupt or unreadable file is treated as an empty store
/// rather than an error, so a bad file cannot stop the application.
/// </summary>
public sealed class AiKeyStore
{
    private readonly string _path;

    public AiKeyStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenDeviceToolkit",
            "ai-keys.bin");
    }

    public string? GetKey(AiProviderKind kind)
    {
        var keys = Load();
        return keys.TryGetValue(KeyName(kind), out var value) ? value : null;
    }

    public void SaveKey(AiProviderKind kind, string apiKey)
    {
        if (kind is AiProviderKind.Ollama or AiProviderKind.None)
            throw new ArgumentException($"{kind} does not use API keys.", nameof(kind));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key must not be empty.", nameof(apiKey));

        var keys = Load();
        keys[KeyName(kind)] = apiKey.Trim();
        Persist(keys);
    }

    public void DeleteKey(AiProviderKind kind)
    {
        var keys = Load();
        if (keys.Remove(KeyName(kind)))
            Persist(keys);
    }

    private static string KeyName(AiProviderKind kind) => kind.ToString();

    private Dictionary<string, string> Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new Dictionary<string, string>();

            var decrypted = Dpapi.Unprotect(File.ReadAllBytes(_path));
            var json = Encoding.UTF8.GetString(decrypted);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch
        {
            // Corrupt or unreadable file (or non-Windows platform):
            // start empty rather than failing the application.
            return new Dictionary<string, string>();
        }
    }

    private void Persist(Dictionary<string, string> keys)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(keys);
        File.WriteAllBytes(_path, Dpapi.Protect(Encoding.UTF8.GetBytes(json)));
    }
}

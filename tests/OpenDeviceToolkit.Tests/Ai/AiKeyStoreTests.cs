using OpenDeviceToolkit.Core.Ai;
using Xunit;

namespace OpenDeviceToolkit.Tests.Ai;

public class AiKeyStoreTests
{
    private static string TempPath()
        => Path.Combine(Path.GetTempPath(), "odt-aikeys-" + Guid.NewGuid().ToString("N") + ".bin");

    [Fact]
    public void RoundTrip_PersistsAndDeletesKeys()
    {
        if (!OperatingSystem.IsWindows())
            return; // DPAPI is Windows-only

        var path = TempPath();
        try
        {
            var store = new AiKeyStore(path);
            store.SaveKey(AiProviderKind.Mistral, "  my-key  ");
            Assert.Equal("my-key", store.GetKey(AiProviderKind.Mistral));

            // A brand new instance over the same file sees the same key.
            Assert.Equal("my-key", new AiKeyStore(path).GetKey(AiProviderKind.Mistral));

            store.DeleteKey(AiProviderKind.Mistral);
            Assert.Null(store.GetKey(AiProviderKind.Mistral));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CorruptFile_IsTreatedAsEmptyStore()
    {
        var path = TempPath();
        try
        {
            File.WriteAllText(path, "this is not a DPAPI blob");
            var store = new AiKeyStore(path);
            Assert.Null(store.GetKey(AiProviderKind.OpenAi));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void SaveKey_RejectsProvidersWithoutKeys()
    {
        var store = new AiKeyStore(TempPath());
        Assert.Throws<ArgumentException>(() => store.SaveKey(AiProviderKind.Ollama, "x"));
        Assert.Throws<ArgumentException>(() => store.SaveKey(AiProviderKind.None, "x"));
    }
}

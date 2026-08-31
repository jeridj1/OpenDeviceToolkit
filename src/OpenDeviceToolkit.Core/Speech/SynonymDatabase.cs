using System.Text.Json;

namespace OpenDeviceToolkit.Core.Speech;

public static class SynonymDatabase
{
    private static Dictionary<string, List<string>>? _synonyms;
    private static string? _customFilePath;
    
    static SynonymDatabase() => LoadDefaultSynonyms();
    
    public static void LoadFromFile(string path)
    {
        _customFilePath = path;
        if (File.Exists(path))
        {
            try { _synonyms = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(path)); }
            catch { _synonyms = null; }
        }
        if (_synonyms == null) LoadDefaultSynonyms();
    }
    
    public static void SaveToFile()
    {
        if (_synonyms == null || string.IsNullOrEmpty(_customFilePath)) return;
        try { File.WriteAllText(_customFilePath, JsonSerializer.Serialize(_synonyms, new JsonSerializerOptions { WriteIndented = true })); }
        catch { }
    }
    
    private static void LoadDefaultSynonyms()
    {
        _synonyms = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["phone"] = new List<string> { "device", "hardware", "gadget", "thing", "board", "chip", "unit" },
            ["lg"] = new List<string> { "lg phone", "v50", "v40", "v60", "g8", "g7", "stylo" },
            ["scan"] = new List<string> { "detect", "find", "look for", "check", "see", "what's", "discover", "identify" },
            ["unlock"] = new List<string> { "root", "gain access", "bypass", "hack", "crack", "open", "access", "control", "jailbreak" },
            ["generate"] = new List<string> { "create", "make", "build", "produce", "write", "save" },
            ["reboot"] = new List<string> { "restart", "reset", "power cycle", "turn off and on" },
            ["help"] = new List<string> { "what can", "commands", "tell me", "how to", "how do", "options", "list" },
            ["exit"] = new List<string> { "quit", "close", "stop", "goodbye", "bye", "shut down", "end" }
        };
    }
    
    public static IReadOnlyList<string> GetSynonyms(string word)
    {
        if (_synonyms == null) LoadDefaultSynonyms();
        return _synonyms.TryGetValue(word, out var synonyms) ? synonyms.AsReadOnly() : Array.Empty<string>();
    }
    
    public static bool AreSynonyms(string word1, string word2)
    {
        var synonyms = GetSynonyms(word1);
        return synonyms.Contains(word2, StringComparer.OrdinalIgnoreCase);
    }
    
    public static void AddSynonym(string word, string synonym)
    {
        if (_synonyms == null) LoadDefaultSynonyms();
        if (!_synonyms.ContainsKey(word)) _synonyms[word] = new List<string>();
        if (!_synonyms[word].Contains(synonym, StringComparer.OrdinalIgnoreCase)) _synonyms[word].Add(synonym);
    }
    
    public static bool RemoveSynonym(string word, string synonym)
    {
        if (_synonyms == null) return false;
        return _synonyms.TryGetValue(word, out var synonyms) && synonyms.RemoveAll(s => s.Equals(synonym, StringComparison.OrdinalIgnoreCase)) > 0;
    }
}

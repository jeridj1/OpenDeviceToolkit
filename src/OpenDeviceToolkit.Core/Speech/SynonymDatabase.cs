using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDeviceToolkit.Core.Speech;

/// <summary>
/// Manages synonyms for voice command parsing.
/// Allows customization and persistence of user-specific phrases.
/// </summary>
public static class SynonymDatabase
{
    private static Dictionary<string, List<string>>? _synonyms;
    private static string? _customFilePath;
    
    /// <summary>
    /// Initializes the synonym database.
    /// </summary>
    static SynonymDatabase()
    {
        LoadDefaultSynonyms();
    }
    
    /// <summary>
    /// Loads synonyms from a custom file.
    /// </summary>
    public static void LoadFromFile(string path)
    {
        _customFilePath = path;
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                _synonyms = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            }
            catch
            {
                _synonyms = null;
            }
        }
        
        // Ensure defaults are loaded
        if (_synonyms == null)
            LoadDefaultSynonyms();
    }
    
    /// <summary>
    /// Saves synonyms to the custom file.
    /// </summary>
    public static void SaveToFile()
    {
        if (_synonyms == null || string.IsNullOrEmpty(_customFilePath))
            return;
        
        try
        {
            var json = JsonSerializer.Serialize(_synonyms, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_customFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
    
    private static void LoadDefaultSynonyms()
    {
        _synonyms = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            // Devices
            ["phone"] = new List<string> { "device", "hardware", "gadget", "thing", "board", "chip", "unit" },
            ["lg"] = new List<string> { "lg phone", "lg device", "v50", "v40", "v60", "g8", "g7", "stylo" },
            ["samsung"] = new List<string> { "galaxy", "note", "s series", "a series" },
            ["pixel"] = new List<string> { "google phone", "google device" },
            
            // Actions: Scan/Detect
            ["scan"] = new List<string> { "detect", "find", "look for", "check", "see", "what's", "discover", "identify" },
            ["detect"] = new List<string> { "scan", "find", "look for", "check", "see", "discover", "identify" },
            
            // Actions: Access/Unlock
            ["unlock"] = new List<string> { "root", "gain access", "bypass", "hack", "crack", "open", "access", "control", "jailbreak" },
            ["root"] = new List<string> { "unlock", "gain access", "bypass", "hack", "crack", "open", "access", "jailbreak" },
            
            // Actions: Generate
            ["generate"] = new List<string> { "create", "make", "build", "produce", "write", "save" },
            ["report"] = new List<string> { "log", "file", "document", "summary", "info", "details" },
            
            // Actions: Reboot
            ["reboot"] = new List<string> { "restart", "reset", "power cycle", "turn off and on" },
            ["restart"] = new List<string> { "reboot", "reset", "power cycle" },
            
            // Actions: Help
            ["help"] = new List<string> { "what can", "commands", "tell me", "how to", "how do", "options", "list" },
            
            // Actions: Exit
            ["exit"] = new List<string> { "quit", "close", "stop", "goodbye", "bye", "shut down", "end" },
            
            // Connectivity
            ["usb"] = new List<string> { "usb cable", "usb port", "usb connection" },
            ["connected"] = new List<string> { "plugged in", "attached", "hooked up", "linked" },
            
            // States
            ["locked"] = new List<string> { "unlocked", "secured", "protected" },
            ["bootloader"] = new List<string> { "boot loader", "fastboot", "download mode", "edl mode", "9008 mode" },
            
            // Qualcomm specific
            ["edl"] = new List<string> { "emergency download", "9008", "qualcomm mode" },
            ["qualcomm"] = new List<string> { "qc", "snapdragon", "sd" },
            
            // Tools
            ["rp2040"] = new List<string> { "pico", "raspberry pi pico" },
            ["programmer"] = new List<string> { "flasher", "writer", "burner" },
            ["logic analyzer"] = new List<string> { "logic", "analyzer", "sniffer", "scope" },
            
            // Fillers (words to ignore)
            ["please"] = new List<string> { "can you", "could you", "would you" },
            ["the"] = new List<string> { "this", "that", "my", "a", "an" },
        };
    }
    
    /// <summary>
    /// Gets all synonyms for a word.
    /// </summary>
    public static IReadOnlyList<string> GetSynonyms(string word)
    {
        if (_synonyms == null)
            LoadDefaultSynonyms();
        
        if (_synonyms.TryGetValue(word, out var synonyms))
            return synonyms.AsReadOnly();
        
        return Array.Empty<string>();
    }
    
    /// <summary>
    /// Checks if two words are synonyms.
    /// </summary>
    public static bool AreSynonyms(string word1, string word2)
    {
        var synonyms1 = GetSynonyms(word1);
        return synonyms1.Contains(word2, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Adds a synonym for a word.
    /// </summary>
    public static void AddSynonym(string word, string synonym)
    {
        if (_synonyms == null)
            LoadDefaultSynonyms();
        
        if (!_synonyms.ContainsKey(word))
            _synonyms[word] = new List<string>();
        
        if (!_synonyms[word].Contains(synonym, StringComparer.OrdinalIgnoreCase))
            _synonyms[word].Add(synonym);
    }
    
    /// <summary>
    /// Removes a synonym for a word.
    /// </summary>
    public static bool RemoveSynonym(string word, string synonym)
    {
        if (_synonyms == null)
            return false;
        
        if (_synonyms.TryGetValue(word, out var synonyms))
            return synonyms.RemoveAll(s => s.Equals(synonym, StringComparison.OrdinalIgnoreCase)) > 0;
        
        return false;
    }
}

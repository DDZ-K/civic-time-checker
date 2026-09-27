using System.Text.Json;
using System.Text.Json.Serialization;

namespace CivicTimeChecker;

internal sealed class AppConfig
{
    public int MaxReadBytes { get; set; } = 262144;
    public int CacheFlushMinutes { get; set; } = 10;
    public int MaxLogLines { get; set; } = 400;
    public List<FolderProfile> Folders { get; set; } = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string AppDir
    {
        get
        {
            var path = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(path))
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                    return dir;
            }
            return AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }

    public static string ConfigPath => Path.Combine(AppDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                if (cfg != null)
                {
                    cfg.Normalize();
                    return cfg;
                }
            }
        }
        catch
        {
        }

        var fresh = new AppConfig();
        fresh.Normalize();
        return fresh;
    }

    public void Save()
    {
        Normalize();
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOptions));
    }

    public IReadOnlyList<FolderProfile> GetProfiles() => Folders;
    public IReadOnlyList<FolderProfile> GetDispenseProfiles() => Folders.Where(x => !x.IsBind).ToList();
    public IReadOnlyList<FolderProfile> GetBindProfiles() => Folders.Where(x => x.IsBind).ToList();

    public IReadOnlyList<FolderProfile> GetDispenseByPair(string pair)
        => Folders.Where(x => !x.IsBind && x.Pair == Pairs.Normalize(pair)).ToList();

    public void Normalize()
    {
        if (MaxReadBytes < 4096) MaxReadBytes = 4096;
        CacheFlushMinutes = Math.Clamp(CacheFlushMinutes <= 0 ? 10 : CacheFlushMinutes, 1, 120);
        MaxLogLines = Math.Clamp(MaxLogLines <= 0 ? 400 : MaxLogLines, 50, 2000);

        Folders ??= [];
        if (Folders.Count == 0)
        {
            Folders =
            [
                new FolderProfile { Name = "25点检", Role = Roles.Dispense, Pair = Pairs.Housing25, ClearGlueMinutes = 15 },
                new FolderProfile { Name = "30组装", Role = Roles.Bind, Pair = Pairs.Housing25 },
                new FolderProfile { Name = "60点胶", Role = Roles.Dispense, Pair = Pairs.Housing60, ClearGlueMinutes = 15 },
                new FolderProfile { Name = "70组装", Role = Roles.Bind, Pair = Pairs.Housing60 },
                new FolderProfile { Name = "100点胶", Role = Roles.Dispense, Pair = Pairs.Cover100, ClearGlueMinutes = 15 },
                new FolderProfile { Name = "110组装", Role = Roles.Bind, Pair = Pairs.Cover100 }
            ];
        }

        foreach (var folder in Folders)
            folder.Normalize();
        Folders = Folders.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
    }
}

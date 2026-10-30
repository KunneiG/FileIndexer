using System.IO;
using System.Text.Json;
using FileIndexer.Models;

namespace FileIndexer.Services;

public static class IndexCache
{
    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FileIndexer", "index.json");

    public static void Save(List<FileEntry> entries)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(CachePath, json);
        }
        catch
        {
            // Ошибки кэширования не должны приводить к падению приложения
        }
    }

    public static List<FileEntry> Load()
    {
        if (!File.Exists(CachePath))
            return new List<FileEntry>();

        try
        {
            var json = File.ReadAllText(CachePath);
            return JsonSerializer.Deserialize<List<FileEntry>>(json) ?? new List<FileEntry>();
        }
        catch
        {
            return new List<FileEntry>();
        }
    }
}
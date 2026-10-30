using System.IO;

namespace FileIndexer.Models;

public class FileEntry
{
    public string FullPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public string? ContentPreview { get; set; } // Первые 4 КБ текста
    public string Category { get; set; } = "Other"; // Image, Audio, Video, Text, Archive, Other

    // Вычисляемое свойство для удобного отображения размера в DataGrid
    public string SizeHuman => FormatSize(SizeBytes);

    private static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:0.##} {suffixes[i]}";
    }
}
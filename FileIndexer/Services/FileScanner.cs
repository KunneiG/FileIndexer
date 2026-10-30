using System.IO;
using System.Text;
using FileIndexer.Models;

namespace FileIndexer.Services;

public class FileScanner
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".txt", ".log", ".md", ".cs", ".json", ".xml", ".csv", ".html", ".css", ".js" };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".mp3", ".wav", ".flac", ".aac", ".ogg" };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".mp4", ".wmv", ".avi", ".mkv", ".mov" };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".zip", ".rar", ".7z", ".tar", ".gz" };

    /// <summary>
    /// Асинхронно сканирует указанные корневые папки и возвращает список файлов.
    /// </summary>
    public async Task<List<FileEntry>> ScanAsync(
        IEnumerable<string> roots,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var result = new List<FileEntry>();
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true, // Ключевой параметр для пропуска папок без доступа
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.System | FileAttributes.Hidden // Пропускаем скрытые и системные
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;

                // Перечисляем все файлы рекурсивно. Благодаря IgnoreInaccessible
                // ошибки доступа к подпапкам не прерывают процесс.
                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(root, "*", options);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    progress?.Report($"Пропущена папка {root}: {ex.Message}");
                    continue;
                }

                foreach (var filePath in files)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var entry = CreateEntry(filePath);
                        if (entry != null)
                            result.Add(entry);
                    }
                    catch
                    {
                        // Пропускаем отдельные проблемные файлы, не прерывая сканирование
                    }
                }

                progress?.Report($"Завершено сканирование: {root}");
            }

            return result;
        }, ct);
    }

    private FileEntry? CreateEntry(string path)
    {
        var fi = new FileInfo(path);

        string ext = fi.Extension.ToLowerInvariant();
        string? preview = null;

        // Читаем только первые 4 КБ текстовых файлов и файлов размером менее 100 МБ
        if (TextExtensions.Contains(ext) && fi.Length > 0 && fi.Length < 100 * 1024 * 1024)
        {
            try
            {
                using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096);
                char[] buffer = new char[4096];
                int read = reader.Read(buffer, 0, buffer.Length);
                preview = new string(buffer, 0, read);
            }
            catch
            {
                // Если прочитать не удалось, превью остаётся null
            }
        }

        return new FileEntry
        {
            FullPath = path,
            Name = fi.Name,
            Extension = ext,
            SizeBytes = fi.Length,
            Created = fi.CreationTime,
            Modified = fi.LastWriteTime,
            ContentPreview = preview,
            Category = GetCategory(ext)
        };
    }

    private static string GetCategory(string ext)
    {
        if (ImageExtensions.Contains(ext)) return "Image";
        if (AudioExtensions.Contains(ext)) return "Audio";
        if (VideoExtensions.Contains(ext)) return "Video";
        if (TextExtensions.Contains(ext)) return "Text";
        if (ArchiveExtensions.Contains(ext)) return "Archive";
        return "Other";
    }
}
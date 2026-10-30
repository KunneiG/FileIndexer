using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FileIndexer.Models;

namespace FileIndexer.Services;

public static class PreviewService
{
    /// <summary>
    /// Возвращает UIElement для отображения превью выбранного файла.
    /// </summary>
    public static UIElement? GetPreview(FileEntry entry)
    {
        if (entry == null) return null;

        try
        {
            switch (entry.Category)
            {
                case "Image":
                    return CreateImagePreview(entry.FullPath);

                case "Text":
                    return CreateTextPreview(entry);

                case "Audio":
                case "Video":
                    return CreateMediaPreview(entry.FullPath, entry.Category == "Video");

                default:
                    return CreateDefaultPreview(entry);
            }
        }
        catch
        {
            return CreateDefaultPreview(entry);
        }
    }

    private static UIElement CreateImagePreview(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.DecodePixelWidth = 400; // Уменьшаем потребление памяти
        bitmap.EndInit();
        bitmap.Freeze(); // Для потокобезопасного использования

        return new Image
        {
            Source = bitmap,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(5)
        };
    }

    private static UIElement CreateTextPreview(FileEntry entry)
    {
        string text = entry.ContentPreview ?? "Не удалось прочитать содержимое файла.";

        return new TextBox
        {
            Text = text,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(5)
        };
    }

    private static UIElement CreateMediaPreview(string path, bool isVideo)
    {
        var mediaElement = new MediaElement
        {
            Source = new Uri(path),
            LoadedBehavior = MediaState.Manual,
            UnloadedBehavior = MediaState.Manual,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(5)
        };

        // Автоматически запускаем воспроизведение при появлении превью
        mediaElement.Loaded += (_, _) => mediaElement.Play();
        mediaElement.Unloaded += (_, _) => mediaElement.Stop();

        return mediaElement;
    }

    private static UIElement CreateDefaultPreview(FileEntry entry)
    {
        var panel = new StackPanel { Margin = new Thickness(10) };

        panel.Children.Add(new TextBlock
        {
            Text = "📄",
            FontSize = 48,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = entry.Name,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 5)
        });

        panel.Children.Add(new TextBlock { Text = $"Размер: {entry.SizeHuman}" });
        panel.Children.Add(new TextBlock { Text = $"Создан: {entry.Created:g}" });
        panel.Children.Add(new TextBlock { Text = $"Изменён: {entry.Modified:g}" });
        panel.Children.Add(new TextBlock
        {
            Text = entry.FullPath,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Gray,
            FontSize = 11,
            Margin = new Thickness(0, 5, 0, 0)
        });

        return panel;
    }
}
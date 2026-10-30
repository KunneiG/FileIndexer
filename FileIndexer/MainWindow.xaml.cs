using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FileIndexer.Models;
using FileIndexer.Services;
using Microsoft.Win32;

namespace FileIndexer;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _folders = new();
    private readonly FileScanner _scanner = new();
    private readonly SearchIndex _index = new();
    private CancellationTokenSource? _cts;
    private DispatcherTimer? _searchDebounceTimer;

    public MainWindow()
    {
        InitializeComponent();
        FoldersListBox.ItemsSource = _folders;

        // Загружаем кэш при старте
        Loaded += (_, _) => LoadCache();
    }

    private void LoadCache()
    {
        var cached = IndexCache.Load();
        if (cached.Any())
        {
            _index.Build(cached);
            ResultsDataGrid.ItemsSource = cached;
            StatusTextBlock.Text = $"Загружено из кэша: {cached.Count} файлов";
        }
    }

    // ---------- Выбор папок ----------
    private void AddFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // OpenFolderDialog доступен в .NET 8 нативно
        var dialog = new OpenFolderDialog
        {
            Title = "Выберите папку для индексации",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            if (!_folders.Contains(dialog.FolderName))
                _folders.Add(dialog.FolderName);
        }
    }

    // ---------- Индексация ----------
    private async void IndexButton_Click(object sender, RoutedEventArgs e)
    {
        if (_folders.Count == 0)
        {
            MessageBox.Show("Сначала добавьте хотя бы одну папку.", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _cts = new CancellationTokenSource();
        IndexButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        IndexProgressBar.IsIndeterminate = true;

        var progress = new Progress<string>(path => CurrentPathTextBlock.Text = path);

        try
        {
            var entries = await _scanner.ScanAsync(_folders, progress, _cts.Token);
            _index.Build(entries);
            ResultsDataGrid.ItemsSource = entries;
            IndexCache.Save(entries);

            StatusTextBlock.Text = $"Проиндексировано: {entries.Count} файлов";
        }
        catch (OperationCanceledException)
        {
            StatusTextBlock.Text = "Индексация отменена.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка индексации: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IndexButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
            IndexProgressBar.IsIndeterminate = false;
            CurrentPathTextBlock.Text = string.Empty;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    // ---------- Поиск с задержкой (debounce) ----------
    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Перезапускаем таймер при каждом изменении текста
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            RunSearch();
        };
        _searchDebounceTimer.Start();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        RunSearch();
    }

    private void RunSearch()
    {
        var filter = new SearchFilter
        {
            NameContains = SearchTextBox.Text,
            Extension = ExtensionTextBox.Text,
            Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
        };

        if (long.TryParse(MinSizeTextBox.Text, out long minSize))
            filter.MinSize = minSize;

        var results = _index.Search(filter).ToList();
        ResultsDataGrid.ItemsSource = results;
        StatusTextBlock.Text = $"Найдено: {results.Count}";
    }

    // ---------- Превью ----------
    private void ResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PreviewScrollViewer.Content = null;

        if (ResultsDataGrid.SelectedItem is FileEntry entry)
        {
            var preview = PreviewService.GetPreview(entry);
            PreviewScrollViewer.Content = preview;
        }
    }
}
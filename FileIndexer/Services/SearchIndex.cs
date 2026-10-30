using FileIndexer.Models;

namespace FileIndexer.Services;

public class SearchIndex
{
    private List<FileEntry> _all = new();

    /// <summary>
    /// Заполняет индекс новыми данными.
    /// </summary>
    public void Build(IEnumerable<FileEntry> entries)
    {
        _all = entries.ToList();
    }

    /// <summary>
    /// Выполняет поиск по фильтру.
    /// </summary>
    public IEnumerable<FileEntry> Search(SearchFilter filter)
    {
        IEnumerable<FileEntry> query = _all;

        if (!string.IsNullOrWhiteSpace(filter.NameContains))
            query = query.Where(e => e.Name.Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.ContentContains))
            query = query.Where(e => e.ContentPreview != null &&
                                     e.ContentPreview.Contains(filter.ContentContains, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.Extension))
        {
            string ext = filter.Extension.TrimStart('.').ToLowerInvariant();
            query = query.Where(e => e.Extension.TrimStart('.').Equals(ext, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.MinSize.HasValue)
            query = query.Where(e => e.SizeBytes >= filter.MinSize.Value);

        if (filter.MaxSize.HasValue)
            query = query.Where(e => e.SizeBytes <= filter.MaxSize.Value);

        if (filter.DateFrom.HasValue)
            query = query.Where(e => e.Modified >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(e => e.Modified <= filter.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.Category) && filter.Category != "All")
            query = query.Where(e => e.Category == filter.Category);

        return query.OrderBy(e => e.Name);
    }

    public int Count => _all.Count;
}
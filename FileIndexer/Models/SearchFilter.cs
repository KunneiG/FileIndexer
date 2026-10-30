namespace FileIndexer.Models;

public class SearchFilter
{
    public string? NameContains { get; set; }
    public string? ContentContains { get; set; }
    public string? Extension { get; set; }
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Category { get; set; } // "All", "Image", "Audio", "Video", "Text", "Archive", "Other"
}
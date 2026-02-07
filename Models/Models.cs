namespace FileManager.Models;

public class FileManagerViewModel
{
    public string CurrentPath { get; set; } = "";
    public string? ParentPath { get; set; }
    public List<FileItem> Items { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class FileItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string? Extension { get; set; }
    
    // Markdown metadata (populated only for .md files)
    public string? Title { get; set; }
    public string? Id { get; set; }
    public string? Author { get; set; }
    public List<string> Tags { get; set; } = new();

    public string FormattedSize
    {
        get
        {
            if (IsDirectory) return "-";
            string[] sizes = ["B", "KB", "MB", "GB"];
            double len = Size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public string Icon => IsDirectory ? "📁" : Extension?.ToLower() switch
    {
        ".md" => "📝",
        ".pdf" => "📄",
        ".jpg" or ".jpeg" or ".png" or ".gif" => "🖼️",
        ".zip" or ".rar" => "📦",
        ".json" => "📋",
        _ => "📄"
    };
}

public class MarkdownViewModel
{
    public string Title { get; set; } = "";
    public string? Id { get; set; }
    public string? Author { get; set; }
    public string? Date { get; set; }
    public List<string> Tags { get; set; } = new();
    public string HtmlContent { get; set; } = "";
    public string FilePath { get; set; } = "";
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class SearchResult
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? Title { get; set; }
    public string? Id { get; set; }
    public List<string> Tags { get; set; } = new();
    public string MatchType { get; set; } = "";
    public string? Snippet { get; set; }
    public DateTime LastModified { get; set; }
}

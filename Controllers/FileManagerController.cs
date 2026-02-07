using Microsoft.AspNetCore.Mvc;
using FileManager.Models;
using FileManager.Services;
using System.Text.RegularExpressions;

namespace FileManager.Controllers;

public class FileManagerController : Controller
{
    private readonly string _rootPath;
    private readonly IMarkdownService _markdownService;

    public FileManagerController(FileManagerConfig config, IMarkdownService markdownService)
    {
        _rootPath = config.DataPath;
        _markdownService = markdownService;
    }

    // GET: /FileManager or /FileManager/Index/path/to/folder
    public IActionResult Index(string? path = null)
    {
        try
        {
            var fullPath = GetFullPath(path ?? "");
            
            if (!IsPathSafe(fullPath))
                return NotFound();

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            var model = new FileManagerViewModel
            {
                CurrentPath = path ?? "",
                ParentPath = GetParentPath(path),
                Items = GetItems(fullPath, path ?? "")
            };

            return View(model);
        }
        catch (Exception ex)
        {
            return View(new FileManagerViewModel { ErrorMessage = ex.Message });
        }
    }

    // GET: /FileManager/View/path/to/file.md
    public IActionResult View(string path)
    {
        try
        {
            var fullPath = GetFullPath(path);
            
            if (!IsPathSafe(fullPath) || !System.IO.File.Exists(fullPath))
                return NotFound();

            var model = _markdownService.LoadMarkdownFile(fullPath);
            model.FilePath = path;
            
            return View("Markdown", model);
        }
        catch (Exception ex)
        {
            return Content($"Error: {ex.Message}");
        }
    }

    // GET: /FileManager/Raw/path/to/file.md
    public IActionResult Raw(string path)
    {
        var fullPath = GetFullPath(path);
        
        if (!IsPathSafe(fullPath) || !System.IO.File.Exists(fullPath))
            return NotFound();

        var content = System.IO.File.ReadAllText(fullPath);
        return Content(content, "text/plain");
    }

    // GET: /FileManager/Download/path/to/file
    public IActionResult Download(string path)
    {
        var fullPath = GetFullPath(path);
        
        if (!IsPathSafe(fullPath) || !System.IO.File.Exists(fullPath))
            return NotFound();

        var bytes = System.IO.File.ReadAllBytes(fullPath);
        return File(bytes, "application/octet-stream", Path.GetFileName(fullPath));
    }

    // POST: /FileManager/Upload
    [HttpPost]
    public async Task<IActionResult> Upload(string path, List<IFormFile> files)
    {
        try
        {
            var fullPath = GetFullPath(path);
            
            if (!IsPathSafe(fullPath))
                return Json(new { success = false, message = "Invalid path" });

            Directory.CreateDirectory(fullPath);

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(fullPath, file.FileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
            }

            return Json(new { success = true, message = $"{files.Count} file(s) uploaded" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /FileManager/CreateFolder
    [HttpPost]
    public IActionResult CreateFolder(string path, string name)
    {
        try
        {
            var fullPath = Path.Combine(GetFullPath(path), name);
            
            if (!IsPathSafe(fullPath))
                return Json(new { success = false, message = "Invalid path" });

            Directory.CreateDirectory(fullPath);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /FileManager/Delete
    [HttpPost]
    public IActionResult Delete(string path, bool isDirectory)
    {
        try
        {
            var fullPath = GetFullPath(path);
            
            if (!IsPathSafe(fullPath))
                return Json(new { success = false, message = "Invalid path" });

            if (isDirectory)
            {
                if (Directory.EnumerateFileSystemEntries(fullPath).Any())
                    return Json(new { success = false, message = "Directory not empty" });
                Directory.Delete(fullPath);
            }
            else
            {
                System.IO.File.Delete(fullPath);
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /FileManager/Rename
    [HttpPost]
    public IActionResult Rename(string path, string newName)
    {
        try
        {
            var fullPath = GetFullPath(path);
            var newPath = Path.Combine(Path.GetDirectoryName(fullPath)!, newName);
            
            if (!IsPathSafe(fullPath) || !IsPathSafe(newPath))
                return Json(new { success = false, message = "Invalid path" });

            if (Directory.Exists(fullPath))
                Directory.Move(fullPath, newPath);
            else
                System.IO.File.Move(fullPath, newPath);

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // GET: /FileManager/Search (View)
    [HttpGet("FileManager/Search")]
    public IActionResult SearchView(string q)
    {
        return View("Search");
    }

    // GET: /FileManager/SearchApi?q=query&path=folder&recursive=true
    [HttpGet("FileManager/SearchApi")]
    public IActionResult Search(string q, string? path = null, bool recursive = true)
    {
        try
        {
            var fullPath = GetFullPath(path ?? "");
            
            if (!IsPathSafe(fullPath) || string.IsNullOrWhiteSpace(q))
                return Json(new { success = true, results = Array.Empty<object>() });

            var results = new List<SearchResult>();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(fullPath, "*.md", searchOption);

            foreach (var file in files)
            {
                var content = System.IO.File.ReadAllText(file);
                var (metadata, markdown) = _markdownService.ParseMarkdown(content);
                var tags = _markdownService.ExtractTags(content);
                
                string? matchType = null;
                string? snippet = null;

                // Check ID
                if (metadata.TryGetValue("id", out var id) && 
                    id.Contains(q, StringComparison.OrdinalIgnoreCase))
                    matchType = "ID";
                
                // Check Title
                else if (metadata.TryGetValue("title", out var title) && 
                    title.Contains(q, StringComparison.OrdinalIgnoreCase))
                    matchType = "Title";
                
                // Check Tags
                else if (tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)))
                    matchType = "Tag";
                
                // Check Content
                else if (markdown.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    matchType = "Content";
                    var idx = markdown.IndexOf(q, StringComparison.OrdinalIgnoreCase);
                    var start = Math.Max(0, idx - 50);
                    var end = Math.Min(markdown.Length, idx + q.Length + 50);
                    snippet = (start > 0 ? "..." : "") + 
                              Regex.Replace(markdown[start..end], @"\s+", " ").Trim() + 
                              (end < markdown.Length ? "..." : "");
                }

                if (matchType != null)
                {
                    var relativePath = Path.GetRelativePath(_rootPath, file).Replace('\\', '/');
                    results.Add(new SearchResult
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = relativePath,
                        Title = metadata.GetValueOrDefault("title", Path.GetFileNameWithoutExtension(file)),
                        Id = metadata.GetValueOrDefault("id"),
                        Tags = tags,
                        MatchType = matchType,
                        Snippet = snippet,
                        LastModified = System.IO.File.GetLastWriteTime(file)
                    });
                }
            }

            return Json(new { success = true, results });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // API: /FileManager/Api/List?path=folder
    [HttpGet]
    public IActionResult ApiList(string? path = null)
    {
        var fullPath = GetFullPath(path ?? "");
        if (!IsPathSafe(fullPath) || !Directory.Exists(fullPath))
            return Json(new { success = false, message = "Invalid path" });

        var items = GetItems(fullPath, path ?? "");
        return Json(new { success = true, items });
    }

    // API: /FileManager/Api/Read?path=file.md
    [HttpGet]
    public IActionResult ApiRead(string path)
    {
        var fullPath = GetFullPath(path);
        if (!IsPathSafe(fullPath) || !System.IO.File.Exists(fullPath))
            return Json(new { success = false, message = "File not found" });

        var content = System.IO.File.ReadAllText(fullPath);
        var (metadata, markdown) = _markdownService.ParseMarkdown(content);
        
        return Json(new { 
            success = true, 
            metadata, 
            content = markdown,
            tags = _markdownService.ExtractTags(content)
        });
    }

    // API: /FileManager/Api/Write (for LLM integration)
    [HttpPost]
    public IActionResult ApiWrite(string path, [FromBody] WriteRequest request)
    {
        try
        {
            var fullPath = GetFullPath(path);
            if (!IsPathSafe(fullPath))
                return Json(new { success = false, message = "Invalid path" });

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            System.IO.File.WriteAllText(fullPath, request.Content);
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #region Helpers

    private string GetFullPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return _rootPath;
        return Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private bool IsPathSafe(string fullPath)
    {
        var normalized = Path.GetFullPath(fullPath);
        var normalizedRoot = Path.GetFullPath(_rootPath);
        return normalized.StartsWith(normalizedRoot);
    }

    private string? GetParentPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var parent = Path.GetDirectoryName(path);
        return parent?.Replace('\\', '/');
    }

    private List<FileItem> GetItems(string fullPath, string relativePath)
    {
        var items = new List<FileItem>();

        // Directories
        foreach (var dir in Directory.GetDirectories(fullPath))
        {
            var info = new DirectoryInfo(dir);
            items.Add(new FileItem
            {
                Name = info.Name,
                Path = string.IsNullOrEmpty(relativePath) 
                    ? info.Name 
                    : $"{relativePath}/{info.Name}",
                IsDirectory = true,
                LastModified = info.LastWriteTime
            });
        }

        // Files
        foreach (var file in Directory.GetFiles(fullPath))
        {
            var info = new FileInfo(file);
            var item = new FileItem
            {
                Name = info.Name,
                Path = string.IsNullOrEmpty(relativePath) 
                    ? info.Name 
                    : $"{relativePath}/{info.Name}",
                IsDirectory = false,
                Size = info.Length,
                LastModified = info.LastWriteTime,
                Extension = info.Extension
            };

            // Load markdown metadata
            if (info.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var content = System.IO.File.ReadAllText(file);
                    var (metadata, _) = _markdownService.ParseMarkdown(content);
                    item.Title = metadata.GetValueOrDefault("title");
                    item.Id = metadata.GetValueOrDefault("id");
                    item.Author = metadata.GetValueOrDefault("author");
                    item.Tags = _markdownService.ExtractTags(content);
                }
                catch { /* ignore parsing errors */ }
            }

            items.Add(item);
        }

        return items.OrderByDescending(x => x.IsDirectory).ThenBy(x => x.Name).ToList();
    }

    #endregion
}

public class WriteRequest
{
    public string Content { get; set; } = "";
}

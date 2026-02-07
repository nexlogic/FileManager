using System.Text.RegularExpressions;
using Markdig;
using FileManager.Models;

namespace FileManager.Services;

public interface IMarkdownService
{
    (Dictionary<string, string> Metadata, string Content) ParseMarkdown(string content);
    string ToHtml(string markdown);
    List<string> ExtractTags(string content);
    MarkdownViewModel LoadMarkdownFile(string filePath);
}

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public (Dictionary<string, string> Metadata, string Content) ParseMarkdown(string content)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var markdown = content;

        // Parse YAML front matter
        var match = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n?", RegexOptions.Singleline);
        if (match.Success)
        {
            var yaml = match.Groups[1].Value;
            markdown = content[match.Length..];

            // Simple YAML parser for key: value pairs
            foreach (var line in yaml.Split('\n'))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = line[..colonIndex].Trim();
                    var value = line[(colonIndex + 1)..].Trim().Trim('"', '\'');
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        metadata[key] = value;
                    }
                }
            }
        }

        return (metadata, markdown);
    }

    public string ToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown, _pipeline);
    }

    public List<string> ExtractTags(string content)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Method 1: YAML front matter tags: [tag1, tag2]
        var yamlMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---", RegexOptions.Singleline);
        if (yamlMatch.Success)
        {
            var yaml = yamlMatch.Groups[1].Value;
            
            // tags: [tag1, tag2]
            var arrayMatch = Regex.Match(yaml, @"tags:\s*\[(.*?)\]", RegexOptions.IgnoreCase);
            if (arrayMatch.Success)
            {
                foreach (var tag in arrayMatch.Groups[1].Value.Split(','))
                {
                    var t = tag.Trim().Trim('"', '\'');
                    if (!string.IsNullOrEmpty(t)) tags.Add(t);
                }
            }
        }

        // Method 2: Hashtags #tag
        foreach (Match m in Regex.Matches(content, @"(?<!\w)#(\w+)"))
        {
            var tag = m.Groups[1].Value;
            if (tag.Length > 1) tags.Add(tag);
        }

        // Method 3: Obsidian [[links]]
        foreach (Match m in Regex.Matches(content, @"\[\[([^\]]+)\]\]"))
        {
            tags.Add(m.Groups[1].Value.Trim());
        }

        return tags.ToList();
    }

    public MarkdownViewModel LoadMarkdownFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var (metadata, markdown) = ParseMarkdown(content);
        var tags = ExtractTags(content);

        return new MarkdownViewModel
        {
            Title = metadata.GetValueOrDefault("title", Path.GetFileNameWithoutExtension(filePath)),
            Id = metadata.GetValueOrDefault("id"),
            Author = metadata.GetValueOrDefault("author"),
            Date = metadata.GetValueOrDefault("date"),
            Tags = tags,
            HtmlContent = ToHtml(markdown),
            FilePath = filePath,
            Metadata = metadata
        };
    }
}

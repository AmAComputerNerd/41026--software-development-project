using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RagServer.Configuration;
using RagServer.Models;

namespace RagServer.Services;

public sealed partial class ProjectCorpus
{
    private static readonly FrozenSet<string> SharedSourceIds = new[]
    {
        "AGENTS.md",
        "docs/architecture/data-flows.md",
        "docs/knowledge-base/course_policies.md"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> StopWords = new[]
    {
        "about", "after", "also", "and", "are", "can", "does", "for", "from",
        "how", "into", "its", "our", "the", "their", "this", "through", "what",
        "when", "where", "which", "with", "would", "your"
    }.ToFrozenSet(StringComparer.Ordinal);

    private readonly List<CorpusChunk> _chunks;
    private readonly FrozenDictionary<string, int> _documentFrequency;
    private readonly RagOptions _options;

    public ProjectCorpus(IOptions<RagOptions> options)
    {
        _options = options.Value;
        _chunks = LoadChunks(_options.CorpusPath);
        if (_chunks.Count == 0)
        {
            throw new InvalidOperationException(
                $"No Markdown corpus sources were found in '{_options.CorpusPath}'.");
        }

        _documentFrequency = _chunks
            .SelectMany(chunk => chunk.Terms.Keys.Distinct())
            .GroupBy(term => term, StringComparer.Ordinal)
            .ToFrozenDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    public int SourceCount => _chunks.Select(chunk => chunk.SourceId).Distinct().Count();

    public int ChunkCount => _chunks.Count;

    public int GetChunkCount(string scope) => GetEligibleChunks(scope).Count();

    public IReadOnlyList<RetrievedChunk> Retrieve(string question, string scope = "all")
    {
        var queryTerms = Tokenize(question).Distinct().ToArray();
        if (queryTerms.Length == 0)
        {
            return [];
        }

        var totalQueryWeight = queryTerms.Sum(InverseDocumentFrequency);
        var matches = GetEligibleChunks(scope)
            .Select(chunk =>
            {
                var matchedWeight = queryTerms
                    .Where(chunk.Terms.ContainsKey)
                    .Sum(term => InverseDocumentFrequency(term) *
                        (1 + Math.Log(chunk.Terms[term])));
                var score = totalQueryWeight == 0
                    ? 0
                    : Math.Min(1, matchedWeight / totalQueryWeight);
                return new RetrievedChunk(
                    chunk.SourceId,
                    chunk.Title,
                    chunk.Heading,
                    chunk.Content,
                    Math.Round(score, 3));
            })
            .Where(chunk => chunk.Score >= _options.MinimumRelevanceScore)
            .OrderByDescending(chunk => chunk.Score)
            .ThenBy(chunk => chunk.SourceId, StringComparer.Ordinal)
            .Take(_options.MaximumRetrievedChunks)
            .ToList();

        return matches;
    }

    private IEnumerable<CorpusChunk> GetEligibleChunks(string scope) =>
        _chunks.Where(chunk =>
            scope.Equals("all", StringComparison.OrdinalIgnoreCase) ||
            chunk.SourceId.StartsWith($"{scope}/", StringComparison.OrdinalIgnoreCase) ||
            chunk.SourceId.StartsWith("shared/", StringComparison.OrdinalIgnoreCase) ||
            SharedSourceIds.Contains(chunk.SourceId));

    private double InverseDocumentFrequency(string term)
    {
        var frequency = _documentFrequency.GetValueOrDefault(term, 0);
        return Math.Log((_chunks.Count + 1d) / (frequency + 1d)) + 1d;
    }

    private static List<CorpusChunk> LoadChunks(string corpusPath)
    {
        var root = Path.GetFullPath(corpusPath);
        var chunks = new List<CorpusChunk>();
        foreach (var path in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var sourceId = Path.GetRelativePath(root, path).Replace('\\', '/');
            var markdown = File.ReadAllText(path);
            chunks.AddRange(SplitMarkdown(sourceId, markdown));
        }

        return chunks;
    }

    private static IEnumerable<CorpusChunk> SplitMarkdown(string sourceId, string markdown)
    {
        var title = Path.GetFileNameWithoutExtension(sourceId);
        var heading = title;
        var section = new List<string>();

        foreach (var line in markdown.ReplaceLineEndings("\n").Split('\n'))
        {
            var headingMatch = HeadingPattern().Match(line);
            if (headingMatch.Success)
            {
                foreach (var chunk in BuildSectionChunks(sourceId, title, heading, section))
                {
                    yield return chunk;
                }

                heading = headingMatch.Groups["heading"].Value.Trim();
                if (headingMatch.Groups["level"].Value.Length == 1)
                {
                    title = heading;
                }

                section.Clear();
                continue;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                section.Add(line.Trim());
            }
        }

        foreach (var chunk in BuildSectionChunks(sourceId, title, heading, section))
        {
            yield return chunk;
        }
    }

    private static IEnumerable<CorpusChunk> BuildSectionChunks(
        string sourceId,
        string title,
        string heading,
        List<string> lines)
    {
        const int maximumCharacters = 1800;
        var content = string.Join('\n', lines);
        for (var offset = 0; offset < content.Length; offset += maximumCharacters)
        {
            var length = Math.Min(maximumCharacters, content.Length - offset);
            var text = content.Substring(offset, length).Trim();
            if (text.Length < 40)
            {
                continue;
            }

            var terms = Tokenize($"{heading} {text}")
                .GroupBy(term => term, StringComparer.Ordinal)
                .ToFrozenDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            yield return new CorpusChunk(sourceId, title, heading, text, terms);
        }
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return TokenPattern()
            .Matches(text.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(term => term.Length >= 3 && !StopWords.Contains(term));
    }

    [GeneratedRegex(@"^(?<level>#{1,6})\s+(?<heading>.+?)\s*$")]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(@"[a-z0-9]+(?:-[a-z0-9]+)*")]
    private static partial Regex TokenPattern();

    private sealed record CorpusChunk(
        string SourceId,
        string Title,
        string Heading,
        string Content,
        FrozenDictionary<string, int> Terms);
}

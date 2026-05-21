namespace A2V10.Xaml.Reflection.Documentation;

public class XamlDocumentationProvider
{
    private const string DocumentationRootDirectoryName = "docs";
    private const string XamlDocumentationRelativePath = "xaml-html\\xaml";

    private readonly Lazy<IReadOnlyDictionary<string, XamlTagDocumentation>> _documentation;

    public XamlDocumentationProvider(string? baseDirectory = null)
    {
        BaseDirectory = baseDirectory ?? AppContext.BaseDirectory;
        _documentation = new Lazy<IReadOnlyDictionary<string, XamlTagDocumentation>>(LoadDocumentation, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string BaseDirectory { get; }

    public virtual bool TryGetTagDocumentation(string tagName, out XamlTagDocumentation? documentation)
        => _documentation.Value.TryGetValue(tagName, out documentation);

    private IReadOnlyDictionary<string, XamlTagDocumentation> LoadDocumentation()
    {
        var documentationRoot = FindDocumentationRoot();
        if (documentationRoot is null || !Directory.Exists(documentationRoot))
        {
            return new Dictionary<string, XamlTagDocumentation>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, XamlTagDocumentation>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in Directory.EnumerateFiles(documentationRoot, "*.html", SearchOption.AllDirectories))
        {
            var documentation = ParseFile(filePath);
            if (documentation is null || string.IsNullOrWhiteSpace(documentation.Name))
            {
                continue;
            }

            result[documentation.Name] = documentation;
        }

        return result;
    }

    private string? FindDocumentationRoot()
    {
        var current = BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            var candidate = Path.Combine(current, DocumentationRootDirectoryName, XamlDocumentationRelativePath);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = Path.GetDirectoryName(current);
        }

        return null;
    }

    public static XamlTagDocumentation? ParseContent(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var titleMatch = System.Text.RegularExpressions.Regex.Match(
            html,
            @"<h1>\s*Елемент\s+(?<name>[A-Za-z0-9_]+)\s*</h1>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        if (!titleMatch.Success)
        {
            return null;
        }

        var name = titleMatch.Groups["name"].Value.Trim();
        var description = ExtractTagDescription(html);
        var fullDocumentation = ExtractTagFullDocumentation(html);
        var (attributes, attributeFullDocumentation) = ExtractAttributeDocumentation(html);
        return new XamlTagDocumentation(name, description, fullDocumentation, attributes, attributeFullDocumentation);
    }

    private static XamlTagDocumentation? ParseFile(string filePath)
    {
        var html = File.ReadAllText(filePath);
        return ParseContent(html);
    }

    private static string? ExtractTagDescription(string html)
    {
        var titleMatch = System.Text.RegularExpressions.Regex.Match(
            html,
            @"</div>\s*<p>(?<description>.*?)</p>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        return titleMatch.Success
            ? NormalizeHtmlText(titleMatch.Groups["description"].Value)
            : null;
    }

    private static string? ExtractTagFullDocumentation(string html)
    {
        var contentMatch = System.Text.RegularExpressions.Regex.Match(
            html,
            @"<div\s+class=""title"">.*?</div>(?<content>.*?)(?:<h3>Приклад</h3>|</div>\s*$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        return contentMatch.Success
            ? NormalizeHtmlDocumentation(contentMatch.Groups["content"].Value)
            : ExtractTagDescription(html);
    }

    private static (IReadOnlyDictionary<string, string> descriptions, IReadOnlyDictionary<string, string> fullDocumentation) ExtractAttributeDocumentation(string html)
    {
        var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var fullDocumentation = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tableMatch = System.Text.RegularExpressions.Regex.Match(
            html,
            "<table\\s+class=\"table-props\".*?<tbody>(?<body>.*?)</tbody>\\s*</table>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        if (!tableMatch.Success)
        {
            return (descriptions, fullDocumentation);
        }

        var rowMatches = System.Text.RegularExpressions.Regex.Matches(
            tableMatch.Groups["body"].Value,
            @"<tr(?<attrs>[^>]*)>(?<row>.*?)</tr>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        foreach (System.Text.RegularExpressions.Match rowMatch in rowMatches)
        {
            var cellMatches = System.Text.RegularExpressions.Regex.Matches(
                rowMatch.Groups["row"].Value,
                @"<td(?<attrs>[^>]*)>(?<value>.*?)</td>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

            if (cellMatches.Count < 3)
            {
                continue;
            }

            var attributeName = NormalizeHtmlText(cellMatches[0].Groups["value"].Value);
            var descriptionText = NormalizeHtmlText(cellMatches[2].Groups["value"].Value);
            var fullText = NormalizeHtmlDocumentation(cellMatches[2].Groups["value"].Value);
            var description = ExtractFirstSentence(descriptionText);
            if (string.IsNullOrWhiteSpace(attributeName) || string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            descriptions[attributeName] = description;
            fullDocumentation[attributeName] = fullText;
        }

        return (descriptions, fullDocumentation);
    }

    private static string ExtractFirstSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var sentenceEnd = text.IndexOfAny(['.', '!', '?']);
        return sentenceEnd >= 0
            ? text[..(sentenceEnd + 1)].Trim()
            : text;
    }

    private static string NormalizeHtmlText(string value)
    {
        var withoutScripts = System.Text.RegularExpressions.Regex.Replace(
            value,
            @"<script\b[^>]*>.*?</script>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        var withLineBreaks = System.Text.RegularExpressions.Regex.Replace(
            withoutScripts,
            @"<(br|/p|/li|/tr|/div|/ul|/ol|/table|/h\d)\b[^>]*>",
            " ",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        var withoutTags = System.Text.RegularExpressions.Regex.Replace(
            withLineBreaks,
            @"<.*?>",
            " ",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        return System.Text.RegularExpressions.Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    private static string NormalizeHtmlDocumentation(string value)
    {
        var withoutScripts = System.Text.RegularExpressions.Regex.Replace(
            value,
            @"<script\b[^>]*>.*?</script>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        var withStructure = System.Text.RegularExpressions.Regex.Replace(
            withoutScripts,
            @"<br\s*/?>",
            "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        withStructure = System.Text.RegularExpressions.Regex.Replace(
            withStructure,
            @"<li\b[^>]*>",
            "\n- ",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        withStructure = System.Text.RegularExpressions.Regex.Replace(
            withStructure,
            @"</(p|div|ul|ol|table|h\d)\s*>",
            "\n\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        withStructure = System.Text.RegularExpressions.Regex.Replace(
            withStructure,
            @"<tr\b[^>]*>",
            "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        withStructure = System.Text.RegularExpressions.Regex.Replace(
            withStructure,
            @"</(tr|li)\s*>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        withStructure = System.Text.RegularExpressions.Regex.Replace(
            withStructure,
            @"</(td|th)\s*>",
            " | ",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        var withoutTags = System.Text.RegularExpressions.Regex.Replace(
            withStructure,
            @"<.*?>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags).Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = decoded.Split('\n');
        var result = new List<string>(lines.Length);
        var previousWasEmpty = false;

        foreach (var line in lines)
        {
            var normalizedLine = System.Text.RegularExpressions.Regex.Replace(line, @"\s+", " ").Trim();
            normalizedLine = normalizedLine.TrimEnd('|').TrimEnd();

            if (normalizedLine.Length == 0)
            {
                if (!previousWasEmpty && result.Count > 0)
                {
                    result.Add(string.Empty);
                }

                previousWasEmpty = true;
                continue;
            }

            result.Add(normalizedLine);
            previousWasEmpty = false;
        }

        return string.Join("\n", result).Trim();
    }
}

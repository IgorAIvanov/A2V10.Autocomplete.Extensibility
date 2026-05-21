using System.Text.RegularExpressions;
using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Documents;
using A2V10.Xaml.Core.Models;
using A2V10.Xaml.LanguageServer.Protocol;

namespace A2V10.Xaml.LanguageServer.Application;

public sealed class HoverRequestHandler
{
    private static readonly Regex AttributeRegex = new(@"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IMetadataProvider _metadataProvider;

    public HoverRequestHandler(IMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    public async Task<HoverResponse?> HandleAsync(HoverRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var text = request.Text;
        if (text is null)
        {
            if (!File.Exists(request.FilePath))
            {
                return null;
            }

            text = await File.ReadAllTextAsync(request.FilePath, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(text) || request.Position < 0 || request.Position > text.Length)
        {
            return null;
        }

        var document = new XamlDocumentContext(new Uri(request.FilePath), text, request.ProjectPath);
        var metadata = await _metadataProvider.GetMetadataAsync(document, cancellationToken);
        var symbol = FindSymbol(text, request.Position);
        if (symbol is null)
        {
            return null;
        }

        return symbol.Kind switch
        {
            HoverSymbolKind.Tag => CreateTagResponse(metadata, symbol),
            HoverSymbolKind.Attribute => CreateAttributeResponse(metadata, symbol),
            _ => null
        };
    }

    private static HoverResponse? CreateTagResponse(MetadataRegistry metadata, HoverSymbol symbol)
    {
        var tag = metadata.Tags.FirstOrDefault(tag => string.Equals(tag.Name, symbol.Name, StringComparison.OrdinalIgnoreCase));
        var documentation = tag?.FullDocumentation ?? tag?.Description;
        if (tag is null || string.IsNullOrWhiteSpace(documentation))
        {
            return null;
        }

        return new HoverResponse(documentation, symbol.SourceText, symbol.Start, symbol.End);
    }

    private static HoverResponse? CreateAttributeResponse(MetadataRegistry metadata, HoverSymbol symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol.TagName))
        {
            return null;
        }

        var tag = metadata.Tags.FirstOrDefault(tag => string.Equals(tag.Name, symbol.TagName, StringComparison.OrdinalIgnoreCase));
        var attribute = tag?.Attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, symbol.Name, StringComparison.OrdinalIgnoreCase));
        var documentation = attribute?.FullDocumentation ?? attribute?.Description;
        if (attribute is null || string.IsNullOrWhiteSpace(documentation))
        {
            return null;
        }

        return new HoverResponse(documentation, symbol.SourceText, symbol.Start, symbol.End);
    }

    private static HoverSymbol? FindSymbol(string text, int position)
    {
        var normalizedPosition = Math.Clamp(position, 0, text.Length);
        var beforeCursor = text[..normalizedPosition];
        var tagStart = beforeCursor.LastIndexOf('<');
        if (tagStart < 0)
        {
            return null;
        }

        var tagEnd = text.IndexOf('>', tagStart);
        if (tagEnd >= 0 && normalizedPosition > tagEnd)
        {
            return null;
        }

        var fragmentEnd = tagEnd >= 0 ? tagEnd : text.Length;
        var fragment = text[(tagStart + 1)..fragmentEnd];
        if (string.IsNullOrWhiteSpace(fragment) || fragment.StartsWith("!--", StringComparison.Ordinal))
        {
            return null;
        }

        var relativePosition = normalizedPosition - (tagStart + 1);
        if (relativePosition < 0 || relativePosition > fragment.Length)
        {
            return null;
        }

        var tagNameStart = fragment.StartsWith('/') ? 1 : 0;
        var tagNameEnd = tagNameStart;
        while (tagNameEnd < fragment.Length && !char.IsWhiteSpace(fragment[tagNameEnd]) && fragment[tagNameEnd] is not '/' and not '>')
        {
            tagNameEnd++;
        }

        if (relativePosition >= tagNameStart && relativePosition <= tagNameEnd)
        {
            var tagName = fragment[tagNameStart..tagNameEnd];
            if (!string.IsNullOrWhiteSpace(tagName))
            {
                return new HoverSymbol(HoverSymbolKind.Tag, tagName, null, text, tagStart + 1 + tagNameStart, tagStart + 1 + tagNameEnd);
            }
        }

        var tagNameForAttributes = tagNameEnd > tagNameStart ? fragment[tagNameStart..tagNameEnd] : null;
        foreach (Match match in AttributeRegex.Matches(fragment))
        {
            var start = match.Groups["name"].Index;
            var end = start + match.Groups["name"].Length;
            if (relativePosition >= start && relativePosition <= end)
            {
                return new HoverSymbol(
                    HoverSymbolKind.Attribute,
                    match.Groups["name"].Value,
                    tagNameForAttributes,
                    text,
                    tagStart + 1 + start,
                    tagStart + 1 + end);
            }
        }

        return null;
    }

    private enum HoverSymbolKind
    {
        Tag,
        Attribute
    }

    private sealed record HoverSymbol(HoverSymbolKind Kind, string Name, string? TagName, string SourceText, int Start, int End);
}

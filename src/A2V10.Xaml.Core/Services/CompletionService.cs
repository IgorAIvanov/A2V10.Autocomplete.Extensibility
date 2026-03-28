using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Models;

namespace A2V10.Xaml.Core.Services;

public sealed class CompletionService : ICompletionService
{
    public IReadOnlyCollection<CompletionSuggestion> GetSuggestions(XamlCompletionContext context, MetadataRegistry metadata)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(metadata);

        return context.Kind switch
        {
            XamlCompletionKind.TagName => GetTagSuggestions(context, metadata),
            XamlCompletionKind.AttributeName => GetAttributeSuggestions(context, metadata),
            XamlCompletionKind.AttributeValue => GetAttributeValueSuggestions(context, metadata),
            _ => Array.Empty<CompletionSuggestion>()
        };
    }

    private static IReadOnlyCollection<CompletionSuggestion> GetTagSuggestions(XamlCompletionContext context, MetadataRegistry metadata)
    {
        return metadata.Tags
            .Where(tag => StartsWith(tag.Name, context.Prefix))
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tag => new CompletionSuggestion(tag.Name, tag.Name, tag.Description, XamlCompletionKind.TagName))
            .ToArray();
    }

    private static IReadOnlyCollection<CompletionSuggestion> GetAttributeSuggestions(XamlCompletionContext context, MetadataRegistry metadata)
    {
        if (string.IsNullOrWhiteSpace(context.TagName))
        {
            return Array.Empty<CompletionSuggestion>();
        }

        var tag = metadata.Tags.FirstOrDefault(tag => string.Equals(tag.Name, context.TagName, StringComparison.OrdinalIgnoreCase));
        if (tag is null)
        {
            return Array.Empty<CompletionSuggestion>();
        }

        return tag.Attributes
            .Where(attribute => StartsWith(attribute.Name, context.Prefix))
            .OrderBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase)
            .Select(attribute => new CompletionSuggestion(attribute.Name, attribute.Name, attribute.Description, XamlCompletionKind.AttributeName))
            .ToArray();
    }

    private static IReadOnlyCollection<CompletionSuggestion> GetAttributeValueSuggestions(XamlCompletionContext context, MetadataRegistry metadata)
    {
        if (string.IsNullOrWhiteSpace(context.TagName) || string.IsNullOrWhiteSpace(context.AttributeName))
        {
            return Array.Empty<CompletionSuggestion>();
        }

        var tag = metadata.Tags.FirstOrDefault(tag => string.Equals(tag.Name, context.TagName, StringComparison.OrdinalIgnoreCase));
        var attribute = tag?.Attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, context.AttributeName, StringComparison.OrdinalIgnoreCase));
        if (attribute is null)
        {
            return Array.Empty<CompletionSuggestion>();
        }

        return attribute.AllowedValues
            .Where(value => StartsWith(value, context.Prefix))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Select(value => new CompletionSuggestion(value, value, attribute.Description, XamlCompletionKind.AttributeValue))
            .ToArray();
    }

    private static bool StartsWith(string candidate, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return true;
        }

        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}

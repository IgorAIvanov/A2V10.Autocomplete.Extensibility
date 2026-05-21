using System.Collections.ObjectModel;

namespace A2V10.Xaml.Reflection.Documentation;

public sealed record XamlTagDocumentation
{
    public XamlTagDocumentation(
        string name,
        string? description,
        string? fullDocumentation = null,
        IReadOnlyDictionary<string, string>? attributeDescriptions = null,
        IReadOnlyDictionary<string, string>? attributeFullDocumentation = null)
    {
        Name = name;
        Description = description;
        FullDocumentation = fullDocumentation;
        AttributeDescriptions = attributeDescriptions is null
            ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(attributeDescriptions, StringComparer.OrdinalIgnoreCase));
        AttributeFullDocumentation = attributeFullDocumentation is null
            ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
            : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(attributeFullDocumentation, StringComparer.OrdinalIgnoreCase));
    }

    public string Name { get; }

    public string? Description { get; }

    public string? FullDocumentation { get; }

    public IReadOnlyDictionary<string, string> AttributeDescriptions { get; }

    public IReadOnlyDictionary<string, string> AttributeFullDocumentation { get; }

    public bool TryGetAttributeDescription(string attributeName, out string? description)
    {
        if (AttributeDescriptions.TryGetValue(attributeName, out var value))
        {
            description = value;
            return true;
        }

        description = null;
        return false;
    }

    public bool TryGetAttributeFullDocumentation(string attributeName, out string? documentation)
    {
        if (AttributeFullDocumentation.TryGetValue(attributeName, out var value))
        {
            documentation = value;
            return true;
        }

        documentation = null;
        return false;
    }
}

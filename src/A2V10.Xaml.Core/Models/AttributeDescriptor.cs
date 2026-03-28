using System.Collections.Immutable;

namespace A2V10.Xaml.Core.Models;

public sealed record AttributeDescriptor
{
    public AttributeDescriptor(string name, string? description = null, IReadOnlyCollection<string>? allowedValues = null)
    {
        Name = name;
        Description = description;
        AllowedValues = allowedValues?.ToImmutableArray() ?? ImmutableArray<string>.Empty;
    }

    public string Name { get; }

    public string? Description { get; }

    public IReadOnlyCollection<string> AllowedValues { get; }
}

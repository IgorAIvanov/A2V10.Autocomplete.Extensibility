using System.Collections.Immutable;

namespace A2V10.Xaml.Core.Models;

public sealed record TagDescriptor
{
    public TagDescriptor(string name, string? description = null, IReadOnlyCollection<AttributeDescriptor>? attributes = null)
    {
        Name = name;
        Description = description;
        Attributes = attributes?.ToImmutableArray() ?? ImmutableArray<AttributeDescriptor>.Empty;
    }

    public string Name { get; }

    public string? Description { get; }

    public IReadOnlyCollection<AttributeDescriptor> Attributes { get; }
}

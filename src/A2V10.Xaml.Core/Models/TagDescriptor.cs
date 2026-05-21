using System.Collections.Immutable;

namespace A2V10.Xaml.Core.Models;

public sealed record TagDescriptor
{
    public TagDescriptor(
        string name,
        string? description = null,
        IReadOnlyCollection<AttributeDescriptor>? attributes = null,
        string? fullDocumentation = null)
    {
        Name = name;
        Description = description;
        Attributes = attributes?.ToImmutableArray() ?? ImmutableArray<AttributeDescriptor>.Empty;
        FullDocumentation = fullDocumentation;
    }

    public string Name { get; }

    public string? Description { get; }

    public IReadOnlyCollection<AttributeDescriptor> Attributes { get; }

    public string? FullDocumentation { get; }
}

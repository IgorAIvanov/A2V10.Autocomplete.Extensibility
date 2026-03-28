using System.Collections.Immutable;

namespace A2V10.Xaml.Core.Models;

public sealed record MetadataRegistry
{
    public static MetadataRegistry Empty { get; } = new([]);

    public MetadataRegistry(IReadOnlyCollection<TagDescriptor> tags)
    {
        Tags = tags.ToImmutableArray();
    }

    public IReadOnlyCollection<TagDescriptor> Tags { get; }
}

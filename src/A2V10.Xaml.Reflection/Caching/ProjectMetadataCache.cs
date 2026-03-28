using System.Collections.Concurrent;
using A2V10.Xaml.Core.Models;

namespace A2V10.Xaml.Reflection.Caching;

public sealed class ProjectMetadataCache
{
    private readonly ConcurrentDictionary<string, MetadataRegistry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string key, out MetadataRegistry metadata)
        => _cache.TryGetValue(key, out metadata!);

    public void Set(string key, MetadataRegistry metadata)
        => _cache[key] = metadata;
}

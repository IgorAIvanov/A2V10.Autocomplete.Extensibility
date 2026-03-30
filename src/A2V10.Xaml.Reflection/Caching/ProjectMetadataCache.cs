using System.Collections.Concurrent;
using System.Threading;
using A2V10.Xaml.Core.Models;

namespace A2V10.Xaml.Reflection.Caching;

public sealed class ProjectMetadataCache
{
    private readonly ConcurrentDictionary<string, Lazy<MetadataRegistry>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGetValue(string key, out MetadataRegistry metadata)
    {
        if (_cache.TryGetValue(key, out var lazy) && lazy.IsValueCreated)
        {
            metadata = lazy.Value;
            return true;
        }

        metadata = null!;
        return false;
    }

    public MetadataRegistry GetOrAdd(string key, Func<MetadataRegistry> valueFactory)
    {
        var lazy = _cache.GetOrAdd(key,
            static (_, factory) => new Lazy<MetadataRegistry>(factory, LazyThreadSafetyMode.ExecutionAndPublication),
            valueFactory);

        try
        {
            return lazy.Value;
        }
        catch
        {
            _cache.TryRemove(new KeyValuePair<string, Lazy<MetadataRegistry>>(key, lazy));
            throw;
        }
    }
}

using System.Reflection;
using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Documents;
using A2V10.Xaml.Core.Models;
using A2V10.Xaml.Reflection.Caching;

namespace A2V10.Xaml.Reflection;

public sealed class ReflectionMetadataProvider : IMetadataProvider
{
    private const string XamlElementTypeName = "A2v10.Xaml.XamlElement";
    private const string AttachedPropertiesAttributeTypeName = "A2v10.System.Xaml.AttachedPropertiesAttribute";
    private const string ContentPropertyAttributeTypeName = "A2v10.System.Xaml.ContentPropertyAttribute";
    private const string IgnoreWritePropertiesAttributeTypeName = "A2v10.System.Xaml.IgnoreWritePropertiesAttribute";

    private readonly IAssemblyReferenceResolver _assemblyReferenceResolver;
    private readonly ProjectMetadataCache _cache;

    public ReflectionMetadataProvider(IAssemblyReferenceResolver assemblyReferenceResolver, ProjectMetadataCache? cache = null)
    {
        _assemblyReferenceResolver = assemblyReferenceResolver;
        _cache = cache ?? new ProjectMetadataCache();
    }

    public async Task<MetadataRegistry> GetMetadataAsync(XamlDocumentContext documentContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentContext);

        var assemblyPaths = await _assemblyReferenceResolver.ResolveAsync(documentContext, cancellationToken);
        if (assemblyPaths.Count == 0)
        {
            return MetadataRegistry.Empty;
        }

        var cacheKey = CreateCacheKey(documentContext.ProjectPath, assemblyPaths);
        if (_cache.TryGetValue(cacheKey, out var cachedMetadata))
        {
            return cachedMetadata;
        }

        return _cache.GetOrAdd(cacheKey, () => CreateMetadataRegistry(assemblyPaths, cancellationToken));
    }

    private static MetadataRegistry CreateMetadataRegistry(IReadOnlyCollection<string> assemblyPaths, CancellationToken cancellationToken)
    {
        using var metadataLoadContext = new MetadataLoadContext(
            new PathAssemblyResolver(CreateResolverPaths(assemblyPaths)),
            typeof(object).Assembly.GetName().Name);

        var tags = assemblyPaths
            .SelectMany(path => LoadTags(metadataLoadContext, path, cancellationToken))
            .GroupBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MetadataRegistry(tags);
    }

    private static IReadOnlyCollection<string> CreateResolverPaths(IReadOnlyCollection<string> assemblyPaths)
    {
        var resolverPaths = new HashSet<string>(assemblyPaths, StringComparer.OrdinalIgnoreCase);

        foreach (var assembliesDirectory in GetAssembliesDirectories(assemblyPaths))
        {
            foreach (var path in Directory.EnumerateFiles(assembliesDirectory, "*.dll", SearchOption.AllDirectories))
            {
                resolverPaths.Add(path);
            }
        }

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies)
        {
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                resolverPaths.Add(path);
            }
        }

        resolverPaths.Add(typeof(object).Assembly.Location);
        resolverPaths.Add(typeof(Enumerable).Assembly.Location);

        return resolverPaths.ToArray();
    }

    private static IReadOnlyCollection<string> GetAssembliesDirectories(IReadOnlyCollection<string> assemblyPaths)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assemblyPath in assemblyPaths)
        {
            for (var current = Path.GetDirectoryName(assemblyPath); !string.IsNullOrWhiteSpace(current); current = Path.GetDirectoryName(current))
            {
                if (string.Equals(Path.GetFileName(current), "@assemblies", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(current);
                    break;
                }
            }
        }

        return result.ToArray();
    }

    private static IEnumerable<TagDescriptor> LoadTags(MetadataLoadContext metadataLoadContext, string assemblyPath, CancellationToken cancellationToken)
    {
        Assembly? assembly = null;

        try
        {
            assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
        }
        catch
        {
            yield break;
        }

        Type[] exportedTypes;
        try
        {
            exportedTypes = assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            exportedTypes = ex.Types.Where(static type => type is not null).Cast<Type>().ToArray();
        }
        catch
        {
            yield break;
        }

        var tags = exportedTypes.Where(IsTagType)
            .Select(type =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var attributes = GetAttributes(type)
                    .OrderBy(static attribute => attribute.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new TagDescriptor(type.Name, type.FullName, attributes);
            })
            .ToArray();

        foreach (var tag in tags)
        {
            yield return tag;
        }
    }

    private static bool IsTagType(Type type)
        => type.IsClass
            && !type.IsAbstract
            && type.IsPublic
            && InheritsFrom(type, XamlElementTypeName);

    private static IReadOnlyCollection<AttributeDescriptor> GetAttributes(Type type)
    {
        var contentProperties = GetInheritedAttributeNames(type, ContentPropertyAttributeTypeName, "Name");
        var ignoredProperties = GetInheritedAttributeNames(type, IgnoreWritePropertiesAttributeTypeName, "Attrs");

        var regularAttributes = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => IsAttributeProperty(property, contentProperties, ignoredProperties))
            .Select(CreateAttribute);

        var attachedAttributes = GetInheritedAttributeNames(type, AttachedPropertiesAttributeTypeName, "List")
            .Select(static name => new AttributeDescriptor(name, "Attached property"));

        return regularAttributes
            .Concat(attachedAttributes)
            .GroupBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static bool IsAttributeProperty(
        PropertyInfo property,
        ISet<string> contentProperties,
        ISet<string> ignoredProperties)
    {
        if (!property.CanRead || property.GetMethod is null || !property.GetMethod.IsPublic)
        {
            return false;
        }

        if (property.SetMethod is null || !property.SetMethod.IsPublic)
        {
            return false;
        }

        if (property.GetIndexParameters().Length != 0)
        {
            return false;
        }

        if (contentProperties.Contains(property.Name) || ignoredProperties.Contains(property.Name))
        {
            return false;
        }

        return !InheritsFrom(property.PropertyType, typeof(Delegate).FullName!);
    }

    private static AttributeDescriptor CreateAttribute(PropertyInfo property)
    {
        var propertyType = GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        var allowedValues = string.Equals(propertyType.FullName, typeof(bool).FullName, StringComparison.Ordinal)
            ? new[] { bool.TrueString.ToLowerInvariant(), bool.FalseString.ToLowerInvariant() }
            : propertyType.IsEnum
                ? GetEnumNames(propertyType)
                : Array.Empty<string>();

        return new AttributeDescriptor(property.Name, property.PropertyType.FullName, allowedValues);
    }

    private static Type? GetUnderlyingType(Type type)
    {
        if (!type.IsGenericType || !string.Equals(type.GetGenericTypeDefinition().FullName, typeof(Nullable<>).FullName, StringComparison.Ordinal))
        {
            return null;
        }

        return type.GetGenericArguments()[0];
    }

    private static string[] GetEnumNames(Type type)
        => type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(static field => field.Name)
            .ToArray();

    private static bool InheritsFrom(Type type, string fullName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (string.Equals(current.FullName, fullName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static ISet<string> GetInheritedAttributeNames(Type type, string attributeTypeName, string propertyName)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var current in GetTypeHierarchy(type))
        {
            foreach (var attribute in current.GetCustomAttributesData().Where(attribute =>
                         string.Equals(attribute.AttributeType.FullName, attributeTypeName, StringComparison.Ordinal)))
            {
                foreach (var name in ExtractAttributeNames(attribute, propertyName))
                {
                    result.Add(name);
                }
            }
        }

        return result;
    }

    private static IEnumerable<Type> GetTypeHierarchy(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }

    private static IEnumerable<string> ExtractAttributeNames(CustomAttributeData attribute, string propertyName)
    {
        if (attribute.ConstructorArguments.Count != 0)
        {
            foreach (var name in SplitNames(attribute.ConstructorArguments[0].Value as string))
            {
                yield return name;
            }

            yield break;
        }

        var namedArgument = attribute.NamedArguments.FirstOrDefault(argument =>
            string.Equals(argument.MemberName, propertyName, StringComparison.Ordinal));

        foreach (var name in SplitNames(namedArgument.TypedValue.Value as string))
        {
            yield return name;
        }
    }

    private static IEnumerable<string> SplitNames(string? names)
    {
        if (string.IsNullOrWhiteSpace(names))
        {
            yield break;
        }

        foreach (var name in names.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            yield return name;
        }
    }

    private static string CreateCacheKey(string? projectPath, IReadOnlyCollection<string> assemblyPaths)
    {
        var projectPart = NormalizeProjectPath(projectPath);

        var assemblyPart = string.Join('|', assemblyPaths.Order(StringComparer.OrdinalIgnoreCase).Select(path =>
        {
            var timestamp = File.Exists(path)
                ? File.GetLastWriteTimeUtc(path).Ticks
                : 0;

            return $"{path}:{timestamp}";
        }));

        return $"{projectPart}|{assemblyPart}";
    }

    private static string NormalizeProjectPath(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return "no-project";
        }

        if (File.Exists(projectPath) || projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(projectPath) ?? projectPath;
        }

        return projectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

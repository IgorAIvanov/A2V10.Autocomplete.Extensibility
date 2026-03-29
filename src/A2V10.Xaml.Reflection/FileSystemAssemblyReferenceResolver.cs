using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Documents;

namespace A2V10.Xaml.Reflection;

public sealed class FileSystemAssemblyReferenceResolver : IAssemblyReferenceResolver
{
    private static readonly string[] ExcludedFileNamePrefixes =
    [
        "System.",
        "Microsoft.",
        "mscorlib",
        "netstandard",
        "WindowsBase",
        "PresentationCore",
        "PresentationFramework"
    ];

    public Task<IReadOnlyCollection<string>> ResolveAsync(XamlDocumentContext documentContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentContext);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(documentContext.ProjectPath))
        {
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        var projectDirectory = File.Exists(documentContext.ProjectPath)
            ? Path.GetDirectoryName(documentContext.ProjectPath)
            : documentContext.ProjectPath;

        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        var assembliesDirectory = Path.Combine(projectDirectory, "@assemblies");
        if (!Directory.Exists(assembliesDirectory))
        {
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        var assemblies = Directory.EnumerateFiles(assembliesDirectory, "*.dll", SearchOption.AllDirectories)
            .Where(IsCandidateAssembly)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<string>>(assemblies);
    }

    private static bool IsCandidateAssembly(string assemblyPath)
    {
        var fileName = Path.GetFileName(assemblyPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var directoryName = Path.GetDirectoryName(assemblyPath);
        if (!string.IsNullOrWhiteSpace(directoryName))
        {
            var directorySegments = directoryName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (directorySegments.Any(static segment => string.Equals(segment, "ref", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return !ExcludedFileNamePrefixes.Any(prefix => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

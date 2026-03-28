using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Documents;

namespace A2V10.Xaml.Reflection;

public sealed class FileSystemAssemblyReferenceResolver : IAssemblyReferenceResolver
{
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

        var binDirectory = Path.Combine(projectDirectory, "bin");
        if (!Directory.Exists(binDirectory))
        {
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        var assemblies = Directory.EnumerateFiles(binDirectory, "A2V10*.dll", SearchOption.AllDirectories)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<string>>(assemblies);
    }
}

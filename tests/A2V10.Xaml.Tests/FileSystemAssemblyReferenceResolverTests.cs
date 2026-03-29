using A2V10.Xaml.Core.Documents;
using A2V10.Xaml.Reflection;

namespace A2V10.Xaml.Tests;

public sealed class FileSystemAssemblyReferenceResolverTests
{
    [Fact]
    public async Task ResolveAsync_ReturnsApplicationAssemblies_FromAssembliesDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var projectDirectory = Path.Combine(root, "src", "SampleApp");
        var assembliesDirectory = Path.Combine(projectDirectory, "@assemblies");
        Directory.CreateDirectory(assembliesDirectory);

        var projectPath = Path.Combine(projectDirectory, "SampleApp.csproj");
        var appAssemblyPath = Path.Combine(assembliesDirectory, "SampleApp.dll");
        var frameworkAssemblyPath = Path.Combine(assembliesDirectory, "System.Text.Json.dll");
        var refDirectory = Path.Combine(assembliesDirectory, "ref");
        var refAssemblyPath = Path.Combine(refDirectory, "SampleApp.dll");

        File.WriteAllText(projectPath, "<Project />");
        File.WriteAllText(appAssemblyPath, string.Empty);
        File.WriteAllText(frameworkAssemblyPath, string.Empty);
        Directory.CreateDirectory(refDirectory);
        File.WriteAllText(refAssemblyPath, string.Empty);

        try
        {
            var resolver = new FileSystemAssemblyReferenceResolver();

            var assemblies = await resolver.ResolveAsync(new XamlDocumentContext(new Uri("file:///View.xaml"), "<Page />", projectPath));

            Assert.Contains(appAssemblyPath, assemblies, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain(frameworkAssemblyPath, assemblies, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain(refAssemblyPath, assemblies, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

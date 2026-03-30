using System.Reflection;
using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Documents;
using A2V10.Xaml.Reflection;

namespace A2V10.Xaml.Tests;

public sealed class ReflectionMetadataProviderTests
{
    [Fact]
    public async Task GetMetadataAsync_ReturnsOnlyXamlTags()
    {
        var provider = CreateProvider();

        var metadata = await provider.GetMetadataAsync(CreateDocumentContext());

        Assert.Contains(metadata.Tags, tag => tag.Name == "Grid");
        Assert.DoesNotContain(metadata.Tags, tag => tag.Name == "ToastConverter");
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsPropertiesAndAttachedAttributes()
    {
        var provider = CreateProvider();

        var metadata = await provider.GetMetadataAsync(CreateDocumentContext());
        var grid = Assert.Single(metadata.Tags.Where(tag => tag.Name == "Grid"));

        Assert.Contains(grid.Attributes, attribute => attribute.Name == "Rows");
        Assert.Contains(grid.Attributes, attribute => attribute.Name == "Col");
        Assert.Contains(grid.Attributes, attribute => attribute.Name == "RowSpan");
        Assert.DoesNotContain(grid.Attributes, attribute => attribute.Name == "Children");
        Assert.DoesNotContain(grid.Attributes, attribute => attribute.Name == "Attach");
        Assert.DoesNotContain(grid.Attributes, attribute => attribute.Name == "AttachedPropertyManager");
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsAllowedValuesForEnumAndNullableBoolAttributes()
    {
        var provider = CreateProvider();

        var metadata = await provider.GetMetadataAsync(CreateDocumentContext());
        var grid = Assert.Single(metadata.Tags.Where(tag => tag.Name == "Grid"));

        var autoFlow = Assert.Single(grid.Attributes.Where(attribute => attribute.Name == "AutoFlow"));
        var bold = Assert.Single(grid.Attributes.Where(attribute => attribute.Name == "Bold"));

        Assert.NotEmpty(autoFlow.AllowedValues);
        Assert.Contains("true", bold.AllowedValues);
        Assert.Contains("false", bold.AllowedValues);
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsCachedMetadata_ForSameProjectAndAssemblies()
    {
        var provider = CreateProvider();
        var documentContext = CreateDocumentContext();

        var first = await provider.GetMetadataAsync(documentContext);
        var second = await provider.GetMetadataAsync(documentContext);

        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsCachedMetadata_ForProjectDirectoryAndProjectFile()
    {
        var provider = CreateProvider();
        var documentUri = new Uri("file:///test.xaml");
        var first = await provider.GetMetadataAsync(new XamlDocumentContext(documentUri, "<Grid />", AppContext.BaseDirectory));
        var second = await provider.GetMetadataAsync(new XamlDocumentContext(documentUri, "<Grid />", Path.Combine(AppContext.BaseDirectory, "Test.csproj")));

        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetMetadataAsync_ReleasesAssemblyFile_AfterCachingMetadata()
    {
        var sourceAssemblyPath = GetAssemblyPath("A2v10.Xaml.dll");
        var tempDirectory = Directory.CreateTempSubdirectory();

        try
        {
            var copiedAssemblyPath = Path.Combine(tempDirectory.FullName, "A2v10.Xaml.dll");
            File.Copy(sourceAssemblyPath, copiedAssemblyPath);

            var provider = new ReflectionMetadataProvider(new StubAssemblyReferenceResolver(copiedAssemblyPath));

            _ = await provider.GetMetadataAsync(CreateDocumentContext());

            using var stream = new FileStream(copiedAssemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Assert.True(stream.CanWrite);
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    [Fact]
    public void CreateResolverPaths_IncludesAllDllsFromAssembliesDirectory()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var assembliesDirectory = Path.Combine(root.FullName, "MainApp", "@assemblies");
            var nestedDirectory = Path.Combine(assembliesDirectory, "nested");
            Directory.CreateDirectory(nestedDirectory);

            var candidateAssemblyPath = Path.Combine(nestedDirectory, "MainApp.Controls.dll");
            var dependencyAssemblyPath = Path.Combine(assembliesDirectory, "System.Xaml.dll");
            File.WriteAllText(candidateAssemblyPath, string.Empty);
            File.WriteAllText(dependencyAssemblyPath, string.Empty);

            var method = typeof(ReflectionMetadataProvider).GetMethod("CreateResolverPaths", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var resolverPaths = Assert.IsAssignableFrom<IReadOnlyCollection<string>>(method!.Invoke(null, [new[] { candidateAssemblyPath }])!);

            Assert.Contains(candidateAssemblyPath, resolverPaths);
            Assert.Contains(dependencyAssemblyPath, resolverPaths);
        }
        finally
        {
            root.Delete(true);
        }
    }

    private static ReflectionMetadataProvider CreateProvider()
        => new(new StubAssemblyReferenceResolver(GetAssemblyPath("A2v10.Xaml.dll")));

    private static XamlDocumentContext CreateDocumentContext()
        => new(new Uri("file:///test.xaml"), "<Grid />", AppContext.BaseDirectory);

    private static string GetAssemblyPath(string assemblyFileName)
    {
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, assemblyFileName);
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly '{assemblyFileName}' was not found.", assemblyPath);
        }

        return assemblyPath;
    }

    private sealed class StubAssemblyReferenceResolver(string assemblyPath) : IAssemblyReferenceResolver
    {
        public Task<IReadOnlyCollection<string>> ResolveAsync(XamlDocumentContext documentContext, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<string>>([assemblyPath]);
    }
}

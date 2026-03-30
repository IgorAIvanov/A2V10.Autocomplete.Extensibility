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

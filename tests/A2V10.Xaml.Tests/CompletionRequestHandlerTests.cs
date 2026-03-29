using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Documents;
using A2V10.Xaml.Core.Models;
using A2V10.Xaml.LanguageServer.Application;
using A2V10.Xaml.LanguageServer.Protocol;

namespace A2V10.Xaml.Tests;

public sealed class CompletionRequestHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsSuggestions_WithoutAssemblyReferenceInText()
    {
        var expectedContext = new XamlCompletionContext(XamlCompletionKind.TagName, "Gr", null, null, 3);
        var metadata = new MetadataRegistry([new TagDescriptor("Grid")]);
        var suggestions = new[]
        {
            new CompletionSuggestion("Grid", "Grid", null, XamlCompletionKind.TagName)
        };

        var parser = new StubXamlContextParser(expectedContext);
        var metadataProvider = new StubMetadataProvider(metadata);
        var completionService = new StubCompletionService(suggestions);
        var handler = new CompletionRequestHandler(parser, metadataProvider, completionService);

        var response = await handler.HandleAsync(new CompletionRequest("C:\\Temp\\View.xaml", 3, "C:\\Temp\\Project.csproj", "<Gr"));

        Assert.Equal(suggestions, response.Items);
        Assert.Equal("<Gr", metadataProvider.LastDocument?.Text);
    }

    private sealed class StubXamlContextParser(XamlCompletionContext context) : IXamlContextParser
    {
        public XamlCompletionContext Parse(string text, int position) => context;
    }

    private sealed class StubMetadataProvider(MetadataRegistry metadata) : IMetadataProvider
    {
        public XamlDocumentContext? LastDocument { get; private set; }

        public Task<MetadataRegistry> GetMetadataAsync(XamlDocumentContext documentContext, CancellationToken cancellationToken = default)
        {
            LastDocument = documentContext;
            return Task.FromResult(metadata);
        }
    }

    private sealed class StubCompletionService(IReadOnlyCollection<CompletionSuggestion> suggestions) : ICompletionService
    {
        public IReadOnlyCollection<CompletionSuggestion> GetSuggestions(XamlCompletionContext context, MetadataRegistry metadata)
            => suggestions;
    }
}

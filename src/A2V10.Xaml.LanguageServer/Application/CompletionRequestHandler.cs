using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Documents;
using A2V10.Xaml.LanguageServer.Protocol;

namespace A2V10.Xaml.LanguageServer.Application;

public sealed class CompletionRequestHandler
{
    private readonly IXamlContextParser _contextParser;
    private readonly IMetadataProvider _metadataProvider;
    private readonly ICompletionService _completionService;

    public CompletionRequestHandler(IXamlContextParser contextParser, IMetadataProvider metadataProvider, ICompletionService completionService)
    {
        _contextParser = contextParser;
        _metadataProvider = metadataProvider;
        _completionService = completionService;
    }

    public async Task<CompletionResponse> HandleAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var text = request.Text;
        if (text is null)
        {
            if (!File.Exists(request.FilePath))
            {
                return new CompletionResponse([]);
            }

            text = await File.ReadAllTextAsync(request.FilePath, cancellationToken);
        }

        var document = new XamlDocumentContext(new Uri(request.FilePath), text, request.ProjectPath);
        var context = _contextParser.Parse(text, request.Position);
        var metadata = await _metadataProvider.GetMetadataAsync(document, cancellationToken);
        var suggestions = _completionService.GetSuggestions(context, metadata);

        return new CompletionResponse(suggestions, context.Prefix.Length);
    }
}

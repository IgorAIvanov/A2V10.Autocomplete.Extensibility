using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Services;
using A2V10.Xaml.Reflection;
using A2V10.Xaml.Reflection.Documentation;

namespace A2V10.Xaml.LanguageServer.Application;

public sealed class LanguageServerComposition
{
    public LanguageServerComposition(CompletionRequestHandler completionHandler, HoverRequestHandler hoverHandler, IMetadataProvider metadataProvider)
    {
        CompletionHandler = completionHandler;
        HoverHandler = hoverHandler;
        MetadataProvider = metadataProvider;
    }

    public CompletionRequestHandler CompletionHandler { get; }

    public HoverRequestHandler HoverHandler { get; }

    public IMetadataProvider MetadataProvider { get; }

    public static LanguageServerComposition CreateDefault()
    {
        IXamlContextParser contextParser = new XamlContextParser();
        IAssemblyReferenceResolver assemblyResolver = new FileSystemAssemblyReferenceResolver();
        var documentationProvider = new XamlDocumentationProvider();
        IMetadataProvider metadataProvider = new ReflectionMetadataProvider(assemblyResolver, documentationProvider: documentationProvider);
        ICompletionService completionService = new CompletionService();
        var hoverHandler = new HoverRequestHandler(metadataProvider);

        return new LanguageServerComposition(new CompletionRequestHandler(contextParser, metadataProvider, completionService), hoverHandler, metadataProvider);
    }
}

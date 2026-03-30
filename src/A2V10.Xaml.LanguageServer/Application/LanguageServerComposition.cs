using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Services;
using A2V10.Xaml.Reflection;

namespace A2V10.Xaml.LanguageServer.Application;

public sealed class LanguageServerComposition
{
    public LanguageServerComposition(CompletionRequestHandler completionHandler, IMetadataProvider metadataProvider)
    {
        CompletionHandler = completionHandler;
        MetadataProvider = metadataProvider;
    }

    public CompletionRequestHandler CompletionHandler { get; }

    public IMetadataProvider MetadataProvider { get; }

    public static LanguageServerComposition CreateDefault()
    {
        IXamlContextParser contextParser = new XamlContextParser();
        IAssemblyReferenceResolver assemblyResolver = new FileSystemAssemblyReferenceResolver();
        IMetadataProvider metadataProvider = new ReflectionMetadataProvider(assemblyResolver);
        ICompletionService completionService = new CompletionService();

        return new LanguageServerComposition(new CompletionRequestHandler(contextParser, metadataProvider, completionService), metadataProvider);
    }
}

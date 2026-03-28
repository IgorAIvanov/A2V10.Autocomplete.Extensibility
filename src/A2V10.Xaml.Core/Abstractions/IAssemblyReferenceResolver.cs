using A2V10.Xaml.Core.Documents;

namespace A2V10.Xaml.Core.Abstractions;

public interface IAssemblyReferenceResolver
{
    Task<IReadOnlyCollection<string>> ResolveAsync(XamlDocumentContext documentContext, CancellationToken cancellationToken = default);
}

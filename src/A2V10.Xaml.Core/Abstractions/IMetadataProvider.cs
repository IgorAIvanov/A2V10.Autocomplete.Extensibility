using A2V10.Xaml.Core.Documents;
using A2V10.Xaml.Core.Models;

namespace A2V10.Xaml.Core.Abstractions;

public interface IMetadataProvider
{
    Task<MetadataRegistry> GetMetadataAsync(XamlDocumentContext documentContext, CancellationToken cancellationToken = default);
}

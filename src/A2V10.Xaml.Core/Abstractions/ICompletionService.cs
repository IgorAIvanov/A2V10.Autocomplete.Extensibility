using A2V10.Xaml.Core.Models;

namespace A2V10.Xaml.Core.Abstractions;

public interface ICompletionService
{
    IReadOnlyCollection<CompletionSuggestion> GetSuggestions(XamlCompletionContext context, MetadataRegistry metadata);
}

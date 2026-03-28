using A2V10.Xaml.Core.Models;

namespace A2V10.Xaml.LanguageServer.Protocol;

public sealed record CompletionResponse(IReadOnlyCollection<CompletionSuggestion> Items);

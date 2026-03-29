using System.Text;
using System.Text.Json;
using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Documents;
using A2V10.Xaml.Core.Models;
using A2V10.Xaml.LanguageServer.Application;
using A2V10.Xaml.LanguageServer.Protocol;

namespace A2V10.Xaml.Tests;

public sealed class LspServerHostTests
{
    [Fact]
    public async Task RunAsync_WritesSnippetCompletionItems()
    {
        var suggestion = new CompletionSuggestion(
            "Dialog",
            "Dialog xmlns=\"clr-namespace:A2v10.Xaml;assembly=A2v10.Xaml\"$0></Dialog>",
            "Dialog root",
            XamlCompletionKind.TagName,
            true);

        var handler = new CompletionRequestHandler(
            new StubXamlContextParser(new XamlCompletionContext(XamlCompletionKind.TagName, "Dia", null, null, 3)),
            new StubMetadataProvider(MetadataRegistry.Empty),
            new StubCompletionService([suggestion]));
        var host = new LspServerHost(handler, new TextDocumentStore());

        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xaml");

        try
        {
            await File.WriteAllTextAsync(filePath, "<Di");

            await using var input = new MemoryStream(Encoding.UTF8.GetBytes(CreateMessage(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "textDocument/completion",
                @params = new
                {
                    textDocument = new
                    {
                        uri = new Uri(filePath).AbsoluteUri
                    },
                    position = new
                    {
                        line = 0,
                        character = 3
                    }
                }
            }))));
            await using var output = new MemoryStream();

            await host.RunAsync(input, output);

            output.Position = 0;
            using var document = JsonDocument.Parse(ReadPayload(output));
            var item = document.RootElement
                .GetProperty("result")
                .GetProperty("items")[0];

            Assert.Equal("Dialog", item.GetProperty("label").GetString());
            Assert.Equal("Dialog xmlns=\"clr-namespace:A2v10.Xaml;assembly=A2v10.Xaml\"$0></Dialog>", item.GetProperty("insertText").GetString());
            Assert.Equal("Dialog", item.GetProperty("filterText").GetString());
            Assert.Equal("Dialog", item.GetProperty("sortText").GetString());
            Assert.Equal("Dialog xmlns=\"clr-namespace:A2v10.Xaml;assembly=A2v10.Xaml\"$0></Dialog>", item.GetProperty("textEdit").GetProperty("newText").GetString());
            Assert.Equal(0, item.GetProperty("textEdit").GetProperty("range").GetProperty("start").GetProperty("line").GetInt32());
            Assert.Equal(0, item.GetProperty("textEdit").GetProperty("range").GetProperty("start").GetProperty("character").GetInt32());
            Assert.Equal(0, item.GetProperty("textEdit").GetProperty("range").GetProperty("end").GetProperty("line").GetInt32());
            Assert.Equal(3, item.GetProperty("textEdit").GetProperty("range").GetProperty("end").GetProperty("character").GetInt32());
            Assert.Equal(2, item.GetProperty("insertTextFormat").GetInt32());
            Assert.Equal(15, item.GetProperty("kind").GetInt32());
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static string CreateMessage(string json)
        => $"Content-Length: {Encoding.UTF8.GetByteCount(json)}\r\n\r\n{json}";

    private static string ReadPayload(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
        while (true)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                return string.Empty;
            }

            if (line.Length == 0)
            {
                return reader.ReadToEnd();
            }
        }
    }

    private sealed class StubXamlContextParser(XamlCompletionContext context) : IXamlContextParser
    {
        public XamlCompletionContext Parse(string text, int position) => context;
    }

    private sealed class StubMetadataProvider(MetadataRegistry metadata) : IMetadataProvider
    {
        public Task<MetadataRegistry> GetMetadataAsync(XamlDocumentContext documentContext, CancellationToken cancellationToken = default)
            => Task.FromResult(metadata);
    }

    private sealed class StubCompletionService(IReadOnlyCollection<CompletionSuggestion> suggestions) : ICompletionService
    {
        public IReadOnlyCollection<CompletionSuggestion> GetSuggestions(XamlCompletionContext context, MetadataRegistry metadata)
            => suggestions;
    }
}

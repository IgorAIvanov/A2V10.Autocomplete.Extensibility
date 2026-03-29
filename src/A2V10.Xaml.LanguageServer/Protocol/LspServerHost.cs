using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using A2V10.Xaml.Core.Models;
using A2V10.Xaml.LanguageServer.Application;

namespace A2V10.Xaml.LanguageServer.Protocol;

public sealed class LspServerHost
{
    private readonly CompletionRequestHandler _completionHandler;
    private readonly TextDocumentStore _documentStore;

    public LspServerHost(CompletionRequestHandler completionHandler, TextDocumentStore documentStore)
    {
        _completionHandler = completionHandler;
        _documentStore = documentStore;
    }

    public async Task RunAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await ReadMessageAsync(input, cancellationToken);
            if (message is null)
            {
                return;
            }

            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;
            if (!root.TryGetProperty("method", out var methodProperty))
            {
                continue;
            }

            var method = methodProperty.GetString();
            var hasId = root.TryGetProperty("id", out var idProperty);

            switch (method)
            {
                case "initialize":
                    if (hasId)
                    {
                        await WriteInitializeResponseAsync(output, idProperty, cancellationToken);
                    }
                    break;
                case "initialized":
                    break;
                case "textDocument/didOpen":
                    HandleDidOpen(root);
                    break;
                case "textDocument/didChange":
                    HandleDidChange(root);
                    break;
                case "textDocument/didClose":
                    HandleDidClose(root);
                    break;
                case "textDocument/completion":
                    if (hasId)
                    {
                        await HandleCompletionAsync(root, idProperty, output, cancellationToken);
                    }
                    break;
                case "shutdown":
                    if (hasId)
                    {
                        await WriteNullResultAsync(output, idProperty, cancellationToken);
                    }
                    break;
                case "exit":
                    return;
                default:
                    if (hasId)
                    {
                        await WriteMethodNotFoundAsync(output, idProperty, cancellationToken);
                    }
                    break;
            }
        }
    }

    private void HandleDidOpen(JsonElement root)
    {
        var textDocument = root.GetProperty("params").GetProperty("textDocument");
        var uri = textDocument.GetProperty("uri").GetString();
        var text = textDocument.GetProperty("text").GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(uri))
        {
            return;
        }

        _documentStore.Open(uri, text, ProjectPathResolver.FindProjectPath(GetFilePath(uri)));
    }

    private void HandleDidChange(JsonElement root)
    {
        var parameters = root.GetProperty("params");
        var uri = parameters.GetProperty("textDocument").GetProperty("uri").GetString();
        if (string.IsNullOrWhiteSpace(uri))
        {
            return;
        }

        var changes = parameters.GetProperty("contentChanges");
        if (changes.GetArrayLength() == 0)
        {
            return;
        }

        var text = changes[changes.GetArrayLength() - 1].GetProperty("text").GetString() ?? string.Empty;
        _documentStore.Update(uri, text);
    }

    private void HandleDidClose(JsonElement root)
    {
        var uri = root.GetProperty("params").GetProperty("textDocument").GetProperty("uri").GetString();
        if (string.IsNullOrWhiteSpace(uri))
        {
            return;
        }

        _documentStore.Close(uri);
    }

    private async Task HandleCompletionAsync(JsonElement root, JsonElement idProperty, Stream output, CancellationToken cancellationToken)
    {
        var parameters = root.GetProperty("params");
        var position = parameters.GetProperty("position");
        var line = position.GetProperty("line").GetInt32();
        var character = position.GetProperty("character").GetInt32();
        var uri = parameters.GetProperty("textDocument").GetProperty("uri").GetString();
        if (string.IsNullOrWhiteSpace(uri))
        {
            await WriteCompletionResponseAsync(output, idProperty, new CompletionResponse(Array.Empty<CompletionSuggestion>()), line, character, cancellationToken);
            return;
        }

        var filePath = GetFilePath(uri);
        var text = GetDocumentText(uri, filePath);
        if (text is null)
        {
            await WriteCompletionResponseAsync(output, idProperty, new CompletionResponse(Array.Empty<CompletionSuggestion>()), line, character, cancellationToken);
            return;
        }

        var projectPath = GetProjectPath(uri, filePath);
        var offset = LspTextPositionConverter.ToOffset(text, line, character);
        var response = await _completionHandler.HandleAsync(new CompletionRequest(filePath, offset, projectPath, text), cancellationToken);

        await WriteCompletionResponseAsync(output, idProperty, response, line, character, cancellationToken);
    }

    private string? GetDocumentText(string documentUri, string filePath)
    {
        if (_documentStore.TryGet(documentUri, out var state) && state is not null)
        {
            return state.Text;
        }

        return File.Exists(filePath)
            ? File.ReadAllText(filePath)
            : null;
    }

    private string? GetProjectPath(string documentUri, string filePath)
    {
        if (_documentStore.TryGet(documentUri, out var state) && state is not null && !string.IsNullOrWhiteSpace(state.ProjectPath))
        {
            return state.ProjectPath;
        }

        return ProjectPathResolver.FindProjectPath(filePath);
    }

    private static string GetFilePath(string documentUri)
    {
        if (Uri.TryCreate(documentUri, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            return uri.LocalPath;
        }

        return documentUri;
    }

    private static async Task<string?> ReadMessageAsync(Stream input, CancellationToken cancellationToken)
    {
        int? contentLength = null;
        while (true)
        {
            var line = await ReadHeaderLineAsync(input, cancellationToken);
            if (line is null)
            {
                return null;
            }

            if (line.Length == 0)
            {
                break;
            }

            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                var value = line["Content-Length:".Length..].Trim();
                contentLength = int.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        if (contentLength is null or <= 0)
        {
            return null;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(contentLength.Value);
        try
        {
            await ReadExactlyAsync(input, buffer.AsMemory(0, contentLength.Value), cancellationToken);
            return Encoding.UTF8.GetString(buffer, 0, contentLength.Value);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<string?> ReadHeaderLineAsync(Stream input, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        while (true)
        {
            var nextByte = new byte[1];
            var read = await input.ReadAsync(nextByte, cancellationToken);
            if (read == 0)
            {
                return buffer.Length == 0 ? null : Encoding.ASCII.GetString(buffer.ToArray()).TrimEnd('\r');
            }

            if (nextByte[0] == '\n')
            {
                return Encoding.ASCII.GetString(buffer.ToArray()).TrimEnd('\r');
            }

            buffer.WriteByte(nextByte[0]);
        }
    }

    private static async Task ReadExactlyAsync(Stream input, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await input.ReadAsync(buffer[totalRead..], cancellationToken);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading an LSP payload.");
            }

            totalRead += read;
        }
    }

    private static Task WriteInitializeResponseAsync(Stream output, JsonElement idProperty, CancellationToken cancellationToken)
        => WriteMessageAsync(output, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("jsonrpc", "2.0");
            writer.WritePropertyName("id");
            idProperty.WriteTo(writer);
            writer.WritePropertyName("result");
            writer.WriteStartObject();
            writer.WritePropertyName("capabilities");
            writer.WriteStartObject();
            writer.WriteNumber("textDocumentSync", 1);
            writer.WritePropertyName("completionProvider");
            writer.WriteStartObject();
            writer.WriteBoolean("resolveProvider", false);
            writer.WritePropertyName("triggerCharacters");
            writer.WriteStartArray();
            writer.WriteStringValue("<");
            writer.WriteStringValue(" ");
            writer.WriteStringValue(":");
            writer.WriteStringValue("\"");
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WritePropertyName("serverInfo");
            writer.WriteStartObject();
            writer.WriteString("name", "A2V10.Xaml.LanguageServer");
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }, cancellationToken);

    private static Task WriteCompletionResponseAsync(
        Stream output,
        JsonElement idProperty,
        CompletionResponse response,
        int line,
        int character,
        CancellationToken cancellationToken)
        => WriteMessageAsync(output, writer =>
        {
            var replaceStartCharacter = Math.Max(0, character - response.ReplaceLength);

            writer.WriteStartObject();
            writer.WriteString("jsonrpc", "2.0");
            writer.WritePropertyName("id");
            idProperty.WriteTo(writer);
            writer.WritePropertyName("result");
            writer.WriteStartObject();
            writer.WriteBoolean("isIncomplete", false);
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            foreach (var item in response.Items)
            {
                writer.WriteStartObject();
                writer.WriteString("label", item.Label);
                writer.WriteString("insertText", item.InsertText);
                writer.WriteString("filterText", item.Label);
                writer.WriteString("sortText", item.Label);
                writer.WritePropertyName("textEdit");
                writer.WriteStartObject();
                writer.WritePropertyName("range");
                writer.WriteStartObject();
                writer.WritePropertyName("start");
                writer.WriteStartObject();
                writer.WriteNumber("line", line);
                writer.WriteNumber("character", replaceStartCharacter);
                writer.WriteEndObject();
                writer.WritePropertyName("end");
                writer.WriteStartObject();
                writer.WriteNumber("line", line);
                writer.WriteNumber("character", character);
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteString("newText", item.InsertText);
                writer.WriteEndObject();
                if (item.IsSnippet)
                {
                    writer.WriteNumber("insertTextFormat", 2);
                }
                if (!string.IsNullOrWhiteSpace(item.Detail))
                {
                    writer.WriteString("detail", item.Detail);
                }
                writer.WriteNumber("kind", MapCompletionKind(item));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }, cancellationToken);

    private static Task WriteMethodNotFoundAsync(Stream output, JsonElement idProperty, CancellationToken cancellationToken)
        => WriteMessageAsync(output, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("jsonrpc", "2.0");
            writer.WritePropertyName("id");
            idProperty.WriteTo(writer);
            writer.WritePropertyName("error");
            writer.WriteStartObject();
            writer.WriteNumber("code", -32601);
            writer.WriteString("message", "Method not found.");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }, cancellationToken);

    private static Task WriteNullResultAsync(Stream output, JsonElement idProperty, CancellationToken cancellationToken)
        => WriteMessageAsync(output, writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("jsonrpc", "2.0");
            writer.WritePropertyName("id");
            idProperty.WriteTo(writer);
            writer.WriteNull("result");
            writer.WriteEndObject();
        }, cancellationToken);

    private static async Task WriteMessageAsync(Stream output, Action<Utf8JsonWriter> writePayload, CancellationToken cancellationToken)
    {
        using var payloadStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(payloadStream))
        {
            writePayload(writer);
        }

        var header = Encoding.ASCII.GetBytes($"Content-Length: {payloadStream.Length}\r\n\r\n");
        await output.WriteAsync(header, cancellationToken);
        payloadStream.Position = 0;
        await payloadStream.CopyToAsync(output, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }

    private static int MapCompletionKind(CompletionSuggestion item)
        => item.IsSnippet
            ? 15
            : item.Kind switch
        {
            XamlCompletionKind.TagName => 7,
            XamlCompletionKind.AttributeName => 10,
            XamlCompletionKind.AttributeValue => 12,
            _ => 1,
        };
}

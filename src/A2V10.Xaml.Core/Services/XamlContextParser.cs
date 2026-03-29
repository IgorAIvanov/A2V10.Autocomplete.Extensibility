using A2V10.Xaml.Core.Abstractions;
using A2V10.Xaml.Core.Models;

namespace A2V10.Xaml.Core.Services;

public sealed class XamlContextParser : IXamlContextParser
{
    public XamlCompletionContext Parse(string text, int position)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (position < 0 || position > text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        var beforeCursor = text[..position];
        var tagStart = beforeCursor.LastIndexOf('<');
        var tagEnd = beforeCursor.LastIndexOf('>');

        if (tagStart < 0 || tagStart < tagEnd)
        {
            return new XamlCompletionContext(XamlCompletionKind.Unknown, string.Empty, null, null, position);
        }

        var fragment = beforeCursor[(tagStart + 1)..];
        if (string.IsNullOrWhiteSpace(fragment))
        {
            return new XamlCompletionContext(XamlCompletionKind.TagName, string.Empty, null, null, position);
        }

        if (fragment.StartsWith("!--", StringComparison.Ordinal))
        {
            return new XamlCompletionContext(XamlCompletionKind.Unknown, string.Empty, null, null, position);
        }

        if (fragment[0] == '/')
        {
            return new XamlCompletionContext(XamlCompletionKind.TagName, fragment[1..].Trim(), null, null, position, true);
        }

        return ParseOpenTag(fragment, position);
    }

    private static XamlCompletionContext ParseOpenTag(string fragment, int position)
    {
        var index = 0;
        while (index < fragment.Length && !char.IsWhiteSpace(fragment[index]) && fragment[index] != '/' && fragment[index] != '>')
        {
            index++;
        }

        var tagName = fragment[..index];
        if (index == fragment.Length)
        {
            return new XamlCompletionContext(XamlCompletionKind.TagName, tagName, null, null, position);
        }

        var state = AttributeParseState.BetweenAttributes;
        var currentAttributeName = string.Empty;
        var currentToken = string.Empty;
        char? quote = null;

        for (var i = index; i < fragment.Length; i++)
        {
            var ch = fragment[i];
            if (quote.HasValue)
            {
                if (ch == quote.Value)
                {
                    quote = null;
                    currentToken = string.Empty;
                    state = AttributeParseState.BetweenAttributes;
                    continue;
                }

                currentToken += ch;
                state = AttributeParseState.AttributeValue;
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                if (state == AttributeParseState.AttributeName)
                {
                    state = AttributeParseState.AfterAttributeName;
                }
                else if (state == AttributeParseState.BetweenAttributes)
                {
                    currentToken = string.Empty;
                }

                continue;
            }

            if (ch == '=')
            {
                currentAttributeName = currentToken;
                currentToken = string.Empty;
                state = AttributeParseState.BeforeAttributeValue;
                continue;
            }

            if (ch is '"' or '\'')
            {
                quote = ch;
                currentToken = string.Empty;
                state = AttributeParseState.AttributeValue;
                continue;
            }

            if (ch == '/' || ch == '>')
            {
                break;
            }

            if (state is AttributeParseState.BetweenAttributes or AttributeParseState.AfterAttributeName)
            {
                currentToken = string.Empty;
                state = AttributeParseState.AttributeName;
            }

            currentToken += ch;
        }

        return state switch
        {
            AttributeParseState.AttributeValue => new XamlCompletionContext(
                XamlCompletionKind.AttributeValue,
                currentToken,
                tagName,
                currentAttributeName,
                position),
            AttributeParseState.BeforeAttributeValue => new XamlCompletionContext(
                XamlCompletionKind.AttributeValue,
                string.Empty,
                tagName,
                currentAttributeName,
                position),
            AttributeParseState.AttributeName or AttributeParseState.AfterAttributeName or AttributeParseState.BetweenAttributes => new XamlCompletionContext(
                XamlCompletionKind.AttributeName,
                currentToken,
                tagName,
                null,
                position),
            _ => new XamlCompletionContext(XamlCompletionKind.Unknown, string.Empty, tagName, null, position)
        };
    }

    private enum AttributeParseState
    {
        BetweenAttributes,
        AttributeName,
        AfterAttributeName,
        BeforeAttributeValue,
        AttributeValue
    }
}

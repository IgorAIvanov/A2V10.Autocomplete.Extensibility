namespace A2V10.Xaml.LanguageServer.Application;

public static class LspTextPositionConverter
{
    public static (int line, int character) ToLineCharacter(string text, int offset)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalizedOffset = Math.Clamp(offset, 0, text.Length);
        var line = 0;
        var lineStart = 0;

        for (var index = 0; index < normalizedOffset; index++)
        {
            if (text[index] == '\r')
            {
                if (index + 1 < normalizedOffset && text[index + 1] == '\n')
                {
                    index++;
                }

                line++;
                lineStart = index + 1;
                continue;
            }

            if (text[index] == '\n')
            {
                line++;
                lineStart = index + 1;
            }
        }

        return (line, normalizedOffset - lineStart);
    }

    public static int ToOffset(string text, int line, int character)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (line <= 0)
        {
            return Math.Clamp(character, 0, text.Length);
        }

        var currentLine = 0;
        var index = 0;
        while (index < text.Length && currentLine < line)
        {
            if (text[index] == '\r')
            {
                index++;
                if (index < text.Length && text[index] == '\n')
                {
                    index++;
                }
                currentLine++;
                continue;
            }

            if (text[index] == '\n')
            {
                index++;
                currentLine++;
                continue;
            }

            index++;
        }

        var lineStart = index;
        while (index < text.Length && text[index] is not ('\r' or '\n'))
        {
            index++;
        }

        var lineLength = index - lineStart;
        return lineStart + Math.Clamp(character, 0, lineLength);
    }
}

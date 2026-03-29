namespace A2V10.Xaml.LanguageServer.Application;

public static class LspTextPositionConverter
{
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

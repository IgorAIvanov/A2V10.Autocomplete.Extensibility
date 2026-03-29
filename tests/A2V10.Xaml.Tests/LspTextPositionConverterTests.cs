using A2V10.Xaml.LanguageServer.Application;

namespace A2V10.Xaml.Tests;

public sealed class LspTextPositionConverterTests
{
    [Fact]
    public void ToOffset_ReturnsExpectedOffset_ForCrLfText()
    {
        const string text = "<Grid>\r\n  <Button />\r\n</Grid>";

        var offset = LspTextPositionConverter.ToOffset(text, 1, 3);

        Assert.Equal(text.IndexOf("<Button", StringComparison.Ordinal) + 1, offset);
    }

    [Fact]
    public void ToOffset_ClampsCharacterPastLineLength()
    {
        const string text = "ab\ncd";

        var offset = LspTextPositionConverter.ToOffset(text, 1, 10);

        Assert.Equal(text.Length, offset);
    }
}

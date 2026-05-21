using A2V10.Xaml.Reflection.Documentation;

namespace A2V10.Xaml.Tests;

public sealed class XamlDocumentationProviderTests
{
    [Fact]
    public void ParseContent_ReturnsTagAndAttributeDescriptions()
    {
        const string html = """
<div>
    <div class="title">
        <h1>Елемент TextBox</h1>
    </div>
    <p>Являє собою поле для введення тексту.</p>
    <h3>Властивості</h3>
    <table class="table-props">
        <tbody>
            <tr>
                <td class="prop-name">Placeholder</td>
                <td>String</td>
                <td>Підказка для порожнього поля.</td>
            </tr>
            <tr class="top">
                <td class="prop-name">Size</td>
                <td><code>ControlSize</code></td>
                <td>
                    Розмір елементу.
                    <ul class="enum-vals">
                        <li><span class="prop-name">Large</span> - збільшений розмір.</li>
                    </ul>
                </td>
            </tr>
        </tbody>
    </table>
</div>
""";

        var documentation = XamlDocumentationProvider.ParseContent(html);

        Assert.NotNull(documentation);
        Assert.Equal("TextBox", documentation.Name);
        Assert.Equal("Являє собою поле для введення тексту.", documentation.Description);
        Assert.Contains("Властивості", documentation.FullDocumentation);
        Assert.True(documentation.TryGetAttributeDescription("Placeholder", out var placeholderDescription));
        Assert.Equal("Підказка для порожнього поля.", placeholderDescription);
        Assert.True(documentation.TryGetAttributeFullDocumentation("Size", out var sizeFullDocumentation));
        Assert.True(documentation.TryGetAttributeDescription("Size", out var sizeDescription));
        Assert.Equal("Розмір елементу.", sizeDescription);
        Assert.Contains("Large - збільшений розмір.", sizeFullDocumentation);
    }

    [Fact]
    public void ParseContent_ReturnsNull_WhenHtmlDoesNotContainTagTitle()
    {
        var documentation = XamlDocumentationProvider.ParseContent("<html><body><p>No tag here</p></body></html>");

        Assert.Null(documentation);
    }
}
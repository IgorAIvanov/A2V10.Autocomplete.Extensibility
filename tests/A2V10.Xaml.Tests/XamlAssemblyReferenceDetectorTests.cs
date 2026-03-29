using A2V10.Xaml.Core.Documents;

namespace A2V10.Xaml.Tests;

public sealed class XamlAssemblyReferenceDetectorTests
{
    [Fact]
    public void ContainsAssemblyReference_ReturnsTrue_ForA2v10AssemblyReference()
    {
        const string xaml = "<Page xmlns:ui=\"clr-namespace:A2V10.Test;assembly=A2v10.Xaml\" />";

        Assert.True(XamlAssemblyReferenceDetector.ContainsAssemblyReference(xaml, "A2v10.Xaml"));
    }

    [Fact]
    public void ContainsAssemblyReference_ReturnsTrue_WhenWhitespaceSurroundsEquals()
    {
        const string xaml = "<Page xmlns:ui=\"clr-namespace:A2V10.Test;assembly = A2v10.Xaml\" />";

        Assert.True(XamlAssemblyReferenceDetector.ContainsAssemblyReference(xaml, "A2v10.Xaml"));
    }

    [Fact]
    public void ContainsAssemblyReference_ReturnsFalse_ForOtherAssembly()
    {
        const string xaml = "<Page xmlns:ui=\"clr-namespace:A2V10.Test;assembly=Other.Xaml\" />";

        Assert.False(XamlAssemblyReferenceDetector.ContainsAssemblyReference(xaml, "A2v10.Xaml"));
    }
}

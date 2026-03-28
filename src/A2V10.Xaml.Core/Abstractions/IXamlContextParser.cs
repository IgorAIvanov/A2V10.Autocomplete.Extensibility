using A2V10.Xaml.Core.Models;

namespace A2V10.Xaml.Core.Abstractions;

public interface IXamlContextParser
{
    XamlCompletionContext Parse(string text, int position);
}

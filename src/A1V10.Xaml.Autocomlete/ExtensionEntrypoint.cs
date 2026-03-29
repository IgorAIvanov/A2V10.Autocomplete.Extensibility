using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

namespace A1V10.Xaml.Autocomlete
{
    /// <summary>
    /// Extension entrypoint for the VisualStudio.Extensibility extension.
    /// </summary>
    [VisualStudioContribution]
    internal class ExtensionEntrypoint : Extension
    {
        /// <inheritdoc/>
        public override ExtensionConfiguration ExtensionConfiguration => new()
        {
            Metadata = new(
                    id: "A2V10.Xaml.Autocomlete.07efd1de-bc19-42a2-8b69-dc289ce090f2",
                    version: this.ExtensionAssemblyVersion,
                    publisherName: "Igor Ivanov",
                    displayName: "A2V10.Xaml.Autocomlete",
                    description: "Extension description"),
        };

        /// <inheritdoc />
        protected override void InitializeServices(IServiceCollection serviceCollection)
        {
            base.InitializeServices(serviceCollection);

            // You can configure dependency injection here by adding services to the serviceCollection.
        }
    }
}

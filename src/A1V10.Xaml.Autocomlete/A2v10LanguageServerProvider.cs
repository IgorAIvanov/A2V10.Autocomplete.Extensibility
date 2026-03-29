using System.IO.Pipelines;
using System.Diagnostics;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.LanguageServer;
using Microsoft.VisualStudio.RpcContracts.LanguageServerProvider;
using Nerdbank.Streams;
using Microsoft.VisualStudio.Extensibility.Editor;

namespace A1V10.Xaml.Autocomlete;

#pragma warning disable VSEXTPREVIEW_LSP // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[VisualStudioContribution]
internal sealed class A2v10LanguageServerProvider : LanguageServerProvider
{
    private Process? _languageServerProcess;

    [VisualStudioContribution]
    public static DocumentTypeConfiguration XamlDocumentType => new("a2v10-xaml")
    {
        FileExtensions = [".xaml"],
        BaseDocumentType = LanguageServerBaseDocumentType,
    };

    public override LanguageServerProviderConfiguration LanguageServerProviderConfiguration => new(
        "A2V10 XAML",
        [DocumentFilter.FromDocumentType(XamlDocumentType)]);
 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    public override Task<IDuplexPipe?> CreateServerConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = CreateStartInfo();
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start A2V10.Xaml.LanguageServer process.");

        _languageServerProcess = process;
        var stream = FullDuplexStream.Splice(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
        var pipe = stream.UsePipe(cancellationToken: cancellationToken);
        return Task.FromResult<IDuplexPipe?>(pipe);
    }

    public override Task OnServerInitializationResultAsync(
        ServerInitializationResult serverInitializationResult,
        LanguageServerInitializationFailureInfo? initializationFailureInfo,
        CancellationToken cancellationToken)
    {
        if (serverInitializationResult != ServerInitializationResult.Succeeded)
        {
            TryStopLanguageServerProcess();
        }

        return base.OnServerInitializationResultAsync(serverInitializationResult, initializationFailureInfo, cancellationToken);
    }

    private static ProcessStartInfo CreateStartInfo()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var serverDirectory = Path.Combine(baseDirectory, "LanguageServer");
        var serverExePath = Path.Combine(serverDirectory, "A2V10.Xaml.LanguageServer.exe");
        var serverDllPath = Path.Combine(serverDirectory, "A2V10.Xaml.LanguageServer.dll");

        ProcessStartInfo startInfo;
        if (File.Exists(serverExePath))
        {
            startInfo = new ProcessStartInfo(serverExePath);
        }
        else if (File.Exists(serverDllPath))
        {
            startInfo = new ProcessStartInfo("dotnet");
            startInfo.ArgumentList.Add(serverDllPath);
        }
        else
        {
            throw new FileNotFoundException("Unable to find packaged A2V10.Xaml.LanguageServer executable.", serverDllPath);
        }

        startInfo.ArgumentList.Add("--stdio");
        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        return startInfo;
    }

    private void TryStopLanguageServerProcess()
    {
        try
        {
            if (_languageServerProcess is { HasExited: false })
            {
                _languageServerProcess.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }
}
#pragma warning restore VSEXTPREVIEW_LSP
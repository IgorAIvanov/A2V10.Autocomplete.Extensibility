namespace A2V10.Xaml.Reflection;

internal static class DiagnosticLog
{
    private const string EnvironmentVariableName = "A2V10_XAML_TRACE";
    private static readonly bool Enabled = ReadEnabled();

    public static void Info(string message)
        => Write("INFO", message);

    public static void Error(string message, Exception exception)
        => Write("ERROR", $"{message} Exception='{exception.Message}'");

    private static void Write(string level, string message)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            Console.Error.WriteLine($"[A2V10.Xaml.Reflection][{level}] {DateTime.UtcNow:O} {message}");
        }
        catch
        {
        }
    }

    private static bool ReadEnabled()
    {
        var value = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        return value is not null &&
            (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase));
    }
}

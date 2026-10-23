namespace OrisJewelCloudExport;

/// <summary>Minimal console logging: progress to stdout, warnings and errors to stderr.</summary>
internal static class Log
{
    public static void Info(string message) => Console.WriteLine(message);

    public static void Warn(string message) => Console.Error.WriteLine($"WARN  {message}");

    public static void Error(string message) => Console.Error.WriteLine($"ERROR {message}");
}

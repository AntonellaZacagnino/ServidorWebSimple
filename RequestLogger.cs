namespace SimpleHttpServer;

public static class RequestLogger
{
    private static readonly object FileLock = new();
    private static string _logFolder = "logs";

    public static void Init(string logFolder)
    {
        _logFolder = logFolder;
        Directory.CreateDirectory(_logFolder);
    }

    public static void Log(string clientIp, string message)
    {
        var fileName = $"log-{DateTime.Now:yyyy-MM-dd}.txt";
        var fullPath = Path.Combine(_logFolder, fileName);
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [IP:{clientIp}] {message}";

        lock (FileLock)
        {
            File.AppendAllText(fullPath, line + Environment.NewLine);
        }
    }
}

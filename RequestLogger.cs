namespace SimpleHttpServer;

/// <summary>
/// Logger simple y thread-safe que escribe un archivo de log por día
/// (Requisito 9). Cada línea incluye marca de tiempo e IP de origen.
/// Como múltiples solicitudes se atienden en paralelo (Requisito 1),
/// se usa un lock para evitar escrituras concurrentes corruptas.
/// </summary>
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

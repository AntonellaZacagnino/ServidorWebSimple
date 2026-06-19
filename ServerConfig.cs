using System.Text.Json;

namespace SimpleHttpServer;

/// <summary>
/// Representa la configuración externa del servidor (Requisitos 3 y 4).
/// Se carga desde un archivo JSON ubicado junto al ejecutable, de modo que
/// el puerto y la carpeta de archivos puedan cambiarse sin recompilar.
/// </summary>
public class ServerConfig
{
    public int Port { get; set; } = 8080;
    public string RootFolder { get; set; } = "wwwroot";
    public string LogFolder { get; set; } = "logs";
    public string DefaultDocument { get; set; } = "index.html";
    public string NotFoundDocument { get; set; } = "404.html";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Carga la configuración desde <paramref name="path"/>. Si el archivo no existe,
    /// crea uno con valores por defecto para facilitar la primera ejecución.
    /// </summary>
    public static ServerConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var defaultConfig = new ServerConfig();
            File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, JsonOptions));
            Console.WriteLine($"[Config] No se encontró '{path}'. Se generó uno con valores por defecto.");
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<ServerConfig>(json, JsonOptions);
            return config ?? new ServerConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Config] Error al leer '{path}': {ex.Message}. Se usarán valores por defecto.");
            return new ServerConfig();
        }
    }
}

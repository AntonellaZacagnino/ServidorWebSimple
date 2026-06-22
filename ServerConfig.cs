using System.Text.Json;

namespace SimpleHttpServer;

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

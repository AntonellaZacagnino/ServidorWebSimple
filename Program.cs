namespace SimpleHttpServer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // El archivo de configuración puede pasarse por argumento; si no, se usa config.json
        // ubicado en el directorio de trabajo (Requisitos 3 y 4: puerto y carpeta configurables externamente).
        var configPath = args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "config.json");
        var config = ServerConfig.Load(configPath);

        // Si las rutas configuradas son relativas, se resuelven respecto al directorio de trabajo
        if (!Path.IsPathRooted(config.RootFolder))
        {
            config.RootFolder = Path.Combine(Directory.GetCurrentDirectory(), config.RootFolder);
        }
        if (!Path.IsPathRooted(config.LogFolder))
        {
            config.LogFolder = Path.Combine(Directory.GetCurrentDirectory(), config.LogFolder);
        }

        RequestLogger.Init(config.LogFolder);

        var server = new HttpServer(config);
        await server.RunAsync();
    }
}

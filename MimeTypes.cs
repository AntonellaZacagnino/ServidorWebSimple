namespace SimpleHttpServer;

/// <summary>
/// Resuelve el encabezado Content-Type según la extensión del archivo solicitado.
/// </summary>
public static class MimeTypes
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".htm"] = "text/html; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".js"] = "application/javascript; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".txt"] = "text/plain; charset=utf-8",
        [".xml"] = "application/xml; charset=utf-8",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".svg"] = "image/svg+xml",
        [".ico"] = "image/x-icon",
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
    };

    public static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return Map.TryGetValue(ext, out var contentType) ? contentType : "application/octet-stream";
    }
}

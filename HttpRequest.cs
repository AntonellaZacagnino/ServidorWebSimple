namespace SimpleHttpServer;

public class HttpRequest
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "/";
    public string RawTarget { get; set; } = "/";
    public string Version { get; set; } = "HTTP/1.1";
    public Dictionary<string, string> QueryParams { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Body { get; set; } = "";
}

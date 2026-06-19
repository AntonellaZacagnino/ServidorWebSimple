using System.Text;

namespace SimpleHttpServer;

/// <summary>
/// Parsea manualmente una solicitud HTTP/1.x leída byte a byte desde el socket.
/// Cubre: método, ruta, parámetros de query string (Req. 7), encabezados y,
/// para POST, el cuerpo según Content-Length (Req. 6). No se usa ninguna
/// librería HTTP de alto nivel (Req. 10).
/// </summary>
public static class HttpRequestParser
{
    public static HttpRequest? Parse(HttpStreamReader reader)
    {
        var requestLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return null; // conexión vacía / cerrada
        }

        // Ej: "GET /index.html?nombre=Anto HTTP/1.1"
        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null; // línea de pedido malformada
        }

        var request = new HttpRequest
        {
            Method = parts[0].ToUpperInvariant(),
            RawTarget = parts[1],
            Version = parts.Length >= 3 ? parts[2] : "HTTP/1.1"
        };

        ParseTarget(request);

        // --- Encabezados ---
        string? headerLine;
        while (!string.IsNullOrEmpty(headerLine = reader.ReadLine()))
        {
            var sepIndex = headerLine.IndexOf(':');
            if (sepIndex <= 0) continue;

            var name = headerLine[..sepIndex].Trim();
            var value = headerLine[(sepIndex + 1)..].Trim();
            request.Headers[name] = value;
        }

        // --- Body (solo si corresponde, según Content-Length) ---
        if (request.Headers.TryGetValue("Content-Length", out var lenStr) &&
            int.TryParse(lenStr, out var contentLength) && contentLength > 0)
        {
            var bodyBytes = reader.ReadExact(contentLength);
            request.Body = Encoding.UTF8.GetString(bodyBytes);
        }

        return request;
    }

    private static void ParseTarget(HttpRequest request)
    {
        var target = request.RawTarget;
        var queryIndex = target.IndexOf('?');

        if (queryIndex >= 0)
        {
            request.Path = target[..queryIndex];
            var queryString = target[(queryIndex + 1)..];
            request.QueryParams = ParseQueryString(queryString);
        }
        else
        {
            request.Path = target;
        }

        if (string.IsNullOrEmpty(request.Path))
        {
            request.Path = "/";
        }
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return result;

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            string key, value;
            if (eq >= 0)
            {
                key = Uri.UnescapeDataString(pair[..eq].Replace('+', ' '));
                value = Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
            }
            else
            {
                key = Uri.UnescapeDataString(pair.Replace('+', ' '));
                value = "";
            }
            result[key] = value;
        }

        return result;
    }
}

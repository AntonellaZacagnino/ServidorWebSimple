using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleHttpServer;

public class HttpServer
{
    private readonly ServerConfig _config;
    private readonly string _rootFullPath;

    public HttpServer(ServerConfig config) // metodo que toma la configuracion personalizada
    {
        _config = config;
        Directory.CreateDirectory(_config.RootFolder);
        _rootFullPath = Path.GetFullPath(_config.RootFolder);
    }

    public async Task RunAsync() //capa de transporte
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, _config.Port));
        listener.Listen(100);

        Console.WriteLine($"[SimpleHttpServer] Escuchando en el puerto {_config.Port}.");
        Console.WriteLine($"[SimpleHttpServer] Carpeta raíz de archivos: '{_rootFullPath}'.");
        Console.WriteLine($"[SimpleHttpServer] Logs en: '{Path.GetFullPath(_config.LogFolder)}'.");
        Console.WriteLine("[SimpleHttpServer] Presione Ctrl+C para detener.");

        // Cada conexión se atiende en su propio hilo del ThreadPool: concurrencia indefinida (Req. 1)
        while (true)
        {
            var clientSocket = await listener.AcceptAsync();            
            _ = Task.Run(() => HandleClient(clientSocket));
        }
    }

    private void HandleClient(Socket clientSocket) //capa controlador de solicitudes
    {
        var clientIp = "unknown";

        try
        {
            if (clientSocket.RemoteEndPoint is IPEndPoint endpoint)
            {
                clientIp = endpoint.Address.ToString();
            }

            using var stream = new NetworkStream(clientSocket);
            var reader = new HttpStreamReader(stream);
            var request = HttpRequestParser.Parse(reader);

            if (request == null)
            {
                return; // conexión vacía o cerrada por el cliente antes de enviar datos
            }

            // Requisito 7: los parámetros de query string sólo se loguean
            if (request.QueryParams.Count > 0)
            {
                var queryLog = string.Join(", ", request.QueryParams.Select(kv => $"{kv.Key}={kv.Value}"));
                RequestLogger.Log(clientIp, $"Query params en '{request.Path}' -> {queryLog}");
            }

            // Requisito 6: para POST, sólo se loguean los datos recibidos
            if (request.Method == "POST")
            {
                RequestLogger.Log(clientIp, $"POST body recibido en '{request.Path}' -> {request.Body}");
                var bodyBytes = Encoding.UTF8.GetBytes($"POST recibido: {request.Body}");
                SendResponse(stream, 200, "OK", "text/plain; charset=utf-8", bodyBytes, false);
                return;
            }           

            ServeFile(stream, request, clientIp);
        }

        catch (Exception ex)
        {
            RequestLogger.Log(clientIp, $"ERROR procesando la solicitud: {ex.Message}");
        }

        finally
        {
            try { clientSocket.Shutdown(SocketShutdown.Both); } catch { }
            clientSocket.Close();
        }
    }    

    private void ServeFile(NetworkStream stream, HttpRequest request, string clientIp)
    {
        var isGzip = AcceptsGzip(request);

        // Requisito 2: si la URL no especifica archivo (path "/"), se sirve el documento por defecto
        var decodedPath = Uri.UnescapeDataString(request.Path);
        var requestedPath = decodedPath == "/" ? "/" + _config.DefaultDocument : decodedPath;

        var combinedPath = Path.GetFullPath(Path.Combine(_rootFullPath, requestedPath.TrimStart('/')));

        // Medida de seguridad: el archivo resuelto debe permanecer dentro de la carpeta raíz configurada
        // (evita acceder a archivos arbitrarios del sistema mediante "../" en la URL).
        var rootWithSeparator = _rootFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                 + Path.DirectorySeparatorChar;
        var staysInsideRoot = combinedPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(combinedPath, _rootFullPath, StringComparison.OrdinalIgnoreCase);

        if (!staysInsideRoot || !File.Exists(combinedPath))
        {
            Send404(stream, request, clientIp, isGzip);
            return;
        }

        byte[] fileBytes;
        try
        {
            fileBytes = File.ReadAllBytes(combinedPath);
        }
        catch (IOException)
        {
            Send404(stream, request, clientIp, isGzip);
            return;
        }

        var contentType = MimeTypes.GetContentType(combinedPath);
        RequestLogger.Log(clientIp,
            $"{request.Method} {request.RawTarget} -> 200 OK ({fileBytes.Length} bytes, {contentType}, gzip={isGzip})");

        SendResponse(stream, 200, "OK", contentType, fileBytes, isGzip);
    }

    // Requisito 5: 404 con documento personalizado cuando el archivo solicitado no existe
    private void Send404(NetworkStream stream, HttpRequest request, string clientIp, bool isGzip)
    {
        var notFoundPath = Path.Combine(_rootFullPath, _config.NotFoundDocument);
        byte[] body;
        string contentType;

        if (File.Exists(notFoundPath))
        {
            body = File.ReadAllBytes(notFoundPath);
            contentType = MimeTypes.GetContentType(notFoundPath);
        }
        else
        {
            // Resguardo por si el 404.html configurado no existe en la carpeta raíz
            body = Encoding.UTF8.GetBytes("<html><body><h1>404 - Recurso no encontrado</h1></body></html>");
            contentType = "text/html; charset=utf-8";
        }

        RequestLogger.Log(clientIp, $"{request.Method} {request.RawTarget} -> 404 Not Found");
        SendResponse(stream, 404, "Not Found", contentType, body, isGzip);
    }

    // Requisito 8: compresión de la respuesta cuando el cliente la acepta (Accept-Encoding: gzip)
    private static void SendResponse(NetworkStream stream, int statusCode, string statusText, string contentType, byte[] bodyBytes, bool useGzip)
    {
        var finalBody = bodyBytes;
        string? contentEncoding = null;

        if (useGzip)
        {
            using var compressedStream = new MemoryStream();
            using (var gzip = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(bodyBytes, 0, bodyBytes.Length);
            }
            finalBody = compressedStream.ToArray();
            contentEncoding = "gzip";
        }

        var header = new StringBuilder();
        header.Append($"HTTP/1.1 {statusCode} {statusText}\r\n");
        header.Append($"Content-Type: {contentType}\r\n");
       
        if (contentEncoding != null)
        {
            header.Append($"Content-Encoding: {contentEncoding}\r\n");
        }
       
        header.Append($"Content-Length: {finalBody.Length}\r\n");
        header.Append("Connection: close\r\n");
        header.Append("Server: SimpleHttpServer-NET\r\n");
        header.Append("\r\n");

        var headerBytes = Encoding.ASCII.GetBytes(header.ToString());
        stream.Write(headerBytes, 0, headerBytes.Length);
        stream.Write(finalBody, 0, finalBody.Length);
        stream.Flush();
    }

    private static bool AcceptsGzip(HttpRequest request)
    {
        return request.Headers.TryGetValue("Accept-Encoding", out var encoding) &&
               encoding.Contains("gzip", StringComparison.OrdinalIgnoreCase);
    }
}

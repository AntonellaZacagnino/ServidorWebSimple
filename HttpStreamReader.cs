using System.Text;

namespace SimpleHttpServer;

/// <summary>
/// Envoltorio mínimo sobre el <see cref="Stream"/> crudo del socket que permite leer
/// líneas de texto (terminadas en CRLF) y bloques de bytes exactos.
/// No usamos StreamReader porque su buffering interno consumiría bytes del
/// cuerpo (body) de la solicitud al leer los encabezados, rompiendo el parseo manual
/// exigido por el Requisito 10 (sólo sockets, parseo HTTP propio).
/// </summary>
public class HttpStreamReader
{
    private readonly Stream _stream;

    public HttpStreamReader(Stream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Lee una línea terminada en \n (tolerando \r\n o \n solo).
    /// Devuelve null si el stream se cerró sin datos (conexión finalizada).
    /// </summary>
    public string? ReadLine()
    {
        var bytes = new List<byte>();
        int b;
        bool any = false;

        while ((b = _stream.ReadByte()) != -1)
        {
            any = true;
            if (b == '\n')
            {
                break;
            }
            if (b != '\r')
            {
                bytes.Add((byte)b);
            }
        }

        if (!any) return null; // conexión cerrada sin enviar nada
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    /// Lee exactamente <paramref name="count"/> bytes (usado para el body, según Content-Length).
    /// </summary>
    public byte[] ReadExact(int count)
    {
        if (count <= 0) return Array.Empty<byte>();

        var buffer = new byte[count];
        var read = 0;
        while (read < count)
        {
            var r = _stream.Read(buffer, read, count - read);
            if (r == 0) break; // el cliente cerró la conexión antes de tiempo
            read += r;
        }

        return read == count ? buffer : buffer[..read];
    }
}

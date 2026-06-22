using System.Text;

namespace SimpleHttpServer;

public class HttpStreamReader
{
    private readonly Stream _stream;

    public HttpStreamReader(Stream stream)
    {
        _stream = stream;
    }

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

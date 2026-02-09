using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TeleprompterApp.Osc;

internal abstract class OscPacket
{
    public static OscPacket? Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            return null;
        }

        if (StartsWith(data, "#bundle"))
        {
            return OscBundle.TryParse(data);
        }

        return OscMessage.TryParse(data);
    }

    private static bool StartsWith(ReadOnlySpan<byte> data, string literal)
    {
        if (data.Length < literal.Length)
        {
            return false;
        }

        for (var i = 0; i < literal.Length; i++)
        {
            if (data[i] != (byte)literal[i])
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed class OscMessage : OscPacket
{
    public OscMessage(string address, IReadOnlyList<object> arguments)
    {
        Address = address;
        Arguments = arguments;
    }

    public string Address { get; }

    public IReadOnlyList<object> Arguments { get; }

    public static OscMessage? TryParse(ReadOnlySpan<byte> data)
    {
        try
        {
            var reader = new OscReader(data);
            var address = reader.ReadString();
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            var tags = reader.ReadString();
            if (string.IsNullOrEmpty(tags) || tags[0] != ',')
            {
                return new OscMessage(address, Array.Empty<object>());
            }

            var arguments = new List<object>(tags.Length - 1);
            for (var i = 1; i < tags.Length; i++)
            {
                var tag = tags[i];
                switch (tag)
                {
                    case 'i':
                        arguments.Add(reader.ReadInt32());
                        break;
                    case 'h':
                        arguments.Add(reader.ReadInt64());
                        break;
                    case 'f':
                        arguments.Add(reader.ReadFloat());
                        break;
                    case 'd':
                        arguments.Add(reader.ReadDouble());
                        break;
                    case 's':
                        arguments.Add(reader.ReadString());
                        break;
                    case 'T':
                        arguments.Add(true);
                        break;
                    case 'F':
                        arguments.Add(false);
                        break;
                    case 'N':
                        arguments.Add(null!);
                        break;
                    case 'b':
                        arguments.Add(reader.ReadBlob());
                        break;
                    default:
                        // Skip unsupported argument types gracefully
                        reader.SkipArgument(tag);
                        break;
                }
            }

            return new OscMessage(address, arguments);
        }
        catch
        {
            return null;
        }
    }

    public byte[] ToByteArray()
    {
        using var stream = new MemoryStream();
        var writer = new OscWriter(stream);
        writer.WriteString(Address);

        var tags = new StringBuilder(",");
        foreach (var argument in Arguments)
        {
            tags.Append(OscWriter.GetTypeTag(argument));
        }

        writer.WriteString(tags.ToString());
        foreach (var argument in Arguments)
        {
            writer.WriteArgument(argument);
        }

        return stream.ToArray();
    }
}

internal sealed class OscBundle : OscPacket
{
    private const string BundleTag = "#bundle";

    public OscBundle(IEnumerable<OscPacket> packets)
    {
        Packets = packets.ToList();
    }

    public IReadOnlyList<OscPacket> Packets { get; }

    public static OscBundle? TryParse(ReadOnlySpan<byte> data)
    {
        try
        {
            var reader = new OscReader(data);
            var tag = reader.ReadString();
            if (!string.Equals(tag, BundleTag, StringComparison.Ordinal))
            {
                return null;
            }

            // Skip the 8-byte timetag (we do not use it)
            reader.Skip(8);

            var packets = new List<OscPacket>();
            while (!reader.EndOfStream)
            {
                var size = reader.ReadInt32();
                if (size <= 0 || reader.Remaining < size)
                {
                    break;
                }

                var slice = reader.ReadSlice(size);
                var child = OscPacket.Parse(slice);
                if (child != null)
                {
                    packets.Add(child);
                }
            }

            return new OscBundle(packets);
        }
        catch
        {
            return null;
        }
    }
}

internal ref struct OscReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _offset;

    public OscReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _offset = 0;
    }

    public bool EndOfStream => _offset >= _buffer.Length;

    public int Remaining => _buffer.Length - _offset;

    public void Skip(int byteCount)
    {
        _offset = Math.Min(_buffer.Length, _offset + byteCount);
    }

    public ReadOnlySpan<byte> ReadSlice(int length)
    {
        var slice = _buffer.Slice(_offset, length);
        _offset += length;
        return slice;
    }

    public string ReadString()
    {
        if (_offset >= _buffer.Length)
        {
            return string.Empty;
        }

        var start = _offset;
        while (_offset < _buffer.Length && _buffer[_offset] != 0)
        {
            _offset++;
        }

        var str = Encoding.UTF8.GetString(_buffer.Slice(start, _offset - start));

        // Skip null terminator
        if (_offset < _buffer.Length)
        {
            _offset++;
        }

        AlignToFourBytes();
        return str;
    }

    public int ReadInt32()
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(_buffer.Slice(_offset, 4));
        _offset += 4;
        return value;
    }

    public long ReadInt64()
    {
        var value = BinaryPrimitives.ReadInt64BigEndian(_buffer.Slice(_offset, 8));
        _offset += 8;
        return value;
    }

    public float ReadFloat()
    {
        var value = BinaryPrimitives.ReadSingleBigEndian(_buffer.Slice(_offset, 4));
        _offset += 4;
        return value;
    }

    public double ReadDouble()
    {
        var value = BinaryPrimitives.ReadDoubleBigEndian(_buffer.Slice(_offset, 8));
        _offset += 8;
        return value;
    }

    public byte[] ReadBlob()
    {
        var length = ReadInt32();
        var blob = _buffer.Slice(_offset, length).ToArray();
        _offset += length;
        AlignToFourBytes();
        return blob;
    }

    public void SkipArgument(char typeTag)
    {
        switch (typeTag)
        {
            case 'i':
            case 'f':
                Skip(4);
                break;
            case 'h':
            case 'd':
                Skip(8);
                break;
            case 's':
            case 'S':
                ReadString();
                break;
            case 'b':
                ReadBlob();
                break;
            default:
                break;
        }
    }

    private void AlignToFourBytes()
    {
        while (_offset % 4 != 0 && _offset < _buffer.Length)
        {
            _offset++;
        }
    }
}

internal sealed class OscWriter
{
    private readonly Stream _stream;
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    public OscWriter(Stream stream)
    {
        _stream = stream;
    }

    public void WriteString(string value)
    {
        var bytes = Utf8.GetBytes(value);
        _stream.Write(bytes, 0, bytes.Length);
        _stream.WriteByte(0);
        WritePadding(bytes.Length + 1);
    }

    public void WriteArgument(object value)
    {
        switch (value)
        {
            case null:
                break;
            case bool b:
                // Boolean values are encoded only via their type tag and carry no body
                break;
            case int i:
                Span<byte> buffer = stackalloc byte[4];
                BinaryPrimitives.WriteInt32BigEndian(buffer, i);
                _stream.Write(buffer);
                break;
            case long l:
                Span<byte> longBuffer = stackalloc byte[8];
                BinaryPrimitives.WriteInt64BigEndian(longBuffer, l);
                _stream.Write(longBuffer);
                break;
            case float f:
                Span<byte> floatBuffer = stackalloc byte[4];
                BinaryPrimitives.WriteSingleBigEndian(floatBuffer, f);
                _stream.Write(floatBuffer);
                break;
            case double d:
                Span<byte> doubleBuffer = stackalloc byte[8];
                BinaryPrimitives.WriteDoubleBigEndian(doubleBuffer, d);
                _stream.Write(doubleBuffer);
                break;
            case string s:
                WriteString(s);
                break;
            case byte[] blob:
                WriteBlob(blob);
                break;
            default:
                // Fallback to string representation
                WriteString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                break;
        }
    }

    public void WriteBlob(byte[] data)
    {
        Span<byte> lengthBuffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, data.Length);
        _stream.Write(lengthBuffer);
        _stream.Write(data, 0, data.Length);
        WritePadding(data.Length);
    }

    private void WritePadding(int originalLength)
    {
        var padding = (4 - (originalLength % 4)) % 4;
        for (var i = 0; i < padding; i++)
        {
            _stream.WriteByte(0);
        }
    }

    public static char GetTypeTag(object value)
    {
        return value switch
        {
            null => 'N',
            bool b => b ? 'T' : 'F',
            int => 'i',
            long => 'h',
            float => 'f',
            double => 'd',
            byte[] => 'b',
            _ => 's',
        };
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

if (args.Length < 2)
{
    Console.WriteLine("Uso: PngToIco <input.png|webp> <output.ico>");
    return 1;
}

var inputPath = args[0];
var icoPath = args[1];

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"File non trovato: {inputPath}");
    return 1;
}

var outDir = Path.GetDirectoryName(icoPath);
if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
    Directory.CreateDirectory(outDir);

var sizes = new[] { 256, 128, 64, 48, 32, 16 };
var images = new List<byte[]>();

using (var source = Image.Load<Rgba32>(inputPath))
{
    foreach (var size in sizes)
    {
        using var resized = source.Clone(x => x.Resize(size, size));
        using var ms = new MemoryStream();
        resized.SaveAsPng(ms);
        images.Add(ms.ToArray());
    }
}

using var icoStream = new MemoryStream();
using var writer = new BinaryWriter(icoStream);

writer.Write((short)0);
writer.Write((short)1);
writer.Write((short)sizes.Length);

var offset = 6 + 16 * sizes.Length;

for (var i = 0; i < sizes.Length; i++)
{
    var size = sizes[i];
    var pngBytes = images[i];
    writer.Write((byte)(size == 256 ? 0 : size));
    writer.Write((byte)(size == 256 ? 0 : size));
    writer.Write((byte)0);
    writer.Write((byte)0);
    writer.Write((short)1);
    writer.Write((short)32);
    writer.Write(pngBytes.Length);
    writer.Write(offset);
    offset += pngBytes.Length;
}

foreach (var img in images)
    writer.Write(img);

File.WriteAllBytes(icoPath, icoStream.ToArray());
return 0;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

var outDir = args.Length > 0 ? args[0] : "Assets";
Directory.CreateDirectory(outDir);
var outPath = Path.Combine(outDir, "dep3.ico");
SaveIco(outPath, [16, 32, 48, 64, 128, 256]);
Console.WriteLine($"Wrote {outPath}");

static void SaveIco(string path, int[] sizes)
{
    using var stream = File.Create(path);
    using var writer = new BinaryWriter(stream);

    writer.Write((short)0);
    writer.Write((short)1);
    writer.Write((short)sizes.Length);

    var offset = 6 + 16 * sizes.Length;
    var images = new byte[sizes.Length][];

    for (var i = 0; i < sizes.Length; i++)
    {
        using var bmp = Render(sizes[i]);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        images[i] = ms.ToArray();
    }

    for (var i = 0; i < sizes.Length; i++)
    {
        var size = sizes[i];
        var data = images[i];
        writer.Write((byte)(size >= 256 ? 0 : size));
        writer.Write((byte)(size >= 256 ? 0 : size));
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(data.Length);
        writer.Write(offset);
        offset += data.Length;
    }

    foreach (var data in images)
        writer.Write(data);
}

static Bitmap Render(int size)
{
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.CompositingQuality = CompositingQuality.HighQuality;
    g.Clear(Color.Transparent);

    var margin = size * 0.12f;
    var rect = new RectangleF(margin, margin, size - margin * 2, size - margin * 2);
    using (var fill = new SolidBrush(Color.FromArgb(46, 160, 120)))
        g.FillEllipse(fill, rect);

    using (var border = new Pen(Color.FromArgb(30, 110, 82), Math.Max(1f, size / 64f)))
        g.DrawEllipse(border, rect);

    var fontSize = Math.Max(6f, size * 0.28f);
    using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
    using var text = new SolidBrush(Color.FromArgb(236, 236, 236));
    var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
    g.DrawString("D3", font, text, new RectangleF(0, 0, size, size), format);
    return bmp;
}

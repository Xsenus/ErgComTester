using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MicroluxErgConnect.Branding;

internal static class AppBranding
{
    private const string IconResourceName = "MicroluxErgConnect.Resources.microlux-connect.ico";
    private const string ImageResourceName = "MicroluxErgConnect.Resources.microlux-connect.png";

    private static readonly Lazy<Icon> IconSource = new(LoadIcon);
    private static readonly Lazy<Image> HeaderImageSource = new(LoadImage);

    public static Icon CreateWindowIcon()
    {
        return CloneIcon();
    }

    public static Icon CreateTrayIcon()
    {
        return CloneIcon();
    }

    public static Image GetHeaderImage()
    {
        return (Image)HeaderImageSource.Value.Clone();
    }

    private static Icon CloneIcon()
    {
        return (Icon)IconSource.Value.Clone();
    }

    private static Icon LoadIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(IconResourceName);

        if (stream is not null)
        {
            using var icon = new Icon(stream);
            return (Icon)icon.Clone();
        }

        return CreateFallbackIcon();
    }

    private static Image LoadImage()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ImageResourceName);

        if (stream is not null)
        {
            using var bitmap = Image.FromStream(stream);
            return (Image)bitmap.Clone();
        }

        return CreateFallbackHeaderImage();
    }

    private static Icon CreateFallbackIcon()
    {
        using var bitmap = new Bitmap(256, 256);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.FromArgb(26, 38, 55));

            using var accentBrush = new SolidBrush(Color.FromArgb(255, 94, 179, 255));
            using var pen = new Pen(Color.FromArgb(255, 255, 139, 56), 12f) { Alignment = PenAlignment.Inset };
            graphics.DrawEllipse(pen, new Rectangle(18, 18, 220, 220));

            using var font = new Font("Segoe UI Semibold", 120, FontStyle.Bold, GraphicsUnit.Pixel);
            var text = "M";
            var size = graphics.MeasureString(text, font);
            graphics.DrawString(text, font, accentBrush, (bitmap.Width - size.Width) / 2f, (bitmap.Height - size.Height) / 2f - 6f);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static Image CreateFallbackHeaderImage()
    {
        var bitmap = new Bitmap(360, 160);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(Point.Empty, bitmap.Size);
        using (var background = new LinearGradientBrush(rect, Color.FromArgb(30, 45, 65), Color.FromArgb(18, 27, 39), LinearGradientMode.Horizontal))
        {
            graphics.FillRectangle(background, rect);
        }

        using (var accentBrush = new LinearGradientBrush(rect, Color.FromArgb(255, 139, 56), Color.FromArgb(94, 179, 255), LinearGradientMode.ForwardDiagonal))
        {
            graphics.FillPolygon(accentBrush, new[]
            {
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Right - 120, rect.Bottom)
            });
        }

        using var titleFont = new Font("Segoe UI Semibold", 44, FontStyle.Bold, GraphicsUnit.Pixel);
        using var subtitleFont = new Font("Segoe UI", 18, FontStyle.Regular, GraphicsUnit.Pixel);
        using var whiteBrush = new SolidBrush(Color.White);

        const string title = "Microlux";
        const string subtitle = "ERG-Connect";

        var titleSize = graphics.MeasureString(title, titleFont);
        graphics.DrawString(title, titleFont, whiteBrush, 24f, 44f - titleSize.Height / 2f);
        graphics.DrawString(subtitle, subtitleFont, whiteBrush, 28f, 96f);

        return bitmap;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint hIcon);
}

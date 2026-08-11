using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DpiTray;

internal static class IconFactory
{
    public static Icon CreateStatusIcon(bool running)
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var fill = running ? Color.FromArgb(40, 167, 69) : Color.FromArgb(220, 53, 69);
            var border = running ? Color.FromArgb(20, 120, 45) : Color.FromArgb(160, 30, 45);

            using var brush = new SolidBrush(fill);
            using var pen = new Pen(border, 2f);
            g.FillEllipse(brush, 3, 3, 26, 26);
            g.DrawEllipse(pen, 3, 3, 26, 26);

            using var textBrush = new SolidBrush(Color.White);
            using var font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel);
            var label = running ? "ON" : "OFF";
            var size = g.MeasureString(label, font);
            g.DrawString(label, font, textBrush, (32 - size.Width) / 2f, (32 - size.Height) / 2f);
        }

        var hIcon = bmp.GetHicon();
        using var tmp = Icon.FromHandle(hIcon);
        return (Icon)tmp.Clone();
    }
}

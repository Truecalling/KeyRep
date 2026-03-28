using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace KeyRep
{
    internal static class BrandAssets
    {
        private const string EmbeddedLogoName = "KeyRep.key_logo.png";
        private const string LogoFileName = "key_logo.png";

        public static Image? TryLoadLogoPng(int maxEdgePixels)
        {
            var embedded = TryLoadFromEmbedded(maxEdgePixels);
            if (embedded != null)
                return embedded;

            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(dir))
                return null;
            var path = Path.Combine(dir, LogoFileName);
            if (!File.Exists(path))
                return null;

            try
            {
                using var fs = File.OpenRead(path);
                using var src = Image.FromStream(fs, false, false);
                var bmp = new Bitmap(src);
                return ResizeToMaxEdge(bmp, maxEdgePixels);
            }
            catch
            {
                return null;
            }
        }

        private static Image? TryLoadFromEmbedded(int maxEdgePixels)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream(EmbeddedLogoName))
                {
                    if (s != null)
                    {
                        using var src = Image.FromStream(s, false, false);
                        var bmp = new Bitmap(src);
                        return ResizeToMaxEdge(bmp, maxEdgePixels);
                    }
                }

                foreach (var n in asm.GetManifestResourceNames() ?? Array.Empty<string>())
                {
                    if (!n.EndsWith("key_logo.png", StringComparison.OrdinalIgnoreCase))
                        continue;
                    using var s2 = asm.GetManifestResourceStream(n);
                    if (s2 == null)
                        continue;
                    using var src = Image.FromStream(s2, false, false);
                    var bmp = new Bitmap(src);
                    return ResizeToMaxEdge(bmp, maxEdgePixels);
                }
            }
            catch
            {
                /* */
            }

            return null;
        }

        public static Image CreateFallbackToolbarIcon()
        {
            const int s = 36;
            var bmp = new Bitmap(s, s);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(28, 32, 40));
                using var pen = new Pen(Color.Goldenrod, 2.2f);
                g.DrawRectangle(pen, 6, 10, 24, 18);
                g.FillEllipse(Brushes.Goldenrod, 14, 14, 8, 8);
            }

            return bmp;
        }

        private static Image ResizeToMaxEdge(Image source, int maxEdge)
        {
            if (maxEdge < 8)
                maxEdge = 8;
            var w = source.Width;
            var h = source.Height;
            if (w <= maxEdge && h <= maxEdge)
                return source;

            double scale = w >= h ? (double)maxEdge / w : (double)maxEdge / h;
            var nw = Math.Max(1, (int)(w * scale));
            var nh = Math.Max(1, (int)(h * scale));
            var dest = new Bitmap(nw, nh);
            using (var g = Graphics.FromImage(dest))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(source, 0, 0, nw, nh);
            }

            source.Dispose();
            return dest;
        }
    }
}

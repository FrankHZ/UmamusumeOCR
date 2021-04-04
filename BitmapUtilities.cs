using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace UmamusumeOCR
{
    public static class BitmapUtilities
    {
        public static void ReverseWhiteText(this Bitmap bitmap)
        {
            var backgroud = bitmap.GetPixel(0, 0).Reverse();
            for (int j = 0; j < bitmap.Height; j++)
            {
                for (int i = 0; i < bitmap.Width; i++)
                {
                    var c = bitmap.GetPixel(i, j);
                    var newC = c.Reverse().ScaleToWhite(backgroud);
                    bitmap.SetPixel(i, j, newC);
                }
            }
        }

        public static Color Reverse(this Color c)
        {
            return Color.FromArgb((int)(255 - c.R * 0.9), (int)(255 - c.G * 0.9), (int)(255 - c.B * 0.9));
        }

        public static Color Binary(this Color c)
        {
            int rgb = (c.R + c.G + c.B) >= 127 * 3 ? 255 : 0;
            return Color.FromArgb(rgb, rgb, rgb);
        }

        public static Color ScaleToWhite(this Color c, Color targetToWhite)
        {
            var rToWhite = 256.0f / (targetToWhite.R + 1);
            var gToWhite = 256.0f / (targetToWhite.G + 1);
            var bToWhite = 256.0f / (targetToWhite.B + 1);
            var newR = (int)(c.R  * rToWhite - 1);
            var newG = (int)(c.G  * gToWhite - 1);
            var newB = (int)(c.B  * bToWhite - 1);
            return Color.FromArgb(newR > 255? 255 : newR,
                newG > 255 ? 255 : newG,
                newB > 255 ? 255 : newB);
        }

        public static bool IsSimilar(this Color c1, Color c2, int threshold = 45)
        {
            return Math.Abs(c1.R - c2.R) < threshold
                && Math.Abs(c1.G - c2.G) < threshold
                && Math.Abs(c1.B - c2.B) < threshold;
        }

        public static bool IsWhite(this Color c, int threshold = 210 * 3)
        {
            return c.R + c.G + c.B > threshold;
        }

        public static bool IsDark(this Color c, int threshold = 128 * 3)
        {
            return c.R + c.G + c.B < threshold;
        }

        public static bool IsSimilar(Bitmap b1, Bitmap b2)
        {
            var badCount = 0;
            if (b1.Size != b2.Size)
            {
                return false;
            }

            for (int j = 0; j < b1.Height; j++)
            {
                for (int i = 1; i < b1.Width; i++)
                {
                    var c1 = b1.GetPixel(i, j);
                    var c2 = b2.GetPixel(i, j);
                    if (!IsSimilar(c1, c2))
                    {
                        badCount++;
                    }
                }
            }
            return badCount < b1.Height * b1.Width / 20;
        }

        public static void DebugWriteArea(this Bitmap bitmap, string fName)
        {
            var file = new StreamWriter(fName);
            for (int j = 0; j < bitmap.Height; j++)
            {
                for (int i = 0; i < bitmap.Width; i++)
                {
                    var c = bitmap.GetPixel(i, j);
                    file.Write($"({c.R} {c.G} {c.B}) ");
                }
                file.WriteLine();
            }
            file.Close();
        }

        public static void DebugWriteColors(this IEnumerable<Color> colors, string fName)
        {
            var file = new StreamWriter(fName);
            foreach (var c in colors)
            {
                file.WriteLine($"({c.R} {c.G} {c.B})");
            }
            file.Close();
        }

        public static Bitmap CaptureGame(Rectangle absoluteGameArea, Size finalSize)
        {
            using var bitmap = new Bitmap(absoluteGameArea.Width, absoluteGameArea.Height);
            using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(absoluteGameArea.X, absoluteGameArea.Y, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);

            return new(bitmap, finalSize.Width, finalSize.Height);
        }

        public static Bitmap CaptureAreaFromBitmap(Bitmap game, Rectangle area)
        {
            var bitmap = new Bitmap(area.Width, area.Height);
            using var g = Graphics.FromImage(bitmap);
            g.DrawImage(game, 0, 0, area, GraphicsUnit.Pixel);
            return bitmap;
        }

        public static Bitmap CombineBitmaps(IEnumerable<Bitmap> bs)
        {
            var bitmap = new Bitmap(bs.Max(bm => bm.Width), bs.Sum(bm => bm.Height));
            using var g = Graphics.FromImage(bitmap);
            var h = 0;
            foreach (var bm in bs)
            {
                g.DrawImage(bm, 0, h);
                h += bm.Height;
            }
            return bitmap;
        }
    }
}

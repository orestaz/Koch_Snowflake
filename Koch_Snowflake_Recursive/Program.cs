using System;
using System.IO;

namespace BMP_example
{
    class Program
    {
        const int W = 2000;
        const int H = 2000;

        // 24-bit: 3 bytes per pixel, row padded to multiple of 4
        static int rowBytes = ((W * 3 + 3) / 4) * 4;
        static byte[] t;

        // random spalva segmentui
        static readonly Random rng = new Random();
        static byte curR, curG, curB;

        static void Main(string[] args)
        {
            int? depth = 5;
            if (args != null && args.Length > 0)
            {
                if (int.TryParse(args[0], out int parsedDepth))
                {
                    depth = parsedDepth;
                }
                else
                {
                    depth = 1;
                }
                if (depth < 0) depth = 0;
                Console.WriteLine($"depth={depth}");
            }
            else
            {
                Console.WriteLine($"depth=max");
                depth = null; 
            }

            t = new byte[H * rowBytes];

            // baltas fonas 24-bit (R=G=B=255)
            Array.Fill(t, (byte)255);

            // 24-bit BMP header (54 bytes, be palette)
            var header = new byte[54]
            {
                0x42, 0x4d,                         // 'BM'
                0x0, 0x0, 0x0, 0x0,                 // file size (patch)
                0x0, 0x0, 0x0, 0x0,                 // reserved
                0x0, 0x0, 0x0, 0x0,                 // data offset (patch)

                0x28, 0x0, 0x0, 0x0,                // DIB header size = 40
                0x0, 0x0, 0x0, 0x0,                 // width (patch)
                0x0, 0x0, 0x0, 0x0,                 // height (patch)
                0x1, 0x0,                           // planes = 1
                0x18, 0x0,                          // bpp = 24
                0x0, 0x0, 0x0, 0x0,                 // compression = 0
                0x0, 0x0, 0x0, 0x0,                 // image size (patch)
                0x0, 0x0, 0x0, 0x0,                 // xppm
                0x0, 0x0, 0x0, 0x0,                 // yppm
                0x0, 0x0, 0x0, 0x0,                 // clrUsed
                0x0, 0x0, 0x0, 0x0                  // clrImportant
            };

            PatchHeader(header);

            Pt[] initialRectangle =
            {
                new Pt(20, 20),
                new Pt(W - 20, 20),
                new Pt(W - 20, H - 20),
                new Pt(20, H - 20)
            };

            DrawPolygonFractal(initialRectangle, 0, depth);

            string outName = (depth == null) ? $"sample_dMAX.bmp" : $"sample_d{depth}.bmp";
            using (FileStream file = new FileStream(outName, FileMode.Create, FileAccess.Write))
            {
                file.Write(header, 0, header.Length);
                file.Write(t, 0, t.Length);
            }

            Console.WriteLine($"Saved: {outName}");
        }

        static void PatchHeader(byte[] header)
        {
            int dataOffset = 54;
            int imageSize = H * rowBytes;
            int fileSize = dataOffset + imageSize;

            Array.Copy(BitConverter.GetBytes(fileSize), 0, header, 2, 4);        // file size
            Array.Copy(BitConverter.GetBytes(dataOffset), 0, header, 10, 4);     // data offset
            Array.Copy(BitConverter.GetBytes(W), 0, header, 0x12, 4);            // width
            Array.Copy(BitConverter.GetBytes(H), 0, header, 0x16, 4);            // height
            Array.Copy(BitConverter.GetBytes(imageSize), 0, header, 0x22, 4);    // image size
        }

        static void DrawPolygonFractal(Pt[] poly, int i, int? depth)
        {
            if (i >= poly.Length) return;

            int next = (i == poly.Length - 1) ? 0 : i + 1;

            FractalSegment(poly[i], poly[next], depth);
            DrawPolygonFractal(poly, i + 1, depth);
        }

        static void FractalSegment(Pt A, Pt E, int? depth)
        {
            double dx = E.X - A.X;
            double dy = E.Y - A.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);

            if (len <= 1.2 || (depth != null && depth <= 0))
            {
                // RANDOM spalva kiekvienam segmentui
                PickRandomColor();
                DrawLineRec(A, E);
                return;
            }

            // B ir D (trečdaliai)
            Pt B = new Pt(A.X + dx / 3.0, A.Y + dy / 3.0);
            Pt D = new Pt(A.X + 2.0 * dx / 3.0, A.Y + 2.0 * dy / 3.0);

            // v = (E-A)/3
            double vx = dx / 3.0;
            double vy = dy / 3.0;

            Rotate(vx, vy, 60, out double rx, out double ry);

            // viršūnė
            Pt C = new Pt(B.X + rx, B.Y + ry);
            if (depth != null)
            {
                FractalSegment(A, B, depth - 1);
                FractalSegment(B, C, depth - 1);
                FractalSegment(C, D, depth - 1);
                FractalSegment(D, E, depth - 1);
            }
            else
            {
                FractalSegment(A, B, null);
                FractalSegment(B, C, null);
                FractalSegment(C, D, null);
                FractalSegment(D, E, null);
            }
        }

        static void Rotate(double x, double y, double angleDeg, out double xr, out double yr)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double ca = Math.Cos(rad);
            double sa = Math.Sin(rad);
            xr = ca * x - sa * y;
            yr = sa * x + ca * y;
        }

        static void DrawLineRec(Pt a, Pt b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;

            if (MaxAbs(dx, dy) <= 0.75)
            {
                SetPixelRound(a.X, a.Y);
                SetPixelRound(b.X, b.Y);
                return;
            }

            Pt m = new Pt((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
            DrawLineRec(a, m);
            DrawLineRec(m, b);
        }

        static double MaxAbs(double dx, double dy)
        {
            double adx = dx < 0 ? -dx : dx;
            double ady = dy < 0 ? -dy : dy;
            return adx > ady ? adx : ady;
        }

        static void SetPixelRound(double x, double y)
        {
            int xi = (int)Math.Round(x);
            int yi = (int)Math.Round(y);
            SetPixel(xi, yi);
        }

        static void SetPixel(int x, int y)
        {
            if ((uint)x >= (uint)W) return;
            if ((uint)y >= (uint)H) return;

            int idx = y * rowBytes + x * 3;

            // 24-bit BMP: BGR
            t[idx + 0] = curB;
            t[idx + 1] = curG;
            t[idx + 2] = curR;
        }

        static void PickRandomColor()
        {
            curR = (byte)rng.Next(256);
            curG = (byte)rng.Next(256);
            curB = (byte)rng.Next(256);
        }

        readonly struct Pt
        {
            public readonly double X;
            public readonly double Y;
            public Pt(double x, double y) { X = x; Y = y; }
        }
    }
}

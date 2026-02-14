using System;
using System.IO;

namespace BMP_example
{
    class Program
    {
        const int W = 2000;
        const int H = 2000;

        static int rowBytes = (W + 31) / 32 * 4;
        static byte[] t;

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
            }

            t = new byte[H * rowBytes];

            var header = new byte[62]
            {
                0x42, 0x4d,
                0x0, 0x0, 0x0, 0x0,     // file size (patch)
                0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0,     // data offset (patch)
                0x28, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0,     // width=1000
                0x0, 0x0, 0x0, 0x0,     // height=1000
                0x1, 0x0,
                0x1, 0x0,                // bpp=1
                0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0,      // image size (patch)
                0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0,
                0xff, 0xff, 0xff, 0x0,   // white
                0x0,  0x0,  0x0,  0x0    // black
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
            int dataOffset = 62;
            int imageSize = H * rowBytes;
            int fileSize = dataOffset + imageSize;

            // file size
            Array.Copy(BitConverter.GetBytes(fileSize), 0, header, 2, 4);
            // pixel data offset
            Array.Copy(BitConverter.GetBytes(dataOffset), 0, header, 10, 4);
            // width @ offset 0x12 (18)
            Array.Copy(BitConverter.GetBytes(W), 0, header, 0x12, 4);
            // height @ offset 0x16 (22)
            Array.Copy(BitConverter.GetBytes(H), 0, header, 0x16, 4);
            // image size @ offset 0x22 (34)
            Array.Copy(BitConverter.GetBytes(imageSize), 0, header, 0x22, 4);
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
            if(depth != null)
            {
                // 4 rekursiniai kvietimai
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

            int idx = y * rowBytes + (x >> 3);
            byte mask = (byte)(0x80 >> (x & 7));
            t[idx] |= mask;
        }

        readonly struct Pt
        {
            public readonly double X;
            public readonly double Y;
            public Pt(double x, double y) { X = x; Y = y; }
        }
    }
}

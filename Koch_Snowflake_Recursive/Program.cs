using System;
using System.Data.SqlTypes;
using System.Diagnostics.Metrics;
using System.IO;
using System.Runtime.Intrinsics.X86;

namespace BMP_example
{
    class Program
    {
        static int W = 1000;
        static int H = 1000;
        static int counter = 0;

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
            counter = 0;
            DateTime start = DateTime.Now;
            DrawPolygonFractal(initialRectangle, 0, depth);
            double miliseconds = (DateTime.Now - start).TotalMilliseconds;

            string outName = (depth == null) ? $"sample_dMAX.bmp" : $"sample_d{depth}.bmp";
            using (FileStream file = new FileStream(outName, FileMode.Create, FileAccess.Write))
            {
                file.Write(header, 0, header.Length);
                file.Write(t, 0, t.Length);
            }

            Console.WriteLine($"Saved: {outName} – Operation count={counter}, Time={miliseconds}");
            Test10Recursions(header, initialRectangle);
            //TestBySizeMaxDepth(header);
        }

        static void Test10Recursions(byte[] header, Pt[] poly)
        {
            PatchHeader(header);

            using (StreamWriter sw = new StreamWriter("Resulsts10Recursions.csv"))
            {
                sw.WriteLine("Recursion Nr." + ';' + "Operation Count" + ';' + "Time, ms" + ';');
                for (int i = 0; i < 11; i++)
                {
                    t = new byte[H * rowBytes];
                    Array.Fill(t, (byte)255);
                    counter = 0;
                    DateTime start = DateTime.Now;
                    DrawPolygonFractal(poly, 0, i);
                    double miliseconds = (DateTime.Now - start).TotalMilliseconds;
                    string outName = $"sample_d{i}.bmp";
                    using (FileStream file = new FileStream(outName, FileMode.Create, FileAccess.Write))
                    {
                        file.Write(header, 0, header.Length);
                        file.Write(t, 0, t.Length);
                    }
                    string line = String.Format("{0};{1};{2}", i, counter, miliseconds);
                    sw.WriteLine(line);
                }
                sw.Close();
            }
        }

        static void TestBySizeMaxDepth(byte[] header)
        {
            using (StreamWriter sw = new StreamWriter("ResulstsBySizeMaxDepth.csv"))
            {
                sw.WriteLine("Image size;Operation Count;Time, ms");

                for (int w = 2000; w <= 20000 ; w += 1000)
                {

                    W = w;
                    H = w;

                    rowBytes = ((W * 3 + 3) / 4) * 4;
                    t = new byte[H * rowBytes];
                    Array.Fill(t, (byte)255);

                    Pt[] initialRectangle =
                    {
                        new Pt(20, 20),
                        new Pt(W - 20, 20),
                        new Pt(W - 20, H - 20),
                        new Pt(20, H - 20)
                    };

                    PatchHeader(header);

                    counter = 0;
                    DateTime start = DateTime.Now;
                    DrawPolygonFractal(initialRectangle, 0, null);
                    double ms = (DateTime.Now - start).TotalMilliseconds;
                    string outName =  $"sample_dMAX_{W}x{H}.bmp";
                    using (FileStream file = new FileStream(outName, FileMode.Create, FileAccess.Write))
                    {
                        file.Write(header, 0, header.Length);
                        file.Write(t, 0, t.Length);
                    }
                    sw.WriteLine($"{W};{counter};{ms}");
                }
            }
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
            counter++;
            if (i >= poly.Length)
            {
                counter++;
                return;
            }
            counter+=2;
            int next = (i == poly.Length - 1) ? 0 : i + 1;

            counter += 2;
            FractalSegment(poly[i], poly[next], depth);
            DrawPolygonFractal(poly, i + 1, depth);
        }

        static void FractalSegment(Pt A, Pt E, int? depth)
        {
            counter += 4;
            double dx = E.X - A.X;
            double dy = E.Y - A.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);

            if (len <= 1.2 || (depth != null && depth <= 0))
            {
                counter += 3;
                // RANDOM spalva kiekvienam segmentui
                PickRandomColor();
                DrawLineRec(A, E);
                return;
            }

            counter += 7;
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
                counter += 4;
                FractalSegment(A, B, depth - 1);
                FractalSegment(B, C, depth - 1);
                FractalSegment(C, D, depth - 1);
                FractalSegment(D, E, depth - 1);
            }
            else
            {
                counter += 4;
                FractalSegment(A, B, null);
                FractalSegment(B, C, null);
                FractalSegment(C, D, null);
                FractalSegment(D, E, null);
            }
        }

        static void Rotate(double x, double y, double angleDeg, out double xr, out double yr)
        {
            counter += 5;
            double rad = angleDeg * Math.PI / 180.0;
            double ca = Math.Cos(rad);
            double sa = Math.Sin(rad);
            xr = ca * x - sa * y;
            yr = sa * x + ca * y;
        }

        static void DrawLineRec(Pt a, Pt b)
        {
            counter += 2;
            double dx = b.X - a.X, dy = b.Y - a.Y;

            if (MaxAbs(dx, dy) <= 0.75)
            {
                counter += 3;
                SetPixelRound(a.X, a.Y);
                SetPixelRound(b.X, b.Y);
                return;
            }

            counter += 3;
            Pt m = new Pt((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
            DrawLineRec(a, m);
            DrawLineRec(m, b);
        }

        static double MaxAbs(double dx, double dy)
        {
            double adx;
            double ady;

            counter++;
            if (dx < 0) 
            {
                counter++;
                adx = -dx;
            }
            else 
            {
                counter++;
                adx = dx;
            }

            counter++;
            if (dy < 0)
            {
                counter++;
                ady = -dy;
            }
            else
            {
                counter++;
                ady = dy;
            }

            counter++;
            if (adx > ady)
            {
                counter++;
                return adx;
            }
            else
            {
                counter++;
                return ady;
            }
        }

        static void SetPixelRound(double x, double y)
        {
            counter += 3;
            int xi = (int)Math.Round(x);
            int yi = (int)Math.Round(y);
            SetPixel(xi, yi);
        }

        static void SetPixel(int x, int y)
        {
            counter++;
            if ((uint)x >= (uint)W)
            {
                counter++;
                return;
            }

            counter++;
            if ((uint)y >= (uint)H) 
            {
                counter++;
                return;
            }

            counter += 4;
            int idx = y * rowBytes + x * 3;

            // 24-bit BMP: BGR
            t[idx + 0] = curB;
            t[idx + 1] = curG;
            t[idx + 2] = curR;
        }

        static void PickRandomColor()
        {
            counter += 3;
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

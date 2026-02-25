using System;
using System.Data.SqlTypes;
using System.Diagnostics.Metrics;
using System.IO;
using System.Runtime.Intrinsics.X86;

namespace BMP_example
{
    class Program
    {
        static int W = 20000;
        static int H = 20000;
        static long counter = 0;
        static long sk_suma = 0;
        static long sk_skirtumas = 0;
        static long sk_priskyrimas = 0;
        static long sk_sandauga = 0;
        static long sk_dalyba = 0;
        static long sk_saknis = 0;
        static long sk_sin = 0;
        static long sk_cos = 0;
        static long sk_if_salyga = 0;
        static long sk_return = 0;
        static long sk_metodo_kvietimas = 0;
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
            Test10Recursions(header, initialRectangle);
            TestBySizeMaxDepth(header);
        }

        static void Test10Recursions(byte[] header, Pt[] poly)
        {
            PatchHeader(header);

            using (StreamWriter sw = new StreamWriter("Resulsts10Recursions.csv"))
            {
                sw.WriteLine(String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};", "Rekursijos max gylis", "sk_priskyrimas", "sk_suma", "sk_skirtumas", "sk_sandauga", "sk_dalyba", "sk_saknis", "sk_sin", "sk_cos", "sk_if_salyga", "sk_return", "sk_metodo_kvietimas", "total operations", "miliseconds"));
                for (int i = 0; i < 10; i++)
                {
                    t = new byte[H * rowBytes];
                    Array.Fill(t, (byte)255);


                    //laikas
                    DateTime start = DateTime.Now;
                    DrawPolygonFractal(poly, 0, i);
                    double miliseconds = (DateTime.Now - start).TotalMilliseconds;
                    //..

                    //counteris + piesimas
                    t = new byte[H * rowBytes];
                    Array.Fill(t, (byte)255);
                    _DrawPolygonFractal(poly, 0, i);
                    //..

                    string outName = $"sample_d{i}.bmp";
                    using (FileStream file = new FileStream(outName, FileMode.Create, FileAccess.Write))
                    {
                        file.Write(header, 0, header.Length);
                        file.Write(t, 0, t.Length);
                    }
                    string line = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};", i, sk_priskyrimas, sk_suma, sk_skirtumas, sk_sandauga, sk_dalyba, sk_saknis, sk_sin, sk_cos, sk_if_salyga, sk_return, sk_metodo_kvietimas, sk_priskyrimas + sk_suma + sk_skirtumas + sk_sandauga + sk_dalyba + sk_saknis + sk_sin + sk_cos + sk_if_salyga + sk_return + sk_metodo_kvietimas, miliseconds);
                    sw.WriteLine(line);

                    sk_suma = 0;
                    sk_skirtumas = 0;
                    sk_priskyrimas = 0;
                    sk_sandauga = 0;
                    sk_dalyba = 0;
                    sk_saknis = 0;
                    sk_sin = 0;
                    sk_cos = 0;
                    sk_if_salyga = 0;
                    sk_return = 0;
                    sk_metodo_kvietimas = 0;
                }
                sw.Close();
            }
        }

        static void TestBySizeMaxDepth(byte[] header)
        {

            using (StreamWriter sw = new StreamWriter("ResulstsBySizeMaxDepth.csv"))
            {
                sw.WriteLine(String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};", "Paveikslėlio dydis", "sk_priskyrimas", "sk_suma", "sk_skirtumas", "sk_sandauga", "sk_dalyba", "sk_saknis", "sk_sin", "sk_cos", "sk_if_salyga", "sk_return", "sk_metodo_kvietimas", "total operations", "miliseconds"));

                for (int w = 2000; w <= 20000; w += 1000)
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

                    //laikas
                    DateTime start = DateTime.Now;
                    DrawPolygonFractal(initialRectangle, 0, null);
                    double ms = (DateTime.Now - start).TotalMilliseconds;
                    //..

                    //counteris + piesimas
                    t = new byte[H * rowBytes];
                    Array.Fill(t, (byte)255);
                    _DrawPolygonFractal(initialRectangle, 0, null);
                    //..

                    string outName = $"sample_dMAX_{W}x{H}.bmp";
                    using (FileStream file = new FileStream(outName, FileMode.Create, FileAccess.Write))
                    {
                        file.Write(header, 0, header.Length);
                        file.Write(t, 0, t.Length);
                    }
                    string line = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};", W, sk_priskyrimas, sk_suma, sk_skirtumas, sk_sandauga, sk_dalyba, sk_saknis, sk_sin, sk_cos, sk_if_salyga, sk_return, sk_metodo_kvietimas, sk_priskyrimas + sk_suma + sk_skirtumas + sk_sandauga + sk_dalyba + sk_saknis + sk_sin + sk_cos + sk_if_salyga + sk_return + sk_metodo_kvietimas, ms);
                    sw.WriteLine(line);
                    sk_suma = 0;
                    sk_skirtumas = 0;
                    sk_priskyrimas = 0;
                    sk_sandauga = 0;
                    sk_dalyba = 0;
                    sk_saknis = 0;
                    sk_sin = 0;
                    sk_cos = 0;
                    sk_if_salyga = 0;
                    sk_return = 0;
                    sk_metodo_kvietimas = 0;
                }
            }
        }

        static void count(long SK_priskyrimas, long SK_suma, long SK_skirtumas, long SK_sandauga, long SK_dalyba, long SK_saknis, long SK_sin, long SK_cos, long SK_if_salyga, long SK_return, long SK_metodo_kvietimas)
        {
            sk_suma += SK_suma;
            sk_skirtumas += SK_skirtumas;
            sk_priskyrimas += SK_priskyrimas;
            sk_sandauga += SK_sandauga;
            sk_dalyba += SK_dalyba;
            sk_saknis += SK_saknis;
            sk_sin += SK_sin;
            sk_cos += SK_cos;
            sk_if_salyga += SK_if_salyga;
            sk_return += SK_return;
            sk_metodo_kvietimas += SK_metodo_kvietimas;
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

            if (i >= poly.Length)
            {
                return;
            }
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
            double adx;
            double ady;

            if (dx < 0)
            {
                adx = -dx;
            }
            else
            {
                adx = dx;
            }

            if (dy < 0)
            {
                ady = -dy;
            }
            else
            {
                ady = dy;
            }

            if (adx > ady)
            {
                return adx;
            }
            else
            {
                return ady;
            }
        }

        static void SetPixelRound(double x, double y)
        {
            int xi = (int)Math.Round(x);
            int yi = (int)Math.Round(y);
            SetPixel(xi, yi);
        }

        static void SetPixel(int x, int y)
        {
            if ((uint)x >= (uint)W)
            {
                return;
            }

            if ((uint)y >= (uint)H)
            {
                return;
            }

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

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        static void _DrawPolygonFractal(Pt[] poly, int i, int? depth)
        {

            if (i >= poly.Length)
            {
                count(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0);
                return;
            }

            int next;
            if (i == poly.Length - 1)
            {
                next = 0;
                count(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }
            else
            {
                next = i + 1;
                count(1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            }

            _FractalSegment(poly[i], poly[next], depth);
            _DrawPolygonFractal(poly, i + 1, depth);
            count(0, 1, 1, 0, 0, 0, 0, 0, 2, 0, 2);
        }

        static void _FractalSegment(Pt A, Pt E, int? depth)
        {

            double dx = E.X - A.X;
            double dy = E.Y - A.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            count(3, 1, 2, 2, 0, 1, 0, 0, 1, 0, 0);

            if (len <= 1.2 || (depth != null && depth <= 0))
            {
                // RANDOM spalva kiekvienam segmentui
                _PickRandomColor();
                _DrawLineRec(A, E);
                count(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2);
                return;
            }

            // B ir D (trečdaliai)
            Pt B = new Pt(A.X + dx / 3.0, A.Y + dy / 3.0);
            Pt D = new Pt(A.X + 2.0 * dx / 3.0, A.Y + 2.0 * dy / 3.0);

            // v = (E-A)/3
            double vx = dx / 3.0;
            double vy = dy / 3.0;

            _Rotate(vx, vy, 60, out double rx, out double ry);

            // viršūnė
            Pt C = new Pt(B.X + rx, B.Y + ry);
            count(5, 6, 0, 2, 6, 0, 0, 0, 1, 0, 4);

            if (depth != null)
            {
                _FractalSegment(A, B, depth - 1);
                _FractalSegment(B, C, depth - 1);
                _FractalSegment(C, D, depth - 1);
                _FractalSegment(D, E, depth - 1);
                count(0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 4);
            }
            else
            {
                _FractalSegment(A, B, null);
                _FractalSegment(B, C, null);
                _FractalSegment(C, D, null);
                _FractalSegment(D, E, null);
                count(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4);
            }
        }

        static void _Rotate(double x, double y, double angleDeg, out double xr, out double yr)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double ca = Math.Cos(rad);
            double sa = Math.Sin(rad);
            xr = ca * x - sa * y;
            yr = sa * x + ca * y;
            count(5, 1, 1, 5, 1, 0, 1, 1, 0, 0, 0);
        }

        static void _DrawLineRec(Pt a, Pt b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            count(2, 0, 2, 0, 0, 0, 0, 0, 1, 0, 1);

            if (_MaxAbs(dx, dy) <= 0.75)
            {
                _SetPixelRound(a.X, a.Y);
                _SetPixelRound(b.X, b.Y);
                count(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2);
                return;
            }

            Pt m = new Pt((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);
            _DrawLineRec(a, m);
            _DrawLineRec(m, b);
            count(1, 2, 0, 2, 0, 0, 0, 0, 0, 0, 3);
        }

        static double _MaxAbs(double dx, double dy)
        {
            double adx;
            double ady;

            count(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0);
            if (dx < 0)
            {
                count(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                adx = -dx;
            }
            else
            {
                count(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                adx = dx;
            }

            count(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0);
            if (dy < 0)
            {
                count(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                ady = -dy;
            }
            else
            {
                count(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                ady = dy;
            }

            count(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0);
            if (adx > ady)
            {
                count(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0);
                return adx;
            }
            else
            {
                count(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0);
                return ady;
            }
        }

        static void _SetPixelRound(double x, double y)
        {
            int xi = (int)Math.Round(x);
            int yi = (int)Math.Round(y);
            _SetPixel(xi, yi);
            count(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);
        }

        static void _SetPixel(int x, int y)
        {
            count(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0);
            if ((uint)x >= (uint)W)
            {
                count(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0);
                return;
            }

            count(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0);
            if ((uint)y >= (uint)H)
            {
                count(0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0);
                return;
            }

            int idx = y * rowBytes + x * 3;

            // 24-bit BMP: BGR
            t[idx + 0] = curB;
            t[idx + 1] = curG;
            t[idx + 2] = curR;
            count(4, 4, 0, 2, 0, 0, 0, 0, 0, 0, 0);
        }

        static void _PickRandomColor()
        {
            curR = (byte)rng.Next(256);
            curG = (byte)rng.Next(256);
            curB = (byte)rng.Next(256);
            count(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------

        readonly struct Pt
        {
            public readonly double X;
            public readonly double Y;
            public Pt(double x, double y) { X = x; Y = y; }
        }
    }
}

using System;
using System.IO;

namespace BMP_example
{
    class Program
    {
        const int W = 1000;
        const int H = 1000;

        static int rowBytes = (W + 31) / 32 * 4;
        static byte[] t;

        // Koch generatorius (trikampis): (0,0)->(1,0)
        static readonly Pt[] Generator = new Pt[]
        {
            new Pt(0.0, 0.0),
            new Pt(1.0/3.0, 0.0),
            new Pt(0.5, Math.Sqrt(3.0)/6.0),
            new Pt(2.0/3.0, 0.0),
            new Pt(1.0, 0.0),
        };

        static double BaseEdgeMinLenToFractal; // apsaugo kampus (tik bazinėms briaunoms)

        static void Main(string[] args)
        {
            int depth = 2;
            if (args != null && args.Length > 0)
            {
                if (!int.TryParse(args[0], out depth)) depth = 1;
                if (depth < 0) depth = 0;
            }

            Console.WriteLine($"depth={depth}");

            t = new byte[H * rowBytes];

            // Header 62 bytes (1-bit) kaip tavo skelete
            var header = new byte[62]
            {
                0x42, 0x4d,
                0x0, 0x0, 0x0, 0x0,     // file size (patch)
                0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0,     // data offset (patch)
                0x28, 0x0, 0x0, 0x0,
                0xe8, 0x3, 0x0, 0x0,     // width=1000
                0xe8, 0x3, 0x0, 0x0,     // height=1000
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

            // Bazinė figūra (X)
            Pt[] poly = BuildBaseX();

            // Kampų apsauga: jeigu bazinė briauna trumpesnė už ribą -> jos NEdetalizuojam
            // 0.14 * min(W,H) yra gera pradžia 1000x1000 (apie 140 px)
            BaseEdgeMinLenToFractal = Math.Min(W, H) * 0.14;

            // Poligono orientacija (CCW/CW) kad "trikampiai" eitų ten kur reikia
            bool isCCW = SignedArea(poly, 0, 0.0) > 0.0;

            DrawPolygonFractal(poly, 0, depth, isCCW);

            string outName = $"sample_d{depth}.bmp";
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
            int fileSize = dataOffset + t.Length;

            Array.Copy(BitConverter.GetBytes(fileSize), 0, header, 2, 4);
            Array.Copy(BitConverter.GetBytes(dataOffset), 0, header, 10, 4);
            Array.Copy(BitConverter.GetBytes(t.Length), 0, header, 34, 4);
        }

        // -------- Bazinė X figūra --------
        static Pt[] BuildBaseX()
        {
            double ww = W - 1;
            double hh = H - 1;
            double min = ww < hh ? ww : hh;

            double m = min * 0.06;   // rėmelis nuo krašto
            double a = min * 0.22;   // įpjovos pusplotis
            double d = min * 0.18;   // įpjovos gylis

            double L = m;
            double R = ww - m;
            double B = m;
            double T = hh - m;

            double cx = (L + R) * 0.5;
            double cy = (B + T) * 0.5;

            return new Pt[]
            {
                new Pt(L, T),
                new Pt(cx - a, T),
                new Pt(cx, T - d),
                new Pt(cx + a, T),
                new Pt(R, T),

                new Pt(R, cy + a),
                new Pt(R - d, cy),
                new Pt(R, cy - a),

                new Pt(R, B),
                new Pt(cx + a, B),
                new Pt(cx, B + d),
                new Pt(cx - a, B),
                new Pt(L, B),

                new Pt(L, cy - a),
                new Pt(L + d, cy),
                new Pt(L, cy + a),
            };
        }

        // -------- Orientacija (signed area) rekursiškai --------
        static double SignedArea(Pt[] poly, int i, double acc)
        {
            if (i >= poly.Length) return 0.5 * acc;

            int next = (i == poly.Length - 1) ? 0 : i + 1;
            double term = poly[i].X * poly[next].Y - poly[next].X * poly[i].Y;
            return SignedArea(poly, i + 1, acc + term);
        }

        // -------- Fraktalas per bazines briaunas --------
        static void DrawPolygonFractal(Pt[] poly, int i, int depth, bool isCCW)
        {
            if (i >= poly.Length) return;

            int next = (i == poly.Length - 1) ? 0 : i + 1;

            // baseEdge=true -> kampų apsauga veikia tik čia
            FractalSegment(poly[i], poly[next], depth, isCCW, baseEdge: true);

            DrawPolygonFractal(poly, i + 1, depth, isCCW);
        }

        // -------- Viena briauna: Koch --------
        static void FractalSegment(Pt a, Pt b, int depth, bool isCCW, bool baseEdge)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);

            // Kampų taisymas: jeigu TIK bazinė briauna per trumpa – jos nefraktalinti
            if (baseEdge && len < BaseEdgeMinLenToFractal)
            {
                DrawLineRec(a, b);
                return;
            }

            if (depth <= 0 || len <= 1.0)
            {
                DrawLineRec(a, b);
                return;
            }

            // Vektorius
            double ux = dx / len, uy = dy / len;

            // ČIA yra kryptis "kur dėti kupra":
            // jei poligonas CCW -> vidus yra kairėje -> kupra Į VIDŲ = left normal = (-uy, ux)
            // jei poligonas CW  -> vidus yra dešinėje -> kupra Į VIDŲ = right normal = (uy, -ux)
            double vx, vy;
            if (isCCW)
            {
                vx = -uy; vy = ux;   // Į VIDŲ (CCW)
            }
            else
            {
                vx = uy; vy = -ux;  // Į VIDŲ (CW)
            }

            GenWalk(0, a, len, ux, uy, vx, vy, depth, a, isCCW);
        }

        static void GenWalk(int i, Pt start, double len,
                            double ux, double uy, double vx, double vy,
                            int depth, Pt prev, bool isCCW)
        {
            if (i >= Generator.Length - 1) return;

            Pt g = Generator[i + 1];

            Pt next = new Pt(
                start.X + (g.X * len) * ux + (g.Y * len) * vx,
                start.Y + (g.X * len) * uy + (g.Y * len) * vy
            );

            // baseEdge=false -> kampų apsauga nebetaikoma rekursijos viduje
            FractalSegment(prev, next, depth - 1, isCCW, baseEdge: false);

            GenWalk(i + 1, start, len, ux, uy, vx, vy, depth, next, isCCW);
        }

        // -------- Linija rekursiškai --------
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

        // -------- SetPixel 1-bit --------
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

using System;
using System.Collections.Generic;

namespace CutiePie.Salon.Editor
{
    /// <summary>BackgroundCutter ayarları (SalonSettings'teki "Görsel temizleme" değerlerinden doldurulur).</summary>
    public sealed class CutterOptions
    {
        /// <summary>Arka plan rengine (kenar medyanı) bu uzaklıktan yakın pikseller arka plan olabilir (0-255).</summary>
        public float KeyTolerance;

        /// <summary>Doldurma bir pikselden komşusuna yalnızca renk farkı bundan küçükse geçer (gradyan ve lekeler için).</summary>
        public float StepTolerance;

        /// <summary>Kenar bandındaki pikselin arka plana bu uzaklıkta tam opak sayıldığı renk farkı.</summary>
        public float FeatherDistance;

        /// <summary>Arka plana komşu kenar bandının genişliği (piksel).</summary>
        public int FeatherRadius;

        /// <summary>Arka plandan koyu (gölge) pikseller yarı saydam siyah gölgeye çevrilsin mi?</summary>
        public bool ConvertShadows;

        /// <summary>Arka plandan bu kadar koyu olmayan piksel gölge sayılmaz (parlaklık farkı, 0-255).</summary>
        public float ShadowMinDarkness;

        /// <summary>Gölge saydamlığının en koyuya ulaştığı parlaklık farkı aralığı.</summary>
        public float ShadowDarknessRange;

        /// <summary>Gölgenin en yüksek opaklığı (0-1).</summary>
        public float ShadowMaxAlpha;
    }

    /// <summary>Temizleme sonucu (kayıt / rapor için).</summary>
    public sealed class CutResult
    {
        public byte KeyR;
        public byte KeyG;
        public byte KeyB;
        public int BackgroundPixels;
        public int ShadowPixels;
        public int TotalPixels;
    }

    /// <summary>
    /// Yalnızca editör: düz renkli arka planı kenarlardan başlayan flood fill ile şeffaf yapar.
    /// Unity'ye bağımlı değildir (RGBA bayt dizisi üzerinde çalışır); menü (WhiteBackgroundCleaner) dosyayı okuyup yazar.
    ///
    /// 1. Arka plan rengi kenar piksellerinin medyanıdır (beyaz olmak zorunda değil, ör. gri).
    /// 2. Doldurma kenardaki arka plana yakın piksellerden başlar; komşuya ancak hem arka plan rengine yakınsa hem de
    ///    renk farkı küçükse geçer. Kontur çizgileri doldurmayı durdurur; iç kısımdaki beyazlar (tabak, yaka) korunur.
    /// 3. Arka planın kenar bandındaki pikseller renk farkına göre kısmen saydam olur ve arka plan renginden ayrıştırılır
    ///    (beyaz hale kalmaz); sonra bant 3x3 ortalamayla hafifçe yumuşatılır.
    /// 4. İstenirse arka plandan koyu kalan alan (zemin gölgesi) yarı saydam siyah gölgeye çevrilir.
    /// </summary>
    public static class BackgroundCutter
    {
        private const int Channels = 4;

        public static CutResult Cut(byte[] rgba, int width, int height, CutterOptions options)
        {
            if (rgba == null || options == null)
            {
                throw new ArgumentNullException(rgba == null ? "rgba" : "options");
            }

            int count = width * height;
            if (rgba.Length < count * Channels)
            {
                throw new ArgumentException("Piksel dizisi boyuttan kısa.", "rgba");
            }

            var result = new CutResult();
            result.TotalPixels = count;

            byte[] key = GetBorderMedian(rgba, width, height);
            result.KeyR = key[0];
            result.KeyG = key[1];
            result.KeyB = key[2];

            bool[] background = FloodBackground(rgba, width, height, key, options);
            int[] band = GetBandDistance(background, width, height, Math.Max(0, options.FeatherRadius));

            float keyLum = Luminance(key[0], key[1], key[2]);
            var alpha = new float[count];

            for (int i = 0; i < count; i++)
            {
                int p = i * Channels;
                if (background[i])
                {
                    result.BackgroundPixels++;
                    alpha[i] = 0f;

                    if (options.ConvertShadows)
                    {
                        float darkness = keyLum - Luminance(rgba[p], rgba[p + 1], rgba[p + 2]);
                        float shadow = ShadowAlpha(darkness, options);
                        if (shadow > 0f)
                        {
                            alpha[i] = shadow;
                            result.ShadowPixels++;
                        }
                    }

                    // Gölge siyahtır; tamamen saydam piksellerin rengini Unity (Alpha Is Transparency) komşulardan doldurur.
                    rgba[p] = 0;
                    rgba[p + 1] = 0;
                    rgba[p + 2] = 0;
                    continue;
                }

                if (band[i] <= 0)
                {
                    alpha[i] = 1f;
                    continue;
                }

                // Kenar bandı: arka plana benzedikçe saydamlaşır, rengi arka plandan ayrıştırılır.
                float a = Clamp01(Distance(rgba, p, key) / Math.Max(1f, options.FeatherDistance));
                alpha[i] = a;
                if (a > 0f && a < 1f)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        float unmixed = key[c] + (rgba[p + c] - key[c]) / a;
                        rgba[p + c] = ToByte(unmixed);
                    }
                }
            }

            SmoothBand(alpha, band, width, height);

            for (int i = 0; i < count; i++)
            {
                rgba[i * Channels + 3] = ToByte(alpha[i] * 255f);
            }

            return result;
        }

        private static byte[] GetBorderMedian(byte[] rgba, int width, int height)
        {
            var reds = new List<byte>();
            var greens = new List<byte>();
            var blues = new List<byte>();

            for (int x = 0; x < width; x++)
            {
                AddPixel(rgba, x, 0, width, reds, greens, blues);
                AddPixel(rgba, x, height - 1, width, reds, greens, blues);
            }

            for (int y = 1; y < height - 1; y++)
            {
                AddPixel(rgba, 0, y, width, reds, greens, blues);
                AddPixel(rgba, width - 1, y, width, reds, greens, blues);
            }

            return new[] { Median(reds), Median(greens), Median(blues) };
        }

        private static void AddPixel(byte[] rgba, int x, int y, int width, List<byte> r, List<byte> g, List<byte> b)
        {
            int p = (y * width + x) * Channels;
            r.Add(rgba[p]);
            g.Add(rgba[p + 1]);
            b.Add(rgba[p + 2]);
        }

        private static byte Median(List<byte> values)
        {
            values.Sort();
            return values[values.Count / 2];
        }

        private static bool[] FloodBackground(byte[] rgba, int width, int height, byte[] key, CutterOptions options)
        {
            int count = width * height;
            var background = new bool[count];
            var queue = new Queue<int>();

            // Tohumlar: kenardaki, arka plan rengine yakın pikseller (kenara değen nesne parçaları tohum olmaz).
            for (int x = 0; x < width; x++)
            {
                TrySeed(rgba, x, key, options, background, queue);
                TrySeed(rgba, (height - 1) * width + x, key, options, background, queue);
            }

            for (int y = 1; y < height - 1; y++)
            {
                TrySeed(rgba, y * width, key, options, background, queue);
                TrySeed(rgba, y * width + width - 1, key, options, background, queue);
            }

            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                int x = i % width;
                int y = i / width;

                if (x > 0) TryGrow(rgba, i, i - 1, key, options, background, queue);
                if (x < width - 1) TryGrow(rgba, i, i + 1, key, options, background, queue);
                if (y > 0) TryGrow(rgba, i, i - width, key, options, background, queue);
                if (y < height - 1) TryGrow(rgba, i, i + width, key, options, background, queue);
            }

            return background;
        }

        private static void TrySeed(byte[] rgba, int i, byte[] key, CutterOptions options, bool[] background, Queue<int> queue)
        {
            if (!background[i] && Distance(rgba, i * Channels, key) <= options.KeyTolerance)
            {
                background[i] = true;
                queue.Enqueue(i);
            }
        }

        private static void TryGrow(byte[] rgba, int from, int to, byte[] key, CutterOptions options, bool[] background, Queue<int> queue)
        {
            if (background[to])
            {
                return;
            }

            int p = to * Channels;
            if (Distance(rgba, p, key) > options.KeyTolerance || Distance(rgba, from * Channels, p) > options.StepTolerance)
            {
                return;
            }

            background[to] = true;
            queue.Enqueue(to);
        }

        /// <summary>Nesne piksellerinin arka plana (Chebyshev) uzaklığı; 1..radius bant, 0 bant dışı veya arka plan.</summary>
        private static int[] GetBandDistance(bool[] background, int width, int height, int radius)
        {
            int count = width * height;
            var band = new int[count];
            if (radius <= 0)
            {
                return band;
            }

            // Her turda bir önceki halkaya (ya da arka plana) komşu olan nesne pikselleri işaretlenir.
            for (int ring = 1; ring <= radius; ring++)
            {
                var next = new List<int>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int i = y * width + x;
                        if (background[i] || band[i] != 0)
                        {
                            continue;
                        }

                        if (TouchesRing(background, band, width, height, x, y, ring - 1))
                        {
                            next.Add(i);
                        }
                    }
                }

                foreach (int i in next)
                {
                    band[i] = ring;
                }
            }

            return band;
        }

        private static bool TouchesRing(bool[] background, int[] band, int width, int height, int x, int y, int ring)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                    {
                        continue;
                    }

                    int n = ny * width + nx;
                    if (ring == 0 ? background[n] : band[n] == ring)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Kenar bandındaki saydamlığı 3x3 ortalamayla yumuşatır (yalnızca azaltır; nesnenin içi opak kalır).</summary>
        private static void SmoothBand(float[] alpha, int[] band, int width, int height)
        {
            var source = (float[])alpha.Clone();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (band[i] <= 0)
                    {
                        continue;
                    }

                    float sum = 0f;
                    int samples = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                            {
                                continue;
                            }

                            sum += source[ny * width + nx];
                            samples++;
                        }
                    }

                    alpha[i] = Math.Min(source[i], sum / samples);
                }
            }
        }

        private static float ShadowAlpha(float darkness, CutterOptions options)
        {
            if (darkness <= options.ShadowMinDarkness)
            {
                return 0f;
            }

            float t = (darkness - options.ShadowMinDarkness) / Math.Max(1f, options.ShadowDarknessRange);
            return Clamp01(t) * Clamp01(options.ShadowMaxAlpha);
        }

        private static float Distance(byte[] rgba, int p, byte[] key)
        {
            float dr = rgba[p] - key[0];
            float dg = rgba[p + 1] - key[1];
            float db = rgba[p + 2] - key[2];
            return (float)Math.Sqrt((dr * dr + dg * dg + db * db) / 3f);
        }

        private static float Distance(byte[] rgba, int p, int q)
        {
            float dr = rgba[p] - rgba[q];
            float dg = rgba[p + 1] - rgba[q + 1];
            float db = rgba[p + 2] - rgba[q + 2];
            return (float)Math.Sqrt((dr * dr + dg * dg + db * db) / 3f);
        }

        private static float Luminance(byte r, byte g, byte b)
        {
            return 0.299f * r + 0.587f * g + 0.114f * b;
        }

        private static float Clamp01(float v)
        {
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        private static byte ToByte(float v)
        {
            return (byte)(v < 0f ? 0f : (v > 255f ? 255f : v + 0.5f));
        }
    }
}

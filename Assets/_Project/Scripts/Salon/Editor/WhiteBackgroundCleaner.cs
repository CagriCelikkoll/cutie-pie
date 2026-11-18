using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde: "Cutie Pie → Salon → Beyaz Arka Planı Temizle".
    /// Seçili Salon PNG'lerinin düz renkli arka planını BackgroundCutter ile şeffaf yapar ve sonucu kaynağın yanına
    /// "ad_cut.png" olarak yazar; kaynak dosyaya dokunmaz. Background/, Placeholders/ ve zaten "_cut" olan dosyalar atlanır.
    /// Sonra içe aktarıcı ayarları (kırpma, pivot) yeni dosyaya zorla uygulanır ve vitrin / içecek tezgâhına
    /// SalonSettings'teki 9-slice border konur. Değerler SalonSettings "Görsel temizleme" başlığındadır.
    /// </summary>
    public static class WhiteBackgroundCleaner
    {
        private const string MenuPath = "Cutie Pie/Salon/Beyaz Arka Planı Temizle";
        private const string ArtRoot = "Assets/_Project/Art/Salon/";
        private const string PlaceholderFolder = ArtRoot + "Placeholders/";
        private const string SettingsPath = "Assets/_Project/Data/Salon/SalonSettings.asset";
        private const string PngExtension = ".png";
        private const string ProgressTitle = "Beyaz Arka Planı Temizle";

        [MenuItem(MenuPath)]
        private static void CleanSelected()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SalonSettings>(SettingsPath);
            if (settings == null)
            {
                EditorUtility.DisplayDialog(ProgressTitle, $"{SettingsPath} bulunamadı.", "Tamam");
                return;
            }

            List<string> sources = GetSelectedSources();
            if (sources.Count == 0)
            {
                EditorUtility.DisplayDialog(ProgressTitle,
                    "Project penceresinde Art/Salon altından temizlenecek PNG'leri seçin.\n" +
                    "(Background/, Placeholders/ ve _cut dosyaları atlanır.)", "Tamam");
                return;
            }

            var outputs = new List<string>();
            try
            {
                for (int i = 0; i < sources.Count; i++)
                {
                    string source = sources[i];
                    EditorUtility.DisplayProgressBar(ProgressTitle, Path.GetFileName(source), (float)i / sources.Count);

                    string output = CleanFile(source, settings);
                    if (output != null)
                    {
                        outputs.Add(output);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Yeni ya da üzerine yazılmış _cut dosyalarına kırpma / pivot yeniden uygulanır (sprite kimliği korunur).
            SalonArtPostprocessor.ForceReimport(outputs);
            ApplySliceBorders(outputs, settings);

            Debug.Log($"[WhiteBackgroundCleaner] {outputs.Count}/{sources.Count} görsel temizlendi.");
        }

        [MenuItem(MenuPath, true)]
        private static bool CanClean() => GetSelectedSources().Count > 0;

        private static List<string> GetSelectedSources()
        {
            var result = new List<string>();
            foreach (Object obj in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj).Replace('\\', '/');
                if (path.StartsWith(ArtRoot)
                    && !path.StartsWith(PlaceholderFolder)
                    && !SalonArtPostprocessor.IsBackground(path)
                    && !SalonArtPostprocessor.IsCutCopy(path)
                    && Path.GetExtension(path).ToLowerInvariant() == PngExtension)
                {
                    result.Add(path);
                }
            }

            return result;
        }

        /// <summary>Tek dosyayı temizler, "_cut.png" yazar ve yolunu döner; okunamazsa null.</summary>
        private static string CleanFile(string source, SalonSettings settings)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(source)))
                {
                    Debug.LogWarning($"[WhiteBackgroundCleaner] '{source}' okunamadı.");
                    return null;
                }

                int width = texture.width;
                int height = texture.height;
                Color32[] pixels = texture.GetPixels32();
                byte[] rgba = ToBytes(pixels);

                CutterOptions options = BuildOptions(source, settings);
                CutResult result = BackgroundCutter.Cut(rgba, width, height, options);

                FromBytes(rgba, pixels);
                texture.SetPixels32(pixels);
                texture.Apply(false);

                string output = GetOutputPath(source);
                File.WriteAllBytes(output, texture.EncodeToPNG());

                Debug.Log($"[WhiteBackgroundCleaner] {Path.GetFileName(source)} → {Path.GetFileName(output)} " +
                          $"(arka plan rengi {result.KeyR},{result.KeyG},{result.KeyB}; silinen %{100f * result.BackgroundPixels / result.TotalPixels:0.0}; " +
                          $"gölge %{100f * result.ShadowPixels / result.TotalPixels:0.0}; tolerans {options.KeyTolerance}/{options.StepTolerance})");
                return output;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static CutterOptions BuildOptions(string source, SalonSettings settings)
        {
            var options = new CutterOptions
            {
                KeyTolerance = settings.cleanupKeyTolerance,
                StepTolerance = settings.cleanupStepTolerance,
                FeatherDistance = settings.cleanupFeatherDistance,
                FeatherRadius = settings.cleanupFeatherRadius,
                // Karakterlerde zemin gölgesi yoktur; arka plandaki lekeler gölgeye çevrilmesin.
                ConvertShadows = !SalonArtPostprocessor.IsCustomer(source),
                ShadowMinDarkness = settings.cleanupShadowMinDarkness,
                ShadowDarknessRange = settings.cleanupShadowDarknessRange,
                ShadowMaxAlpha = settings.cleanupShadowMaxAlpha
            };

            string baseName = Path.GetFileNameWithoutExtension(source);
            foreach (ArtCleanupOverride entry in settings.cleanupOverrides)
            {
                if (entry.fileName == baseName)
                {
                    options.KeyTolerance = entry.keyTolerance;
                    options.StepTolerance = entry.stepTolerance;
                    break;
                }
            }

            return options;
        }

        private static string GetOutputPath(string source)
        {
            string folder = Path.GetDirectoryName(source)?.Replace('\\', '/');
            return $"{folder}/{Path.GetFileNameWithoutExtension(source)}{SalonArtPostprocessor.CutSuffix}{PngExtension}";
        }

        private static void ApplySliceBorders(List<string> outputs, SalonSettings settings)
        {
            foreach (string output in outputs)
            {
                string baseName = SalonArtPostprocessor.GetBaseName(output);
                if (baseName == SalonArtPostprocessor.ShowcaseCabinetName)
                {
                    SalonArtPostprocessor.ApplySliceBorder(output, settings.showcaseCabinetSliceBorder);
                }
                else if (baseName == SalonArtPostprocessor.DrinkCounterName)
                {
                    SalonArtPostprocessor.ApplySliceBorder(output, settings.drinkCounterSliceBorder);
                }
            }
        }

        private static byte[] ToBytes(Color32[] pixels)
        {
            var bytes = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                int p = i * 4;
                bytes[p] = pixels[i].r;
                bytes[p + 1] = pixels[i].g;
                bytes[p + 2] = pixels[i].b;
                bytes[p + 3] = pixels[i].a;
            }

            return bytes;
        }

        private static void FromBytes(byte[] bytes, Color32[] pixels)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                int p = i * 4;
                pixels[i] = new Color32(bytes[p], bytes[p + 1], bytes[p + 2], bytes[p + 3]);
            }
        }
    }
}

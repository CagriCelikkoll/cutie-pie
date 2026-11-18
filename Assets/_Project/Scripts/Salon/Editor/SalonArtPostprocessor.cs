using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde: Art/Salon altındaki PNG'lere (Placeholders/ hariç) Salon görsel ayarlarını uygular.
    /// Ayarlar yalnızca ilk içe aktarmada (.meta yokken) uygulanır; sonradan elle yapılan ince ayarlar korunur.
    /// "Görsel Ayarlarını Yeniden Uygula" menüsü seçili (ya da tüm) görsellere ayarları zorla yeniden uygular.
    ///
    /// Görsellerin şeffaf kenar boşlukları birbirinden çok farklı olduğu için sprite, görselin dolu alanına
    /// (alpha sınırlarına) kırpılır ve pivot dolu alanın ortasına konur. Böylece SpriteFitter ve UI Image
    /// (preserveAspect) boşluğu değil görselin kendisini kutuya sığdırır.
    ///
    /// Klasör / ad kuralları:
    /// - Background/: kırpılmaz, tek sprite, Max 2048, mip kapalı (kamerayı kaplayan arka plan).
    /// - Customers/ ve table_*: pivot alt orta (durum görselleri değişince taban aynı yerde kalır).
    /// - showcase_cabinet, drink_counter: 9-slice için Full Rect mesh (border'ı temizleme menüsü koyar).
    /// "_cut" son eki (temizlenmiş kopya) ad kurallarında yok sayılır.
    /// </summary>
    public class SalonArtPostprocessor : AssetPostprocessor
    {
        private const string MenuPath = "Cutie Pie/Salon/Görsel Ayarlarını Yeniden Uygula";
        private const string ArtRoot = "Assets/_Project/Art/Salon/";
        private const string ExcludedFolder = ArtRoot + "Placeholders/";
        private const string BackgroundFolder = ArtRoot + "Background/";
        private const string CustomersFolder = ArtRoot + "Customers/";
        private const string PngExtension = ".png";

        /// <summary>Temizlenmiş kopyaların dosya adı son eki (WhiteBackgroundCleaner yazar).</summary>
        public const string CutSuffix = "_cut";

        private const string TablePrefix = "table_";

        /// <summary>9-slice çizilen görseller (son eksiz ad).</summary>
        public const string ShowcaseCabinetName = "showcase_cabinet";
        public const string DrinkCounterName = "drink_counter";

        private const int MaxTextureSize = 1024;
        private const int BackgroundMaxTextureSize = 2048;

        // Kaynak görseller 2048 px; dünya boyutunu SpriteFitter / BackgroundFitter belirlediği için bu değer
        // yalnızca editördeki ilk görünüşü etkiler.
        private const float PixelsPerUnit = 512f;

        // Bu değerin altındaki alpha (0-255) boş sayılır (kenarlardaki neredeyse görünmez gölge/kenar yumuşatma pikselleri).
        private const byte AlphaThreshold = 8;

        private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 BottomCenterPivot = new Vector2(0.5f, 0f);

        // Menüden yeniden uygularken "yalnızca ilk içe aktarma" kuralını atlar.
        private static bool forceApply;

        private void OnPreprocessTexture()
        {
            if (!IsSalonArt(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing && !forceApply)
            {
                return;
            }

            ApplySettings(importer, assetPath);
        }

        private static string Normalize(string path) => path.Replace('\\', '/');

        private static bool IsSalonArt(string path)
        {
            string normalized = Normalize(path);
            return normalized.StartsWith(ArtRoot)
                   && !normalized.StartsWith(ExcludedFolder)
                   && Path.GetExtension(normalized).ToLowerInvariant() == PngExtension;
        }

        public static bool IsBackground(string path) => Normalize(path).StartsWith(BackgroundFolder);

        public static bool IsCustomer(string path) => Normalize(path).StartsWith(CustomersFolder);

        /// <summary>Dosya adı, "_cut" son eki olmadan (ör. "table_clean_cut.png" → "table_clean").</summary>
        public static string GetBaseName(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            return name.EndsWith(CutSuffix) ? name.Substring(0, name.Length - CutSuffix.Length) : name;
        }

        public static bool IsCutCopy(string path) => Path.GetFileNameWithoutExtension(path).EndsWith(CutSuffix);

        private static bool IsSliced(string path)
        {
            string baseName = GetBaseName(path);
            return baseName == ShowcaseCabinetName || baseName == DrinkCounterName;
        }

        private static bool UsesBottomPivot(string path) => IsCustomer(path) || GetBaseName(path).StartsWith(TablePrefix);

        private static void ApplySettings(TextureImporter importer, string path)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            // Arka plan: ekranı kaplar, uzaklaşmaz (mip gereksiz), kırpılmaz; tam çözünürlükte kalır.
            if (IsBackground(path))
            {
                importer.mipmapEnabled = false;
                importer.maxTextureSize = BackgroundMaxTextureSize;
                importer.spriteImportMode = SpriteImportMode.Single;
                return;
            }

            importer.mipmapEnabled = true;
            importer.maxTextureSize = MaxTextureSize;

            // 9-slice çizim (SpriteRenderer Sliced) Full Rect mesh ister.
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = IsSliced(path) ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            importer.SetTextureSettings(textureSettings);

            // Dolu alan bulunamazsa (tamamen saydam / okunamayan dosya) tüm görsel tek sprite olarak kalır.
            if (TryGetOpaqueRect(path, out Rect rect))
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                bool bottom = UsesBottomPivot(path);
                SetSingleSpriteRect(importer, Path.GetFileNameWithoutExtension(path), rect,
                    bottom ? SpriteAlignment.BottomCenter : SpriteAlignment.Center,
                    bottom ? BottomCenterPivot : CenterPivot);
            }
            else
            {
                Debug.LogWarning($"[SalonArtPostprocessor] '{path}' için dolu alan bulunamadı; görsel kırpılmadan içe aktarıldı.");
                importer.spriteImportMode = SpriteImportMode.Single;
            }
        }

        /// <summary>
        /// Görselde tek sprite tanımlar (2D Sprite paketinin veri sağlayıcısıyla; TextureImporter.spritesheet 2022.3'te kaldırıldı).
        /// Daha önce sprite varsa kimliği ve 9-slice border'ı korunur ki ona bağlı referanslar (ProductData.icon vb.) kopmasın.
        /// </summary>
        private static void SetSingleSpriteRect(TextureImporter importer, string spriteName, Rect rect, SpriteAlignment alignment, Vector2 pivot)
        {
            ISpriteEditorDataProvider provider = CreateProvider(importer);

            SpriteRect[] existing = provider.GetSpriteRects();
            bool hasExisting = existing != null && existing.Length > 0;
            GUID spriteId = hasExisting ? existing[0].spriteID : GUID.Generate();

            var spriteRect = new SpriteRect
            {
                name = spriteName,
                rect = rect,
                alignment = alignment,
                pivot = pivot,
                border = hasExisting ? existing[0].border : Vector4.zero,
                spriteID = spriteId
            };

            provider.SetSpriteRects(new[] { spriteRect });

            var nameFileIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameFileIds?.SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(spriteName, spriteId) });

            provider.Apply();
        }

        private static ISpriteEditorDataProvider CreateProvider(TextureImporter importer)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            return provider;
        }

        /// <summary>Kaynak PNG'deki dolu piksellerin sınırı (kaynak piksel koordinatı, sol alt köşe 0,0).</summary>
        private static bool TryGetOpaqueRect(string path, out Rect rect)
        {
            rect = default;
            if (!File.Exists(path))
            {
                return false;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                {
                    return false;
                }

                int width = texture.width;
                int height = texture.height;
                Color32[] pixels = texture.GetPixels32();

                int minX = width, minY = height, maxX = -1, maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[row + x].a <= AlphaThreshold)
                        {
                            continue;
                        }

                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    return false;
                }

                rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // --- Dışarıdan kullanım (temizleme menüsü) ---

        /// <summary>Verilen görsellere Salon ayarlarını, "yalnızca ilk içe aktarma" kuralını atlayarak yeniden uygular.</summary>
        public static void ForceReimport(IReadOnlyList<string> paths)
        {
            if (paths == null || paths.Count == 0)
            {
                return;
            }

            forceApply = true;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in paths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                forceApply = false;
            }
        }

        /// <summary>
        /// Görselin (tek) sprite'ına 9-slice border koyar. fractions: sprite dikdörtgenine oranla sol, alt, sağ, üst (0-0.5).
        /// İçe aktarma sırasında değil, içe aktarma bittikten sonra çağrılır.
        /// </summary>
        public static bool ApplySliceBorder(string path, Vector4 fractions)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return false;
            }

            ISpriteEditorDataProvider provider = CreateProvider(importer);
            SpriteRect[] rects = provider.GetSpriteRects();
            if (rects == null || rects.Length == 0)
            {
                return false;
            }

            Rect r = rects[0].rect;
            rects[0].border = new Vector4(
                Mathf.Round(Mathf.Clamp(fractions.x, 0f, 0.5f) * r.width),
                Mathf.Round(Mathf.Clamp(fractions.y, 0f, 0.5f) * r.height),
                Mathf.Round(Mathf.Clamp(fractions.z, 0f, 0.5f) * r.width),
                Mathf.Round(Mathf.Clamp(fractions.w, 0f, 0.5f) * r.height));

            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
            return true;
        }

        // --- Menü ---

        [MenuItem(MenuPath)]
        private static void ReapplySettings()
        {
            string[] paths = GetTargetPaths();
            if (paths.Length == 0)
            {
                EditorUtility.DisplayDialog("Salon Görselleri", $"{ArtRoot} altında (Placeholders hariç) PNG bulunamadı.", "Tamam");
                return;
            }

            ForceReimport(paths);
            Debug.Log($"[SalonArtPostprocessor] {paths.Length} görsele Salon ayarları yeniden uygulandı.");
        }

        /// <summary>Seçili Salon görselleri; seçim yoksa Art/Salon altındaki tüm PNG'ler (Placeholders hariç).</summary>
        private static string[] GetTargetPaths()
        {
            var selected = new List<string>();
            foreach (Object obj in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (IsSalonArt(path))
                {
                    selected.Add(path);
                }
            }

            if (selected.Count > 0)
            {
                return selected.ToArray();
            }

            var all = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsSalonArt(path))
                {
                    all.Add(path);
                }
            }

            return all.ToArray();
        }
    }
}

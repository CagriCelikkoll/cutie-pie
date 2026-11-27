using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: UIBuilderKit'i tamamlayan ortak "yerinde güncelleme" yardımcıları ve yer tutucu şekil sprite'ları.
    /// (Pasta Dolabı builder'ındaki aynı yardımcılar ileride buraya taşınacak; şimdilik Tezgâh builder'ı kullanır.)
    /// Kurallar UIBuilderKit ile aynı: nesneler adlarıyla bulunur, yoksa üretilir, hiçbiri silinmez; var olan Image'ların
    /// sprite'ına ve rengine dokunulmaz (sonradan takılan sprite'lar korunur), yalnızca yerleşim eşitlenir.
    /// </summary>
    internal static class UIBuilderShapes
    {
        public const string PlaceholderFolder = "Assets/_Project/Art/Salon/Placeholders";

        private const int ShapeTextureSize = 128;
        private const int Supersample = 4;
        private const float SpritePixelsPerUnit = 100f;

        // ------------------------------------------------------------------ Yerinde güncelleme

        /// <summary>
        /// Image'lı kutu; yoksa verilen sprite ve renkle üretilir (tıklama yakalamaz). Var olanın yalnızca yerleşimi eşitlenir.
        /// startActive yalnızca ilk eklenişte uygulanır.
        /// </summary>
        public static Image EnsureImage(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite, Color color,
            bool sliced, bool startActive, ref bool changed)
        {
            RectTransform tf = UIBuilderKit.EnsureChild(parent, name, () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(parent, name, position, size);
                Image image = created.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
                image.preserveAspect = !sliced && sprite != null;
                image.color = color;
                image.raycastTarget = false;
                created.gameObject.SetActive(startActive);
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(tf, position, size, ref changed);
            return tf.GetComponent<Image>();
        }

        /// <summary>Üst kutuya yayılan Image (inset > 0 içeri, &lt; 0 dışarı taşar); yoksa üretilir.</summary>
        public static Image EnsureStretchedImage(Transform parent, string name, float inset, Sprite sprite, Color color, bool sliced,
            bool raycast, ref bool changed)
        {
            RectTransform tf = UIBuilderKit.EnsureChild(parent, name, () =>
            {
                RectTransform created = UIBuilderKit.CreateStretched(parent, name);
                Image image = created.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
                image.color = color;
                image.raycastTarget = raycast;
                return created;
            }, ref changed);
            UIBuilderKit.EnsureStretchedRect(tf, inset, ref changed);
            return tf.GetComponent<Image>();
        }

        /// <summary>Dış kutu (kenar rengi) > Inner (iç renk, inset kadar küçük). Inner döner; parçalar ona eklenir.</summary>
        public static RectTransform EnsureBorderedBox(Transform parent, string name, Vector2 position, Vector2 size, float inset,
            Sprite sprite, Color borderColor, Color innerColor, ref bool changed)
        {
            RectTransform outer = EnsureImage(parent, name, position, size, sprite, borderColor, true, true, ref changed)
                .rectTransform;
            return EnsureImage(outer, "Inner", Vector2.zero, size - Vector2.one * (inset * 2f), sprite, innerColor, true, true,
                ref changed).rectTransform;
        }

        /// <summary>
        /// Kenarlıklı metin butonu: dış Image kenar rengi (tıklamayı alır), Inner buton rengi (Button tonu buna uygulanır),
        /// Inner/Label yerelleştirilmiş metin.
        /// </summary>
        public static Button EnsureTextButton(Transform parent, string name, string key, Vector2 position, Vector2 size,
            float border, float fontSize, Sprite rounded, Color borderColor, Color color, Color textColor, ref bool changed)
        {
            Vector2 innerSize = size - Vector2.one * (border * 2f);
            Vector2 labelSize = innerSize - new Vector2(24f, 8f);
            RectTransform tf = UIBuilderKit.EnsureChild(parent, name, () =>
            {
                RectTransform created = UIBuilderKit.CreateRect(parent, name, position, size);
                UIBuilderKit.AddSlicedImage(created.gameObject, rounded, borderColor);

                RectTransform innerTf = UIBuilderKit.CreateRect(created, "Inner", Vector2.zero, innerSize);
                Image innerImage = UIBuilderKit.AddSlicedImage(innerTf.gameObject, rounded, color);
                innerImage.raycastTarget = false;

                UIBuilderKit.AddButton(created.gameObject, innerImage);
                UIBuilderKit.CreateText(innerTf, "Label", key, Vector2.zero, labelSize, fontSize, textColor,
                    TextAlignmentOptions.Center);
                return created;
            }, ref changed);
            UIBuilderKit.EnsureRect(tf, position, size, ref changed);

            Transform inner = UIBuilderKit.FindChild(tf, "Inner");
            if (inner != null)
            {
                UIBuilderKit.EnsureRect((RectTransform)inner, Vector2.zero, innerSize, ref changed);
                Transform label = UIBuilderKit.FindChild(inner, "Label");
                if (label != null)
                {
                    UIBuilderKit.EnsureRect((RectTransform)label, Vector2.zero, labelSize, ref changed);
                }
            }

            return tf.GetComponent<Button>();
        }

        /// <summary>Adı verilen doğrudan çocukları bu sırayla en başa dizer (arkadan öne çizim sırası).</summary>
        public static void EnsureSiblingOrder(Transform parent, ref bool changed, params string[] names)
        {
            int index = 0;
            foreach (string name in names)
            {
                Transform child = UIBuilderKit.FindChild(parent, name);
                if (child == null)
                {
                    continue;
                }

                if (child.GetSiblingIndex() != index)
                {
                    child.SetSiblingIndex(index);
                    changed = true;
                }

                index++;
            }
        }

        /// <summary>Adı verilen doğrudan çocuğu en sona alır (en önde çizilir).</summary>
        public static void EnsureLast(Transform parent, string name, ref bool changed)
        {
            Transform child = UIBuilderKit.FindChild(parent, name);
            if (child != null && child.GetSiblingIndex() != parent.childCount - 1)
            {
                child.SetAsLastSibling();
                changed = true;
            }
        }

        public static void EnsureReferenceArray(SerializedObject so, string property, Object[] values, ref bool changed)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null || !prop.isArray)
            {
                Debug.LogError($"[UIBuilderShapes] {so.targetObject.GetType().Name}.{property} dizi alanı bulunamadı.", so.targetObject);
                return;
            }

            if (prop.arraySize != values.Length)
            {
                prop.arraySize = values.Length;
                changed = true;
            }

            for (int i = 0; i < values.Length; i++)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != values[i])
                {
                    element.objectReferenceValue = values[i];
                    changed = true;
                }
            }
        }

        public static void EnsureVector2(SerializedObject so, string property, Vector2 value, ref bool changed)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogError($"[UIBuilderShapes] {so.targetObject.GetType().Name}.{property} alanı bulunamadı.", so.targetObject);
                return;
            }

            if (prop.vector2Value != value)
            {
                prop.vector2Value = value;
                changed = true;
            }
        }

        // ------------------------------------------------------------------ Yer tutucu şekiller

        /// <summary>Kalın ✓ (Pasta Dolabı'nın ürettiği Check.png ile aynı şekil; varsa o kullanılır).</summary>
        public static Sprite GetOrCreateCheckSprite()
        {
            var a = new Vector2(24f, 66f);
            var b = new Vector2(52f, 36f);
            var c = new Vector2(104f, 94f);
            const float halfWidth = 10f;
            return GetOrCreateShapeSprite(PlaceholderFolder + "/Check.png", (x, y) =>
            {
                var p = new Vector2(x, y);
                return DistanceToSegment(p, a, b) <= halfWidth || DistanceToSegment(p, b, c) <= halfWidth;
            });
        }

        /// <summary>İnce halka (radyal dolan ilerleme halkası için).</summary>
        public static Sprite GetOrCreateRingSprite()
        {
            const float outer = 62f;
            const float inner = 50f;
            return GetOrCreateShapeSprite(PlaceholderFolder + "/Ring.png", (x, y) =>
            {
                float d = Mathf.Sqrt(Sqr(x - 64f) + Sqr(y - 64f));
                return d <= outer && d >= inner;
            });
        }

        /// <summary>Tahta kaşık: sol üstte oval baş, sağ alta uzanan sap.</summary>
        public static Sprite GetOrCreateSpoonSprite()
        {
            var handleStart = new Vector2(52f, 74f);
            var handleEnd = new Vector2(116f, 10f);
            return GetOrCreateShapeSprite(PlaceholderFolder + "/Spoon.png", (x, y) =>
            {
                // Başı 45° eğik elips.
                float dx = x - 38f;
                float dy = y - 90f;
                float u = (dx + dy) * 0.7071f;
                float v = (dx - dy) * 0.7071f;
                bool head = Sqr(u / 22f) + Sqr(v / 32f) <= 1f;
                bool handle = DistanceToSegment(new Vector2(x, y), handleStart, handleEnd) <= 6f;
                return head || handle;
            });
        }

        /// <summary>
        /// Beyaz şekil dokusu (128x128; kenarlar Supersample x Supersample örnekle yumuşatılır) yazar ve Sprite olarak içe aktarır.
        /// Dosya zaten varsa yeniden yazılmaz. Doku koordinatında y yukarı doğrudur (0 alt satır).
        /// </summary>
        public static Sprite GetOrCreateShapeSprite(string path, System.Func<float, float, bool> inside)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            int size = ShapeTextureSize;
            var pixels = new Color32[size * size];
            float step = 1f / Supersample;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < Supersample; sy++)
                    {
                        for (int sx = 0; sx < Supersample; sx++)
                        {
                            if (inside(x + (sx + 0.5f) * step, y + (sy + 0.5f) * step))
                            {
                                hits++;
                            }
                        }
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (Supersample * Supersample)));
                }
            }

            UIBuilderKit.EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SpritePixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static float Sqr(float v) => v * v;

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}

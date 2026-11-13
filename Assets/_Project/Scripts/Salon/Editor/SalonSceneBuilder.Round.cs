using CutiePie.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde: G4 round bağlantısını ve kirli masayı kurar.
    /// "7 - Round ve Kirli Masayı Kur" tekrar çalıştırılabilir: Table.prefab'a dokunma alanı ve parıltı ekler,
    /// eski SalonBootstrap nesnesini siler, SalonRound'u (G6: tezgâh ve fiş dahil) bağlar, RoundManager prefab'ını (Core) sahneye ekler.
    /// RoundManager.prefab'ın kendisi değiştirilmez.
    /// </summary>
    public static partial class SalonSceneBuilder
    {
        private const string RoundManagerPrefabPath = "Assets/_Project/Prefabs/Core/RoundManager.prefab";
        private const string SalonRoundName = "SalonRound";
        private const string LegacyBootstrapName = "SalonBootstrap";
        private const string SparkleName = "Sparkle";

        [MenuItem(MenuRoot + "7 - Round ve Kirli Masayı Kur")]
        private static void BuildRoundAndDirtyTables()
        {
            if (!IsSalonSceneOpen())
            {
                return;
            }

            Transform root = GetOrCreateSceneObject("SalonRoot", null);
            Transform tablesTf = root.Find("Tables");
            Transform prepArea = root.Find("PrepArea");
            Transform spawnerTf = root.Find("CustomerSpawner");
            TableManager manager = tablesTf != null ? tablesTf.GetComponent<TableManager>() : null;
            CustomerSpawner spawner = spawnerTf != null ? spawnerTf.GetComponent<CustomerSpawner>() : null;
            if (manager == null || spawner == null || prepArea == null || AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath) == null)
            {
                EditorUtility.DisplayDialog("Round Kurulumu", "Önce 1–6. menü adımlarını çalıştırın (Tables, CustomerSpawner, PrepArea gerekli).", "Tamam");
                return;
            }

            var roundManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoundManagerPrefabPath);
            if (roundManagerPrefab == null)
            {
                EditorUtility.DisplayDialog("Round Kurulumu", $"{RoundManagerPrefabPath} bulunamadı (G5 gerekli).", "Tamam");
                return;
            }

            SalonSettings settings = GetOrCreateSettings();
            Sprite circle = GetOrCreatePlaceholderSprite(CircleSpritePath, true);
            UpdateTablePrefabForCleaning(circle, settings);

            // Eski geçici başlatıcı (betiği silindi) sahneden kaldırılır.
            Transform legacy = root.Find(LegacyBootstrapName);
            if (legacy != null)
            {
                Undo.DestroyObjectImmediate(legacy.gameObject);
            }

            // Round bağlantısı.
            var so = new SerializedObject(GetOrCreateSalonRound(root));
            so.FindProperty("tableManager").objectReferenceValue = manager;
            so.FindProperty("customerSpawner").objectReferenceValue = spawner;
            so.FindProperty("showcaseStock").objectReferenceValue = prepArea.GetComponent<LocalShowcaseStock>();
            so.FindProperty("cakeShelf").objectReferenceValue = FindShelf(prepArea, BoxCakeShowcase);
            so.FindProperty("drinkShelf").objectReferenceValue = FindShelf(prepArea, BoxDrinks);
            so.FindProperty("prepCounter").objectReferenceValue = FindPrepComponent<PrepCounter>(prepArea, BoxPrepCounter);
            so.FindProperty("orderTicket").objectReferenceValue = FindPrepComponent<OrderTicket>(prepArea, OrderTicketName);
            so.FindProperty("readyTrayStation").objectReferenceValue = FindPrepComponent<ReadyTrayStation>(prepArea, ReadyTrayStationName);
            so.ApplyModifiedProperties();

            // RoundManager (sayaç Canvas'ı içinde) yoksa sahne köküne eklenir.
            if (Object.FindObjectOfType<RoundManager>() == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(roundManagerPrefab, root.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(instance, "RoundManager");
            }

            // Masaya dokunma için EventSystem + Physics2DRaycaster (4. adımda da kurulur).
            EnsureEventSystem();
            EnsurePhysics2DRaycaster();

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("[SalonSceneBuilder] Round bağlantısı ve kirli masa kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        /// <summary>Sahnedeki SalonRound bileşenini döner; yoksa SalonRoot altında oluşturur.</summary>
        private static SalonRound GetOrCreateSalonRound(Transform root)
        {
            Transform tf = GetOrCreateSceneObject(SalonRoundName, root);
            return GetOrAddComponent<SalonRound>(tf.gameObject);
        }

        private static ProductShelf FindShelf(Transform prepArea, string boxName)
        {
            Transform tf = prepArea.Find(boxName);
            ProductShelf shelf = tf != null ? tf.GetComponent<ProductShelf>() : null;
            if (shelf == null)
            {
                Debug.LogWarning($"[SalonSceneBuilder] {boxName} rafı bulunamadı; 4. adımı çalıştırın.");
            }

            return shelf;
        }

        private static T FindPrepComponent<T>(Transform prepArea, string childName) where T : Component
        {
            Transform tf = prepArea.Find(childName);
            T component = tf != null ? tf.GetComponent<T>() : null;
            if (component == null)
            {
                Debug.LogWarning($"[SalonSceneBuilder] PrepArea/{childName} üzerinde {typeof(T).Name} bulunamadı; 4. adımı çalıştırın.");
            }

            return component;
        }

        /// <summary>
        /// Mevcut Table.prefab'a dokunma collider'ı ve temizlik parıltısı ekler/günceller.
        /// Var olan Sparkle silinmez; yalnızca farklı olan ayarlar düzeltilir. Hiçbir şey değişmediyse prefab kaydedilmez.
        /// </summary>
        private static void UpdateTablePrefabForCleaning(Sprite circle, SalonSettings settings)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(TablePrefabPath);
            try
            {
                bool changed = false;

                BoxCollider2D col = contents.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = contents.AddComponent<BoxCollider2D>();
                    changed = true;
                }

                if (col.size != TableSize)
                {
                    col.size = TableSize;
                    changed = true;
                }

                if (col.offset != Vector2.zero)
                {
                    col.offset = Vector2.zero;
                    changed = true;
                }

                changed |= EnsureSparkle(contents.transform, circle, settings, out SpriteRenderer sparkle);

                var so = new SerializedObject(contents.GetComponent<Table>());
                SerializedProperty sparkleProp = so.FindProperty("sparkle");
                if (sparkleProp.objectReferenceValue != sparkle)
                {
                    sparkleProp.objectReferenceValue = sparkle;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, TablePrefabPath);
                    Debug.Log("[SalonSceneBuilder] Table.prefab güncellendi.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// Sparkle çocuğu yoksa oluşturur; varsa yalnızca farklı olan ayarlarını günceller.
        /// Bir şey oluşturulduysa veya değiştiyse true döner.
        /// </summary>
        private static bool EnsureSparkle(Transform parent, Sprite circle, SalonSettings settings, out SpriteRenderer sparkle)
        {
            Transform tf = parent.Find(SparkleName);
            if (tf == null)
            {
                sparkle = CreateSpriteChild(parent, SparkleName, circle, TableSize,
                    settings.cleanSparkleColor, SalonSortOrder.TableSparkle);
                sparkle.enabled = false;
                return true;
            }

            bool changed = false;
            sparkle = tf.GetComponent<SpriteRenderer>();
            if (sparkle == null)
            {
                sparkle = tf.gameObject.AddComponent<SpriteRenderer>();
                changed = true;
            }

            var scale = new Vector3(TableSize.x, TableSize.y, 1f);
            if (tf.localScale != scale)
            {
                tf.localScale = scale;
                changed = true;
            }

            if (tf.localPosition != Vector3.zero)
            {
                tf.localPosition = Vector3.zero;
                changed = true;
            }

            if (tf.localRotation != Quaternion.identity)
            {
                tf.localRotation = Quaternion.identity;
                changed = true;
            }

            if (sparkle.sprite != circle)
            {
                sparkle.sprite = circle;
                changed = true;
            }

            if (sparkle.color != settings.cleanSparkleColor)
            {
                sparkle.color = settings.cleanSparkleColor;
                changed = true;
            }

            if (sparkle.sortingOrder != SalonSortOrder.TableSparkle)
            {
                sparkle.sortingOrder = SalonSortOrder.TableSparkle;
                changed = true;
            }

            // Parıltı başta kapalıdır; Table çalışma anında açar.
            if (sparkle.enabled)
            {
                sparkle.enabled = false;
                changed = true;
            }

            return changed;
        }
    }
}

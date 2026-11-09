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

        /// <summary>Mevcut Table.prefab'a dokunma collider'ı ve temizlik parıltısı ekler/günceller.</summary>
        private static void UpdateTablePrefabForCleaning(Sprite circle, SalonSettings settings)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(TablePrefabPath);
            try
            {
                BoxCollider2D col = contents.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = contents.AddComponent<BoxCollider2D>();
                }

                col.size = TableSize;
                col.offset = Vector2.zero;

                Transform old = contents.transform.Find(SparkleName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                SpriteRenderer sparkle = CreateSpriteChild(contents.transform, SparkleName, circle, TableSize,
                    settings.cleanSparkleColor, SalonSortOrder.TableSparkle);
                sparkle.enabled = false;

                var so = new SerializedObject(contents.GetComponent<Table>());
                so.FindProperty("sparkle").objectReferenceValue = sparkle;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, TablePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}

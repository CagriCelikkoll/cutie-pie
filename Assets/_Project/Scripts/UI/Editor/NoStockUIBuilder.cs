using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CutiePie.UI.Editor
{
    /// <summary>
    /// Yalnızca editörde: G16 stok uyarı panelini kurar. "Cutie Pie/UI/Stok Uyarı Panelini Kur" tekrar çalıştırılabilir:
    /// NoStockPanel prefab'ını baştan kurar ve Salon sahnesine (yoksa) ekler. Renkler RoundEndSettings'ten okunur;
    /// buradaki sayılar yalnızca yerleşimdir (1920x1080 referans).
    /// </summary>
    public static class NoStockUIBuilder
    {
        private const string MenuPath = "Cutie Pie/UI/Stok Uyarı Panelini Kur";
        private const string SalonSceneName = "Salon";
        private const string PrefabPath = UIBuilderKit.PrefabFolder + "/NoStockPanel.prefab";

        // Round sonu (10) ve geliştirme (20) panellerinin üstünde.
        private const int CanvasSortingOrder = 30;

        // Kart yerleşimi (kart merkezine göre, referans piksel).
        private static readonly Vector2 CardSize = new Vector2(1000f, 560f);
        private const float TitleY = 170f;
        private static readonly Vector2 TitleSize = new Vector2(900f, 100f);
        private const float TitleFontSize = 64f;
        private const float MessageY = 40f;
        private static readonly Vector2 MessageSize = new Vector2(860f, 90f);
        private const float MessageFontSize = 44f;
        private const float ButtonY = -150f;
        private const float ButtonColumnX = 230f;
        private static readonly Vector2 ButtonSize = new Vector2(420f, 124f);
        private const float ButtonFontSize = 46f;

        private const string KeyTitle = "salon.no_stock_title";
        private const string KeyMessage = "salon.no_stock_message";
        private const string KeyGoToKitchen = "salon.no_stock_go_kitchen";
        private const string KeyContinue = "salon.no_stock_continue";

        [MenuItem(MenuPath)]
        private static void Build()
        {
            if (EditorSceneManager.GetActiveScene().name != SalonSceneName)
            {
                EditorUtility.DisplayDialog("Stok Uyarı Paneli", "Önce Salon.unity sahnesini açın.", "Tamam");
                return;
            }

            RoundEndSettings settings = UIBuilderKit.GetOrCreateSettings();
            Sprite rounded = UIBuilderKit.GetOrCreateRoundedSprite();

            GameObject prefab = BuildPrefab(settings, rounded);
            NoStockPanel panel = PlaceInScene(prefab);
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("[NoStockUIBuilder] Stok uyarı paneli kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        private static GameObject BuildPrefab(RoundEndSettings settings, Sprite rounded)
        {
            var root = new GameObject("NoStockPanel", typeof(RectTransform));
            try
            {
                UIBuilderKit.SetupCanvas(root, CanvasSortingOrder);
                NoStockPanel panel = root.AddComponent<NoStockPanel>();

                // İçerik: perde (alttaki salona dokunulmaz) + kart.
                RectTransform content = UIBuilderKit.CreateStretched(root.transform, "Content");
                UIBuilderKit.CreateDim(content, settings);
                RectTransform card = UIBuilderKit.CreateCard(content, "Card", CardSize, rounded, settings);

                UIBuilderKit.CreateText(card, "Title", KeyTitle, new Vector2(0f, TitleY), TitleSize, TitleFontSize,
                    settings.textColor, TextAlignmentOptions.Center);
                UIBuilderKit.CreateText(card, "Message", KeyMessage, new Vector2(0f, MessageY), MessageSize, MessageFontSize,
                    settings.textColor, TextAlignmentOptions.Center, false);

                Button goToKitchen = UIBuilderKit.CreateButton(card, "GoToKitchenButton", KeyGoToKitchen,
                    new Vector2(-ButtonColumnX, ButtonY), ButtonSize, ButtonFontSize, settings.playAgainColor, rounded, settings);
                Button continueButton = UIBuilderKit.CreateButton(card, "ContinueButton", KeyContinue,
                    new Vector2(ButtonColumnX, ButtonY), ButtonSize, ButtonFontSize, settings.shopButtonColor, rounded, settings);

                var so = new SerializedObject(panel);
                so.FindProperty("content").objectReferenceValue = content.gameObject;
                so.FindProperty("goToKitchenButton").objectReferenceValue = goToKitchen;
                so.FindProperty("continueButton").objectReferenceValue = continueButton;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Prefab içerik kapalı kaydedilir; panel Awake'te de kendini gizler.
                content.gameObject.SetActive(false);

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static NoStockPanel PlaceInScene(GameObject prefab)
        {
            NoStockPanel panel = Object.FindObjectOfType<NoStockPanel>(true);
            if (panel != null)
            {
                return panel;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, EditorSceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(instance, "Stok Uyarı Paneli");
            return instance.GetComponent<NoStockPanel>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>(true) != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "EventSystem");
        }
    }
}

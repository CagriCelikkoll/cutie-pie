using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CutiePie.Salon.Editor
{
    /// <summary>
    /// Yalnızca editörde: G3b teslimini (masaya tepsi bırakma, altın yazısı) kurar.
    /// "6 - Teslimi Kur" tekrar çalıştırılabilir: GoldPopup prefab'ını yeniden üretir, OrderDelivery'yi ve masa bağlantılarını yeniler.
    /// </summary>
    public static partial class SalonSceneBuilder
    {
        private const string GoldPopupPrefabPath = PrefabFolder + "/GoldPopup.prefab";
        private const string OrderDeliveryName = "OrderDelivery";
        private const string PopupContainerName = "Popups";

        // Altın yazısı (popup merkezine göre, birim).
        private const float PopupFontSize = 5f;
        private static readonly Vector2 PopupTextRectSize = new Vector2(1.6f, 0.6f);
        private const float PopupTextOffsetX = 0.2f;
        private const float PopupIconSize = 0.35f;
        private const float PopupIconOffsetX = -0.45f;
        private static readonly Color PopupIconColor = new Color(0.98f, 0.8f, 0.2f, 1f);

        [MenuItem(MenuRoot + "6 - Teslimi Kur")]
        private static void BuildDelivery()
        {
            if (!IsSalonSceneOpen())
            {
                return;
            }

            Transform root = GetOrCreateSceneObject("SalonRoot", null);
            Transform tablesTf = root.Find("Tables");
            Transform prepArea = root.Find("PrepArea");
            TableManager manager = tablesTf != null ? tablesTf.GetComponent<TableManager>() : null;
            DropTargetRegistry registry = prepArea != null ? prepArea.GetComponent<DropTargetRegistry>() : null;
            if (manager == null || registry == null)
            {
                EditorUtility.DisplayDialog("Teslim", "Önce 1–4. menü adımlarını çalıştırın (Tables ve PrepArea/DropTargetRegistry gerekli).", "Tamam");
                return;
            }

            Sprite circle = GetOrCreatePlaceholderSprite(CircleSpritePath, true);
            SalonSettings settings = GetOrCreateSettings();

            EnsureFolder(PrefabFolder);
            GoldPopup popupPrefab = BuildGoldPopupPrefab(circle, settings);

            Transform popups = GetOrCreateSceneObject(PopupContainerName, root);
            Transform deliveryTf = GetOrCreateSceneObject(OrderDeliveryName, root);
            OrderDelivery delivery = GetOrAddComponent<OrderDelivery>(deliveryTf.gameObject);

            var deliverySo = new SerializedObject(delivery);
            deliverySo.FindProperty("settings").objectReferenceValue = settings;
            deliverySo.FindProperty("goldPopupPrefab").objectReferenceValue = popupPrefab;
            deliverySo.FindProperty("popupContainer").objectReferenceValue = popups;
            deliverySo.ApplyModifiedProperties();

            var managerSo = new SerializedObject(manager);
            managerSo.FindProperty("registry").objectReferenceValue = registry;
            managerSo.FindProperty("delivery").objectReferenceValue = delivery;
            managerSo.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = deliveryTf.gameObject;
            Debug.Log("[SalonSceneBuilder] Teslim kuruldu. Sahneyi kaydetmeyi unutmayın (Ctrl+S).");
        }

        private static GoldPopup BuildGoldPopupPrefab(Sprite circle, SalonSettings settings)
        {
            var root = new GameObject("GoldPopup");
            root.AddComponent<SortingGroup>().sortingOrder = SalonSortOrder.Popup;
            GoldPopup popup = root.AddComponent<GoldPopup>();

            SpriteRenderer icon = CreateSpriteChild(root.transform, "Icon", circle, new Vector2(PopupIconSize, PopupIconSize), PopupIconColor, 0);
            icon.transform.localPosition = new Vector3(PopupIconOffsetX, 0f, 0f);

            // Yalnızca "+" ve rakam basılır (oyuncuya görünen kelime yok → yerelleştirme gerekmez).
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);
            textGo.transform.localPosition = new Vector3(PopupTextOffsetX, 0f, 0f);

            TextMeshPro text = textGo.AddComponent<TextMeshPro>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "+0";
            text.fontSize = PopupFontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Left;
            text.color = settings.goldPopupColor;
            text.sortingOrder = 1;
            text.rectTransform.sizeDelta = PopupTextRectSize;

            var so = new SerializedObject(popup);
            so.FindProperty("text").objectReferenceValue = text;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, GoldPopupPrefabPath).GetComponent<GoldPopup>();
        }
    }
}

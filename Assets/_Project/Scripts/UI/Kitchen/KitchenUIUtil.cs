using CutiePie.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>Fırın ekranı bileşenlerinin ortak küçük yardımcıları (süre metni, simge, buton).</summary>
    internal static class KitchenUIUtil
    {
        // Yerelleştirme anahtarları; metinler Localization/tr.json ve en.json'dadır.
        private const string KeyDurationMin = "kitchen.recipe.duration_min";
        private const string KeyDurationSec = "kitchen.recipe.duration_sec";
        private const string KeyDurationMinSec = "kitchen.recipe.duration_min_sec";

        private const int SecondsPerMinute = 60;

        /// <summary>Pişme süresini "1 dk", "45 sn" ya da "1 dk 30 sn" biçiminde yazar.</summary>
        public static void SetDuration(LocalizedText text, int totalSeconds)
        {
            int minutes = Mathf.Max(0, totalSeconds) / SecondsPerMinute;
            int seconds = Mathf.Max(0, totalSeconds) % SecondsPerMinute;

            if (minutes == 0)
            {
                SetKey(text, KeyDurationSec, seconds);
            }
            else if (seconds == 0)
            {
                SetKey(text, KeyDurationMin, minutes);
            }
            else
            {
                SetKey(text, KeyDurationMinSec, minutes, seconds);
            }
        }

        /// <summary>Geri sayım metni (dk:sn). Kalan süre yukarı yuvarlanır; 0,2 sn kala "0:01" görünür.</summary>
        public static int ToCountdownSeconds(float remaining) => Mathf.Max(0, Mathf.CeilToInt(remaining));

        public static string FormatCountdown(int totalSeconds) =>
            $"{totalSeconds / SecondsPerMinute}:{totalSeconds % SecondsPerMinute:00}";

        /// <summary>
        /// Simgeyi gösterir; sprite yoksa prefab'daki yer tutucu (yuvarlak kutu, verilen renk) kalır.
        /// placeholder: prefab'daki sprite (Awake'te saklanmış).
        /// </summary>
        public static void SetIcon(Image image, Sprite sprite, Sprite placeholder, Color placeholderColor)
        {
            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.sprite = placeholder;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.color = placeholderColor;
            }
        }

        public static void SetKey(LocalizedText text, string key, params object[] args)
        {
            if (text != null)
            {
                text.SetKey(key, args);
            }
        }

        public static void SetInteractable(Button button, bool interactable)
        {
            if (button != null && button.interactable != interactable)
            {
                button.interactable = interactable;
            }
        }

        public static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }

        public static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }
    }
}

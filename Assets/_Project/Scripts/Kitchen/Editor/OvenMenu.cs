using System;
using CutiePie.Core;
using CutiePie.Core.Data;
using UnityEditor;
using UnityEngine;

namespace CutiePie.Kitchen.Editor
{
    /// <summary>Yalnızca editörde: Play sırasında fırın test menüleri (fırın ekranı G19'da gelene kadar).</summary>
    public static class OvenMenu
    {
        private const string Root = "Cutie Pie/Fırın/";

        // Saat kaydırma menülerinin adımı; menü etiketleriyle birlikte değiştirilmeli.
        private const int ClockShiftMinutes = 10;

        private const string StartPath = Root + "Pişirmeyi Başlat";
        private const string FinishPath = Root + "Fırınları Bitir";
        private const string ForwardPath = Root + "Saati 10 dk İleri Al";
        private const string BackPath = Root + "Saati 10 dk Geri Al";
        private const string LogPath = Root + "Durumu Yazdır";
        private const string CollectPath = Root + "Hazır Fırınları Topla";
        private const string AdPath = Root + "Reklamla Hızlandır";

        private static IOvenService Ovens => GameManager.Instance.Ovens;

        [MenuItem(StartPath)]
        private static void StartBake()
        {
            RecipeData[] recipes = GameManager.Instance.Config.kitchen.recipes;
            // Boş fırınlara tarifler sırayla konur (0. fırın 1. tarif, 1. fırın 2. tarif...).
            for (int i = 0; i < Ovens.Count; i++)
            {
                if (Ovens.GetState(i) == OvenState.Empty && recipes.Length > 0)
                {
                    Ovens.StartBake(i, recipes[i % recipes.Length].id);
                }
            }

            LogState();
        }

        [MenuItem(FinishPath)]
        private static void FinishAll()
        {
            if (Ovens is OvenService service)
            {
                service.FinishAllForDebug();
            }

            LogState();
        }

        [MenuItem(ForwardPath)]
        private static void ClockForward() => ShiftClock(ClockShiftMinutes);

        [MenuItem(BackPath)]
        private static void ClockBack() => ShiftClock(-ClockShiftMinutes);

        [MenuItem(CollectPath)]
        private static void CollectReady()
        {
            for (int i = 0; i < Ovens.Count; i++)
            {
                if (Ovens.GetState(i) == OvenState.Ready)
                {
                    Ovens.Collect(i);
                }
            }

            LogState();
        }

        [MenuItem(AdPath)]
        private static void SpeedUpWithAd()
        {
            for (int i = 0; i < Ovens.Count; i++)
            {
                if (Ovens.GetState(i) == OvenState.Baking)
                {
                    int index = i;
                    Ovens.SpeedUpWithAd(index, ok =>
                    {
                        Debug.Log($"[OvenMenu] Fırın {index} reklamla hızlandırıldı mı: {ok}");
                        LogState();
                    });
                    return;
                }
            }

            Debug.Log("[OvenMenu] Pişen fırın yok.");
        }

        [MenuItem(LogPath)]
        private static void LogState()
        {
            Debug.Log($"[OvenMenu] Fırın sayısı: {Ovens.Count}, saat kaydırması: {GameClock.DebugOffset}");
            for (int i = 0; i < Ovens.Count; i++)
            {
                Debug.Log($"[OvenMenu] Fırın {i}: {Ovens.GetState(i)}, tarif: {Ovens.GetRecipeId(i) ?? "-"}, " +
                          $"kalan: {Ovens.GetRemainingSeconds(i):0.0} sn");
            }

            foreach (RecipeData recipe in GameManager.Instance.Config.kitchen.recipes)
            {
                if (recipe != null && recipe.product != null)
                {
                    Debug.Log($"[OvenMenu] Stok {recipe.product.id}: {GameManager.Instance.Stock.Get(recipe.product.id)}");
                }
            }
        }

        private static void ShiftClock(int minutes)
        {
            GameClock.DebugOffset += TimeSpan.FromMinutes(minutes);
            LogState();
        }

        [MenuItem(StartPath, true)]
        [MenuItem(FinishPath, true)]
        [MenuItem(ForwardPath, true)]
        [MenuItem(BackPath, true)]
        [MenuItem(LogPath, true)]
        [MenuItem(CollectPath, true)]
        [MenuItem(AdPath, true)]
        private static bool ValidatePlaying() =>
            EditorApplication.isPlaying && GameManager.Instance != null && GameManager.Instance.Ovens != null &&
            GameManager.Instance.Config.kitchen != null;
    }
}

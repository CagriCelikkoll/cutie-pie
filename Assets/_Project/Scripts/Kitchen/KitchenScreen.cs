using CutiePie.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.Kitchen
{
    /// <summary>
    /// Mutfak sahnesinin ana ekranı: "Pastaneye dön" butonu.
    /// Fırın paneli (G19) ve sipariş panosu (G20) prefab'ları sahnedeki OvenPanelSlot / OrderBoardSlot alanlarına yerleştirilir.
    /// </summary>
    public class KitchenScreen : MonoBehaviour
    {
        [SerializeField] private Button backButton;

        private void Awake() => backButton.onClick.AddListener(HandleBack);

        private void OnDestroy() => backButton.onClick.RemoveListener(HandleBack);

        private void HandleBack()
        {
            // GameManager Boot'ta yaşar; sahnede doğrudan bağlanamadığı için buradan çağrılır.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToShop();
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Round sonu ekranından açılan geliştirmeler paneli. Şimdilik boş ("Yakında"); içerik G8 arayüzünde dolacak.
    /// </summary>
    public class UpgradesPanel : MonoBehaviour
    {
        [Tooltip("Açılıp kapanan içerik (perde + kart).")]
        [SerializeField] private GameObject content;

        [SerializeField] private Button closeButton;

        public bool IsOpen => content != null && content.activeSelf;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        public void Open()
        {
            if (content != null)
            {
                content.SetActive(true);
            }
        }

        public void Close()
        {
            if (content != null)
            {
                content.SetActive(false);
            }
        }
    }
}

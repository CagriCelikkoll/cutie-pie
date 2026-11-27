using CutiePie.Core;
using CutiePie.Core.Localization;

namespace CutiePie.UI
{
    /// <summary>
    /// Biçim argümanı olarak yerelleştirilmiş metin: string.Format her çağrıldığında anahtarı güncel dilden çözer.
    /// LocalizedText dil değişince yenilendiğinde "Sıradaki: {0}" gibi metinlerin içindeki ad da yeni dilde yazılır.
    /// </summary>
    internal sealed class LocalizedKeyArg
    {
        private readonly string key;

        public LocalizedKeyArg(string key) => this.key = key;

        public override string ToString()
        {
            LocalizationService service = GameManager.Instance != null ? GameManager.Instance.Localization : null;
            return service != null ? service.Get(key) : key ?? string.Empty;
        }
    }
}

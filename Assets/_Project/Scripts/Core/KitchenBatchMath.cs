using System;

namespace CutiePie.Core
{
    /// <summary>
    /// Parti ve dolap hesapları: kusursuz süsleme, partinin dilim sayısı ve dolap kapasitesinde kırpma.
    /// Unity'den ve SaveSystem'den bağımsızdır (Edit Mode testleri için); RecipeData, StockManager ve OvenService kullanır.
    /// </summary>
    public static class KitchenBatchMath
    {
        /// <summary>
        /// Seçilen krema ve süs, beklenen değerlere eşit mi. Beklenen değerlerden biri boşsa (tarifte atanmamış) false.
        /// Karşılaştırma büyük/küçük harfe duyarlıdır (id'ler değişmeyen kimliklerdir).
        /// </summary>
        public static bool IsPerfect(string expectedFrostingId, string expectedToppingId, string frostingId, string toppingId)
        {
            if (string.IsNullOrEmpty(expectedFrostingId) || string.IsNullOrEmpty(expectedToppingId))
            {
                return false;
            }

            return string.Equals(expectedFrostingId, frostingId, StringComparison.Ordinal) &&
                   string.Equals(expectedToppingId, toppingId, StringComparison.Ordinal);
        }

        /// <summary>Kusursuz süsleme bonusu (perfect değilse 0; negatif ayar 0 sayılır).</summary>
        public static int BonusSlices(bool perfect, int perfectBonusSlices) => perfect ? Math.Max(0, perfectBonusSlices) : 0;

        /// <summary>Partinin stoğa eklenmek istenen toplam dilimi: taban + bonus (negatifler 0 sayılır).</summary>
        public static int TotalSlices(int baseSlices, bool perfect, int perfectBonusSlices) =>
            Math.Max(0, baseSlices) + BonusSlices(perfect, perfectBonusSlices);

        /// <summary>
        /// Dolap kapasitesinde kırpma: current dilimli ürüne amount dilim eklenmek istenir.
        /// added: stoğa giren, lost: kapasite yüzünden giremeyen (added + lost = amount).
        /// Stok zaten kapasitenin üstündeyse (eski kayıt) budanmaz; added 0, lost amount olur.
        /// capacity 0 veya negatifse sınır yoktur (yalnızca int taşmasına karşı sabitlenir).
        /// amount 0 veya negatifse ikisi de 0.
        /// </summary>
        public static void ClampToCapacity(int current, int amount, int capacity, out int added, out int lost)
        {
            if (amount <= 0)
            {
                added = 0;
                lost = 0;
                return;
            }

            current = Math.Max(0, current);
            long limit = capacity > 0 ? capacity : int.MaxValue;
            long free = Math.Max(0L, limit - current);
            added = (int)Math.Min(amount, free);
            lost = amount - added;
        }
    }
}

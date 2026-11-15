using System;
using System.Collections.Generic;
using UnityEngine;

namespace CutiePie.Core.Data
{
    /// <summary>
    /// Bir özel sipariş pastasının içeriği. Hem fişteki hedef pastayı hem oyuncunun yaptığı pastayı taşır.
    /// Kayda (JsonUtility) yazılabilmesi için yalnızca alan ve liste kullanır.
    /// </summary>
    [Serializable]
    public class CakeBuild
    {
        /// <summary>Taban parçasının CakePartData.id değeri.</summary>
        public string baseId;

        /// <summary>Krema parçasının CakePartData.id değeri.</summary>
        public string creamId;

        /// <summary>Pastanın üstündeki süsler.</summary>
        public List<DecorationPlacement> decorations = new List<DecorationPlacement>();

        /// <summary>Kutu parçasının CakePartData.id değeri.</summary>
        public string boxId;
    }

    /// <summary>Pastanın üstüne konmuş tek bir süs.</summary>
    [Serializable]
    public class DecorationPlacement
    {
        /// <summary>Süs parçasının CakePartData.id değeri.</summary>
        public string decorationId;

        /// <summary>
        /// Pasta üstündeki konum, normalize pasta koordinatında (her eksen 0–1).
        /// Koordinat sistemi:
        /// - Referans dikdörtgen, ekranda görünen pasta görselinin süs konulabilen yüzeyini çevreleyen dikdörtgendir
        ///   (UI'da bu yüzeyin RectTransform'u).
        /// - (0,0) bu dikdörtgenin SOL ALT köşesi, (1,1) SAĞ ÜST köşesidir; (0.5, 0.5) pastanın ortasıdır.
        /// - x sağa, y yukarı doğru artar (Unity UI ile aynı yön; ekran y'si aşağı doğru değildir).
        /// - Konum, süs görselinin merkezidir (pivot 0.5, 0.5).
        /// - Uzaklıklar bu normalize uzayda ölçülür (decorationPositionTolerance, decorationMinSpacing). Dikdörtgen kare
        ///   değilse yatay ve dikey uzaklıklar ekranda farklı görünür; bu yüzden yüzey kareye yakın tutulmalıdır.
        /// - Fişler süsleri yalnızca KitchenSettings.decorationAreaMin..decorationAreaMax alt dikdörtgenine koyar
        ///   (pastanın kenarına taşmasın diye). Oyuncu süsü 0–1 içinde istediği yere koyabilir; ekran değeri 0–1'e sınırlamalıdır.
        /// UI'dan dönüşüm: RectTransformUtility ile yüzeyin yerel noktasını alıp Rect.PointToNormalized(rect, yerelNokta).
        /// </summary>
        public Vector2 position;
    }
}

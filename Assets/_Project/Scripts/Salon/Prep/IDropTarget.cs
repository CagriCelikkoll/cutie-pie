using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Sürüklenen bir servis parçasını kabul edebilen yer (tezgâh, tepsi, çöp...).
    /// Hedefler kendilerini DropTargetRegistry'ye kaydeder; bırakılan parça en yakın uygun hedefe gider.
    /// </summary>
    public interface IDropTarget
    {
        /// <summary>Parçanın oturacağı dünya konumu; en yakın hedef bu noktaya göre seçilir.</summary>
        Vector3 SnapPoint { get; }

        /// <summary>Parça şu an bu hedefe bırakılabilir mi? Durumu değiştirmez.</summary>
        bool CanAccept(ServingItem item);

        /// <summary>
        /// Parçayı alır: yerleştirir, sahipliğini üstlenir. Yalnızca CanAccept true döndüğünde çağrılır.
        /// Stoktan düşme gibi kalıcı işlemler yalnızca burada yapılır.
        /// </summary>
        void Accept(ServingItem item);
    }
}

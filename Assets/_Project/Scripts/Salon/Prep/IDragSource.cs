namespace CutiePie.Salon
{
    /// <summary>
    /// Sürüklenecek parçayı veren kaynak (tepsi yığını, vitrin yuvası, tezgâhtaki tepsi...).
    /// DragItem ile aynı GameObject üzerinde durur; sürükleme mantığı DragItem'da, oyun kuralları kaynaktadır.
    /// </summary>
    public interface IDragSource
    {
        /// <summary>
        /// Sürükleme başlarken taşınacak parçayı hazırlar (yeni üretir veya mevcut parçayı verir).
        /// Parça bu anda başlangıç konumunda olmalıdır. null dönerse sürükleme başlamaz.
        /// </summary>
        ServingItem BeginDrag();

        /// <summary>Parça bir hedefe kabul edildi; sahipliği artık hedefte.</summary>
        void OnDropped(ServingItem item, IDropTarget target);

        /// <summary>Parça hiçbir hedefe oturmadı ve başlangıç konumuna geri döndü.</summary>
        void OnReturned(ServingItem item);
    }
}

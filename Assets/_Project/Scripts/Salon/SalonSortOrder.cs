namespace CutiePie.Salon
{
    /// <summary>
    /// Salon'daki SpriteRenderer / SortingGroup sıralama değerleri. Sorting Layer (ortak dosya) yerine
    /// tek katmanda sortingOrder kullanılır. Hem çalışma zamanı kodu hem SalonSceneBuilder buradan okur.
    /// </summary>
    public static class SalonSortOrder
    {
        public const int Floor = 0;
        public const int Door = 1;

        /// <summary>Hazırlama alanında yer tutucu kutuların üstündeki parçalar (tepsi, tabak, dilim, bardak).</summary>
        public const int PrepItem = 5;

        public const int Table = 10;
        public const int TableLock = 11;
        public const int Customer = 20;
        public const int Overlay = 30;
        public const int OverlayContent = 31;

        /// <summary>Sürüklenen parça her şeyin üstünde çizilir.</summary>
        public const int Dragged = 100;
    }
}

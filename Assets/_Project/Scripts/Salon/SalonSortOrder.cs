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

        /// <summary>Hazırlama alanında sabit duran görseller (raf ikonları, fiş, sayfa okları).</summary>
        public const int PrepFixture = 3;

        /// <summary>Sabit görsellerin üstündeki ek (vitrindeki dilim sayısı).</summary>
        public const int PrepFixtureOverlay = 4;

        /// <summary>Hazırlama alanında taşınan parçalar (tepsi, tabak, dilim, bardak).</summary>
        public const int PrepItem = 5;

        public const int Table = 10;
        public const int TableLock = 11;
        public const int Customer = 20;
        public const int Overlay = 30;
        public const int OverlayContent = 31;

        /// <summary>Sürüklenen parça her şeyin üstünde çizilir.</summary>
        public const int Dragged = 100;

        // Parça içi (SortingGroup içinde) sıralar: gövde 0, üstüne konanlar 1+.
        public const int PlateInTray = 1;
        public const int DrinkInTray = 2;
        public const int SliceOnPlate = 1;

    }
}

namespace CutiePie.Upgrades
{
    /// <summary>Geliştirme id'leri; UpgradeData.id değerleriyle aynı olmalı.</summary>
    public static class UpgradeIds
    {
        /// <summary>Etki: fazladan açılan masa sayısı (toplam).</summary>
        public const string ExtraTable = "extra_table";

        /// <summary>Etki: tabaklı yeni tepsinin hazırlanma süresi (saniye).</summary>
        public const string ReadyTray = "ready_tray";

        /// <summary>Etki: içeceğin tepsiye gelme süresi (saniye).</summary>
        public const string AutoDrink = "auto_drink";

        /// <summary>Etki: garsonun tepsiyi masaya götürme süresi (saniye).</summary>
        public const string Waiter = "waiter";
    }
}

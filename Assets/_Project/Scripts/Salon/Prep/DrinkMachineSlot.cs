namespace CutiePie.Salon
{
    /// <summary>
    /// Tek içecek makinesi yeri (servis adımı 4): tepsiye bardak verir. İçeceklerde stok yoktur.
    /// TryPlaceDrink sürüklemesiz yoldur; ileride "İçecek makinesi" geliştirmesi bunu çağırır.
    /// </summary>
    public class DrinkMachineSlot : ShelfSlot
    {
        protected override ServingItemKind ItemKind => ServingItemKind.Drink;

        /// <summary>Sürüklemeden tepsiye bardak koyar (otomasyon girişi).</summary>
        public bool TryPlaceDrink(Tray tray) => TryPlaceOn(tray);
    }
}

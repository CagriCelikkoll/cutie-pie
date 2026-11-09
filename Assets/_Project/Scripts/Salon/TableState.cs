namespace CutiePie.Salon
{
    /// <summary>Salon'daki bir masa yerinin durumu.</summary>
    public enum TableState
    {
        /// <summary>Henüz açılmamış masa yeri (geliştirmeyle açılır).</summary>
        Locked = 0,

        /// <summary>Açık ve boş; müşteri atanabilir.</summary>
        Empty = 1,

        /// <summary>Bir müşteriye ayrılmış (müşteri yoldayken de dolu sayılır).</summary>
        Occupied = 2,

        /// <summary>Müşteri yiyip kalktı; masada boş tabak var, temizlenene kadar müşteri oturmaz.</summary>
        Dirty = 3
    }
}

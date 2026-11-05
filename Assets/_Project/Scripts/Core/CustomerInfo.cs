using CutiePie.Core.Data;

namespace CutiePie.Core
{
    /// <summary>
    /// Müşteri olaylarında taşınan hafif bilgi. Salon'daki Customer sınıfına bağımlılık yaratmamak için Core'da durur.
    /// </summary>
    public readonly struct CustomerInfo
    {
        /// <summary>Round içinde müşteriyi ayırt eden benzersiz numara.</summary>
        public readonly int CustomerId;

        /// <summary>Müşterinin tipi (sabır süresi, bahşiş çarpanı vb.).</summary>
        public readonly CustomerTypeData Type;

        /// <summary>Oturduğu masanın sırası; masada değilse -1.</summary>
        public readonly int TableIndex;

        public CustomerInfo(int customerId, CustomerTypeData type, int tableIndex)
        {
            CustomerId = customerId;
            Type = type;
            TableIndex = tableIndex;
        }
    }
}

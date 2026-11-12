namespace CutiePie.Kitchen
{
    /// <summary>Bir fırının durumu.</summary>
    public enum OvenState
    {
        /// <summary>Fırın boş; tarif konabilir.</summary>
        Empty = 0,

        /// <summary>Parti pişiyor.</summary>
        Baking = 1,

        /// <summary>Parti hazır; toplanmayı bekliyor.</summary>
        Ready = 2
    }
}

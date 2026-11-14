using UnityEngine;

namespace CutiePie.Core
{
    /// <summary>
    /// Yalnızca editörde etkili işaret. Build Settings dışındaki bir test sahnesine konursa, o sahnede Play'e basınca
    /// önce Boot yüklenir (yöneticiler ve kayıt hazırlanır), ardından aynı test sahnesi açılır.
    /// İşaret yoksa Build Settings dışı sahneler eskisi gibi Boot'a uğramadan açılır (ör. RoundTest).
    /// Kural: işaretli sahnede GameManager, SaveSystem, EconomyManager gibi Boot yöneticileri bulunmamalıdır;
    /// bunlar Boot'tan gelir, sahnedeki kopyalar yok edilir. Build'de hiçbir etkisi yoktur.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cutie Pie/Boot In Editor Marker")]
    public class BootInEditorMarker : MonoBehaviour
    {
    }
}

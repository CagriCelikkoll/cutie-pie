using System.Collections.Generic;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Sahnedeki masa yerlerini sırasıyla tutar; açık/kilitli kurulumunu ve boş masa seçimini yapar.
    /// </summary>
    public class TableManager : MonoBehaviour
    {
        [SerializeField] private SalonSettings settings;

        [Tooltip("Index sırasına göre masalar (liste sırası = Table.Index).")]
        [SerializeField] private List<Table> tables = new List<Table>();

        // Rastgele seçimde her seferinde yeni liste oluşturmamak için tekrar kullanılır.
        private readonly List<Table> freeBuffer = new List<Table>();

        public IReadOnlyList<Table> Tables => tables;

        /// <summary>İlk openCount masayı açar, gerisini kilitler.</summary>
        public void Setup(int openCount)
        {
            if (settings == null)
            {
                Debug.LogError("[TableManager] SalonSettings atanmamış.", this);
                return;
            }

            if (openCount > tables.Count)
            {
                Debug.LogWarning($"[TableManager] İstenen açık masa sayısı ({openCount}) masa yerinden ({tables.Count}) fazla.", this);
            }

            for (int i = 0; i < tables.Count; i++)
            {
                if (tables[i] != null)
                {
                    tables[i].Initialize(settings, i < openCount);
                }
            }
        }

        /// <summary>Boş masalardan rastgele birini verir; boş masa yoksa false döner.</summary>
        public bool TryGetRandomFreeTable(out Table table)
        {
            freeBuffer.Clear();
            foreach (Table t in tables)
            {
                if (t != null && t.IsFree)
                {
                    freeBuffer.Add(t);
                }
            }

            if (freeBuffer.Count == 0)
            {
                table = null;
                return false;
            }

            table = freeBuffer[Random.Range(0, freeBuffer.Count)];
            return true;
        }

        /// <summary>Sırası verilen masayı döner.</summary>
        public bool TryGetTable(int index, out Table table)
        {
            table = index >= 0 && index < tables.Count ? tables[index] : null;
            return table != null;
        }

        private void OnValidate()
        {
            // Liste sırası ile masaların Index değeri tutarlı olmalı.
            for (int i = 0; i < tables.Count; i++)
            {
                if (tables[i] != null && tables[i].Index != i)
                {
                    Debug.LogWarning($"[TableManager] Liste sırası {i} ama masanın Index değeri {tables[i].Index}.", tables[i]);
                }
            }
        }
    }
}

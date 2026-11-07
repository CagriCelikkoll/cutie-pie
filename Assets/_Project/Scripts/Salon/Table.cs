using System;
using UnityEngine;

namespace CutiePie.Salon
{
    /// <summary>
    /// Salon'daki tek bir masa yeri. Durumunu tutar ve görünümünü duruma göre günceller.
    /// </summary>
    public class Table : MonoBehaviour
    {
        [Tooltip("Masanın sırası (0-7). Üst sıra soldan sağa 0-3, alt sıra 4-7. Açık masalar ilk N sıradır.")]
        [SerializeField] private int index;

        [Tooltip("Müşterinin yürüyüp duracağı nokta (masanın üst kenarı).")]
        [SerializeField] private Transform seatPoint;

        [Tooltip("Rengi duruma göre değişen masa gövdesi.")]
        [SerializeField] private SpriteRenderer body;

        [Tooltip("Kilitliyken görünen kapak.")]
        [SerializeField] private GameObject lockVisual;

        private SalonSettings settings;

        public int Index => index;
        public TableState State { get; private set; } = TableState.Locked;
        public Transform SeatPoint => seatPoint != null ? seatPoint : transform;
        public bool IsFree => State == TableState.Empty;

        /// <summary>Durum her değiştiğinde tetiklenir.</summary>
        public event Action<Table> StateChanged;

        /// <summary>TableManager kurulumda çağırır: ayarları verir ve başlangıç durumunu belirler.</summary>
        public void Initialize(SalonSettings salonSettings, bool open)
        {
            settings = salonSettings;
            SetState(open ? TableState.Empty : TableState.Locked, true);
        }

        public void Lock() => SetState(TableState.Locked);

        public void Unlock()
        {
            if (State == TableState.Locked)
            {
                SetState(TableState.Empty);
            }
        }

        /// <summary>Boş masayı bir müşteriye ayırır. Masa boş değilse false döner.</summary>
        public bool Reserve()
        {
            if (State != TableState.Empty)
            {
                Debug.LogWarning($"[Table] {index} numaralı masa boş değil ({State}); ayrılamadı.", this);
                return false;
            }

            SetState(TableState.Occupied);
            return true;
        }

        /// <summary>Müşteri gidince (kızgın) masayı yeniden boşaltır.</summary>
        public void Release()
        {
            if (State == TableState.Occupied)
            {
                SetState(TableState.Empty);
            }
        }

        /// <summary>Mutlu teslimden sonra masa kirlenir (G3).</summary>
        public void MarkDirty()
        {
            if (State == TableState.Occupied)
            {
                SetState(TableState.Dirty);
            }
        }

        /// <summary>Kirli masa temizlenince boşalır (G3).</summary>
        public void Clean()
        {
            if (State == TableState.Dirty)
            {
                SetState(TableState.Empty);
            }
        }

        private void SetState(TableState newState, bool force = false)
        {
            if (!force && newState == State)
            {
                return;
            }

            State = newState;
            RefreshVisual();
            StateChanged?.Invoke(this);
        }

        private void RefreshVisual()
        {
            if (lockVisual != null)
            {
                lockVisual.SetActive(State == TableState.Locked);
            }

            if (body != null && settings != null)
            {
                body.color = settings.GetTableColor(State);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using CutiePie.Core.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CutiePie.UI
{
    /// <summary>
    /// Tezgâhın karıştırma kasesi (G23).
    /// Adım 1: malzemelerin bırakma alanıdır (ContainsScreenPoint). Malzeme düşünce kasede küçük simgesi belirir, hamur seviyesi
    /// yükselir, hamur kısa süre malzemenin rengine kayar ve kase hafifçe zıplar. Üstte "Kase · 3 / 6" yazar.
    /// Adım 2: BeginMixing'den sonra kasenin üstünde parmakla daire çizilir. Kase merkezine göre açı değişimi biriktirilir;
    /// yön serbesttir (ileri-geri de sayılır), merkeze çok yakın ya da tek olayda çok büyük (sıçrayan) hareketler sayılmaz.
    /// Biriken açı mixTurns x 360°'ye ulaşınca MixCompleted yayınlanır. İlerleme halkası dolar, hamur rengi koyulaşır,
    /// kasedeki malzeme simgeleri karıştıkça kaybolur, kaşık parmağı izler.
    /// Animasyonlar gerçek zamanla akar (Time.unscaledDeltaTime).
    /// </summary>
    public class MixingBowl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const string KeyCount = "kitchen.counter.bowl";
        private const float FullTurnDegrees = 360f;

        [SerializeField] private CounterSettings settings;

        [Tooltip("Bırakma ve karıştırma alanı (kasenin kendisi); merkez bunun ortasıdır.")]
        [SerializeField] private RectTransform dropArea;

        [Tooltip("Zıplama animasyonunun uygulandığı görsel grup (kase + hamur + simgeler).")]
        [SerializeField] private RectTransform visual;

        [SerializeField] private Image batter;

        [Tooltip("Kaseye düşen malzeme simgelerinin eklendiği kutu.")]
        [SerializeField] private RectTransform slotContainer;

        [Tooltip("Kapalı şablon simge (malzeme başına bir kopya).")]
        [SerializeField] private Image slotTemplate;

        [Tooltip("Radyal dolan ilerleme halkası (Image.Type.Filled).")]
        [SerializeField] private Image progressRing;

        [SerializeField] private GameObject progressTrack;
        [SerializeField] private RectTransform spoon;

        [Tooltip("Malzeme kasenin üstündeyken görünen parıltı.")]
        [SerializeField] private Image hoverGlow;

        [SerializeField] private LocalizedText countText;

        [Tooltip("Kaseye düşen simgelerin kase merkezine göre konumları (sırayla kullanılır).")]
        [SerializeField] private Vector2[] slotPositions =
        {
            new Vector2(-70f, 20f), new Vector2(60f, 40f), new Vector2(0f, -40f),
            new Vector2(-40f, 70f), new Vector2(80f, -30f), new Vector2(-90f, -40f),
            new Vector2(20f, 80f), new Vector2(40f, -80f)
        };

        /// <summary>Karıştırma ilerlemesi değişti (0–1).</summary>
        public event Action<float> MixProgressChanged;

        /// <summary>Karıştırma %100 oldu.</summary>
        public event Action MixCompleted;

        private readonly List<Image> slots = new List<Image>();
        private int total;
        private int count;
        private int mixTurns = 1;
        private Color startColor = Color.white;
        private Color readyColor = Color.white;
        private Color flashColor = Color.white;
        private float flashTime;
        private float popTime;
        private float accumulatedDegrees;
        private float lastAngle;
        private bool hasLastAngle;

        public bool IsMixing { get; private set; }

        public bool IsMixed { get; private set; }

        /// <summary>Karıştırma ilerlemesi (0–1).</summary>
        public float MixProgress => mixTurns > 0 ? Mathf.Clamp01(accumulatedDegrees / (mixTurns * FullTurnDegrees)) : 1f;

        private void Awake()
        {
            if (slotTemplate != null)
            {
                slotTemplate.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (settings == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (flashTime > 0f)
            {
                flashTime = Mathf.Max(0f, flashTime - dt);
                ApplyBatterColor();
            }

            if (popTime > 0f && visual != null)
            {
                popTime = Mathf.Max(0f, popTime - dt);

                // 0 → tepe → 0: yarım sinüs.
                float t = 1f - popTime / settings.dropPopDuration;
                float scale = 1f + (settings.dropPopScale - 1f) * Mathf.Sin(t * Mathf.PI);
                visual.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // ------------------------------------------------------------------ Kurulum

        /// <summary>Yeni tarif için kaseyi boşaltır.</summary>
        public void ResetBowl(int ingredientCount, int turns, Color batterStart, Color batterReady)
        {
            total = Mathf.Max(0, ingredientCount);
            count = 0;
            mixTurns = Mathf.Max(1, turns);
            startColor = batterStart;
            readyColor = batterReady;
            flashTime = 0f;
            popTime = 0f;
            accumulatedDegrees = 0f;
            hasLastAngle = false;
            IsMixing = false;
            IsMixed = false;

            foreach (Image slot in slots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            slots.Clear();

            if (visual != null)
            {
                visual.localScale = Vector3.one;
            }

            KitchenUIUtil.SetActive(batter, false);
            KitchenUIUtil.SetActive(progressRing, false);
            KitchenUIUtil.SetActive(progressTrack, false);
            KitchenUIUtil.SetActive(spoon, false);
            SetHover(false);
            ApplyBatterColor();
            UpdateCountText();
        }

        // ------------------------------------------------------------------ Adım 1: malzemeler

        /// <summary>Ekran noktası kasenin üstünde mi (Overlay Canvas'ta camera null).</summary>
        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera) =>
            dropArea != null && RectTransformUtility.RectangleContainsScreenPoint(dropArea, screenPoint, eventCamera);

        public void SetHover(bool hover)
        {
            if (hoverGlow != null)
            {
                hoverGlow.enabled = hover;
                if (hover && settings != null)
                {
                    hoverGlow.color = settings.bowlHoverColor;
                }
            }
        }

        /// <summary>Malzeme kaseye düştü: simge, hamur seviyesi, renk kayması ve zıplama.</summary>
        public void AddIngredient(IngredientData ingredient, Sprite iconSprite, Color iconColor, Image.Type iconType)
        {
            count++;
            SetHover(false);

            if (slotTemplate != null && slotContainer != null)
            {
                Image slot = Instantiate(slotTemplate, slotContainer, false);
                slot.name = ingredient != null ? $"Slot_{ingredient.id}" : "Slot";
                slot.sprite = iconSprite;
                slot.type = iconType;
                slot.preserveAspect = iconType == Image.Type.Simple;
                slot.color = new Color(iconColor.r, iconColor.g, iconColor.b, 1f);
                ((RectTransform)slot.transform).anchoredPosition =
                    slotPositions != null && slotPositions.Length > 0 ? slotPositions[slots.Count % slotPositions.Length] : Vector2.zero;
                slot.gameObject.SetActive(true);
                slots.Add(slot);
            }

            KitchenUIUtil.SetActive(batter, true);
            if (batter != null && settings != null)
            {
                float level = total > 1 ? (count - 1f) / (total - 1f) : 1f;
                float scale = Mathf.Lerp(settings.batterMinScale, settings.batterMaxScale, level);
                batter.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            if (settings != null)
            {
                flashColor = ingredient != null ? ingredient.bowlTint : startColor;
                flashTime = settings.dropFlashDuration;
                popTime = settings.dropPopDuration;
            }

            ApplyBatterColor();
            UpdateCountText();
        }

        // ------------------------------------------------------------------ Adım 2: karıştırma

        public void BeginMixing()
        {
            IsMixing = true;
            IsMixed = false;
            accumulatedDegrees = 0f;
            hasLastAngle = false;
            KitchenUIUtil.SetActive(progressTrack, true);
            KitchenUIUtil.SetActive(progressRing, true);
            KitchenUIUtil.SetActive(spoon, true);
            if (spoon != null)
            {
                spoon.anchoredPosition = Vector2.zero;
            }

            ApplyMixProgress();
        }

        /// <summary>Karıştırmayı hemen bitirir (test aracı).</summary>
        public void CompleteMixing()
        {
            if (!IsMixing)
            {
                return;
            }

            accumulatedDegrees = mixTurns * FullTurnDegrees;
            ApplyMixProgress();
            FinishMixing();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            hasLastAngle = false;
            if (IsMixing)
            {
                TrackPointer(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (IsMixing)
            {
                TrackPointer(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData) => hasLastAngle = false;

        private void TrackPointer(PointerEventData eventData)
        {
            if (dropArea == null || settings == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(dropArea, eventData.position,
                    eventData.pressEventCamera, out Vector2 local))
            {
                return;
            }

            Vector2 offset = local - dropArea.rect.center;
            if (spoon != null)
            {
                spoon.anchoredPosition = Vector2.ClampMagnitude(offset, settings.spoonMaxRadius);
            }

            // Merkeze çok yakınken açı anlamsızdır; bir sonraki geçerli noktadan yeniden başlanır.
            if (offset.magnitude < settings.minMixRadius)
            {
                hasLastAngle = false;
                return;
            }

            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            if (hasLastAngle)
            {
                float delta = Mathf.Abs(Mathf.DeltaAngle(lastAngle, angle));

                // Sıçrama (ör. parmağın kasenin karşı tarafına atlaması) sayılmaz.
                if (delta <= settings.maxMixStepDegrees)
                {
                    accumulatedDegrees += delta;
                    ApplyMixProgress();
                    if (MixProgress >= 1f)
                    {
                        FinishMixing();
                        return;
                    }
                }
            }

            lastAngle = angle;
            hasLastAngle = true;
        }

        private void FinishMixing()
        {
            IsMixing = false;
            IsMixed = true;
            hasLastAngle = false;
            KitchenUIUtil.SetActive(spoon, false);
            MixCompleted?.Invoke();
        }

        private void ApplyMixProgress()
        {
            float progress = MixProgress;
            if (progressRing != null)
            {
                progressRing.fillAmount = progress;
            }

            // Malzeme simgeleri karıştıkça hamura karışıp kaybolur.
            foreach (Image slot in slots)
            {
                if (slot != null)
                {
                    Color color = slot.color;
                    color.a = 1f - progress;
                    slot.color = color;
                }
            }

            ApplyBatterColor();
            MixProgressChanged?.Invoke(progress);
        }

        // ------------------------------------------------------------------ Görünüm

        // Taban renk: karıştırmada başlangıçtan hazır renge; malzeme düşünce kısa süre malzemenin rengine kayar.
        private void ApplyBatterColor()
        {
            if (batter == null)
            {
                return;
            }

            Color baseColor = IsMixing || IsMixed ? Color.Lerp(startColor, readyColor, MixProgress) : startColor;
            if (flashTime > 0f && settings != null && settings.dropFlashDuration > 0f)
            {
                float k = flashTime / settings.dropFlashDuration * settings.dropFlashStrength;
                baseColor = Color.Lerp(baseColor, flashColor, k);
            }

            baseColor.a = 1f;
            batter.color = baseColor;
        }

        private void UpdateCountText() => KitchenUIUtil.SetKey(countText, KeyCount, count, total);
    }
}

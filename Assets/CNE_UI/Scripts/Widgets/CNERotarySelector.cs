using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Izgaranin isi dugmesi gibi kademeli doner secici (ayarlardaki "Gorunum").
    /// Kademeler koddan kurulur (SetOptions): kayitli gorunum sayisi degisirse dugme kendini yeniden dizer
    /// (1-5 kademe; ikiden fazlasinda uclar +/- edgeAngle, aradakiler esit aralikli).
    /// Dugmeye tiklamak sonraki kademeye cevirir; etikete tiklamak o kademeyi secer; klavyede sol/sag.
    /// Her kademede "tik" sesi, dugme hafifce asarak oturur, secili etiketin lambasi yanar.
    /// </summary>
    [AddComponentMenu("Cook No Evil/UI/Doner Secici")]
    public class CNERotarySelector : Selectable, IPointerClickHandler, ISubmitHandler
    {
        [System.Serializable]
        public class IntEvent : UnityEvent<int> { }

        [Tooltip("Donen dugme")] public RectTransform knob;
        [Tooltip("Her kademenin dugme acisi (derece; pozitif = sola). SetOptions doldurur.")] public float[] angles = { 55f, 0f, -55f };
        [Tooltip("Kademe etiketleri (sirayla). SetOptions doldurur.")] public TMP_Text[] optionLabels = new TMP_Text[0];
        [Tooltip("Kademe lambalari (sirayla). SetOptions doldurur.")] public Graphic[] optionLamps = new Graphic[0];
        public Color labelOn = new Color(0.141f, 0.137f, 0.227f, 1f);   // #24233A
        public Color labelOff = new Color(0.369f, 0.416f, 0.451f, 1f);  // #5E6A73
        public Color lampOn = new Color(0.373f, 0.890f, 0.878f, 1f);    // #5FE3E0
        public Color lampOff = new Color(0.369f, 0.416f, 0.451f, 1f);
        [SerializeField] int index;
        public IntEvent onValueChanged = new IntEvent();

        [Header("Kademe dizilimi")]
        [Tooltip("Pasif kademe sablonu (CNERotarySlot)")] public CNERotarySlot slotTemplate;
        [Tooltip("Kademelerin dizildigi kap")] public RectTransform slotRoot;
        [SerializeField] List<CNERotarySlot> slots = new List<CNERotarySlot>();
        [Tooltip("Dugme merkezi (alt-orta capaya gore)")] public Vector2 center = new Vector2(0f, 50f);
        public float tickRadius = 56f;
        public float lampRadius = 74f;
        public float labelRadius = 100f;
        public float labelLift = 4f;
        public Vector2 labelSize = new Vector2(128f, 30f);
        [Tooltip("Uc kademelerin acisi (uc kademede +/-55)")] public float edgeAngle = 55f;
        [Tooltip("Cok kademede uclarin cikabilecegi en buyuk aci")] public float maxEdgeAngle = 85f;
        [Tooltip("Etiketler arasi en az bosluk")] public float labelGap = 6f;

        Coroutine tickRoutine;

        public int Count { get { return angles != null ? angles.Length : 0; } }

        public int Value
        {
            get { return index; }
            set { SetIndex(value, true, true); }
        }

        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyVisuals(index, false, index);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CNETween.Kill(this);
            tickRoutine = null;
        }

        /// <summary>
        /// Kademeleri verilen seceneklere gore kurar: gerekirse sablondan yeni kademe uretir, fazlasini gizler,
        /// acilari ve etiket genisliklerini hesaplar. Secim (index) korunur, sigmazsa son kademeye cekilir.
        /// </summary>
        public void SetOptions(IList<CNEChoice> choices)
        {
            int n = choices != null ? choices.Count : 0;
            EnsureSlots(n);

            angles = new float[n];
            optionLabels = new TMP_Text[n];
            optionLamps = new Graphic[n];
            var labelCenters = new Vector2[n];

            for (int i = 0; i < n; i++)
            {
                float angle = AngleFor(i, n);
                angles[i] = angle;
                float rad = angle * Mathf.Deg2Rad;
                var dir = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
                labelCenters[i] = center + dir * labelRadius + new Vector2(0f, labelLift);

                var slot = slots[i];
                slot.gameObject.SetActive(true);
                if (slot.tick != null)
                {
                    slot.tick.anchoredPosition = center + dir * tickRadius;
                    slot.tick.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
                if (slot.lamp != null) slot.lamp.anchoredPosition = center + dir * lampRadius;
                if (slot.option != null)
                {
                    slot.option.owner = this;
                    slot.option.option = i;
                }

                // Once duz ad (anahtarli seceneklerde tablo yuklenene kadarki yedek), sonra varsa tablodaki ceviri.
                var choice = choices[i];
                bool keyed = !string.IsNullOrEmpty(choice.labelKey) && slot.localized != null;
                if (slot.label != null) slot.label.text = choice.label ?? string.Empty;
                if (slot.localized != null)
                {
                    slot.localized.enabled = keyed;
                    if (keyed)
                    {
                        slot.localized.SetKey(choice.labelKey);
                        slot.localized.Reapply();
                    }
                }

                optionLabels[i] = slot.label;
                optionLamps[i] = slot.lampGraphic;
            }

            // Etiket genisligi: ayni yukseklikteki en yakin komsuyla cakismasin.
            for (int i = 0; i < n; i++)
            {
                float width = labelSize.x;
                for (int j = 0; j < n; j++)
                {
                    if (j == i || Mathf.Abs(labelCenters[i].y - labelCenters[j].y) >= labelSize.y) continue;
                    width = Mathf.Min(width, Mathf.Abs(labelCenters[i].x - labelCenters[j].x) - labelGap);
                }
                var rt = slots[i].labelRect;
                if (rt == null) continue;
                rt.anchoredPosition = labelCenters[i];
                rt.sizeDelta = new Vector2(Mathf.Max(48f, width), labelSize.y);
            }

            index = n == 0 ? 0 : Mathf.Clamp(index, 0, n - 1);
            ApplyVisuals(index, false, index);
        }

        float AngleFor(int i, int n)
        {
            if (n <= 1) return 0f;
            if (n == 2) return i == 0 ? edgeAngle : -edgeAngle;
            float edge = Mathf.Min(maxEdgeAngle, edgeAngle * Mathf.Max(1f, (n - 1) * 0.5f));
            return Mathf.Lerp(edge, -edge, i / (float)(n - 1));
        }

        void EnsureSlots(int n)
        {
            for (int i = slots.Count - 1; i >= 0; i--)
                if (slots[i] == null) slots.RemoveAt(i);
            while (slots.Count < n && slotTemplate != null)
            {
                var parent = slotRoot != null ? slotRoot : (RectTransform)transform;
                var slot = Instantiate(slotTemplate, parent);
                slot.name = "Slot" + slots.Count;
                slots.Add(slot);
            }
            for (int i = 0; i < slots.Count; i++)
                if (i >= n) slots[i].gameObject.SetActive(false);
            if (slotTemplate != null) slotTemplate.gameObject.SetActive(false);
        }

        public void SetWithoutNotify(int value)
        {
            SetIndex(value, false, false);
        }

        /// <summary>Etiketten secim (CNERotaryOption cagirir).</summary>
        public void SelectOption(int option)
        {
            if (!IsInteractable()) return;
            SetIndex(option, true, true);
        }

        void SetIndex(int value, bool notify, bool animate)
        {
            int n = Count;
            if (n == 0) return;
            value = ((value % n) + n) % n;
            int previous = index;
            index = value;
            ApplyVisuals(previous, animate, value);
            if (notify && previous != value) onValueChanged.Invoke(value);
        }

        void ApplyVisuals(int from, bool animate, int to)
        {
            if (Count == 0) return;
            from = Mathf.Clamp(from, 0, Count - 1);
            to = Mathf.Clamp(to, 0, Count - 1);

            for (int i = 0; i < optionLabels.Length; i++)
                if (optionLabels[i] != null) optionLabels[i].color = i == to ? labelOn : labelOff;
            for (int i = 0; i < optionLamps.Length; i++)
                if (optionLamps[i] != null) optionLamps[i].color = i == to ? lampOn : lampOff;

            if (knob == null) return;
            float targetAngle = angles[to];
            if (!animate || !Application.isPlaying || !isActiveAndEnabled)
            {
                knob.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
                return;
            }

            int steps = Mathf.Abs(to - from);
            if (steps > 0)
            {
                if (tickRoutine != null) StopCoroutine(tickRoutine);
                tickRoutine = StartCoroutine(Ticks(steps));
            }

            var k = knob;
            float startAngle = k.localEulerAngles.z;
            if (startAngle > 180f) startAngle -= 360f;
            CNETween.To(this, "knob", 0.12f + 0.07f * steps, t =>
            {
                k.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(startAngle, targetAngle, t));
            }, t => CNEEase.OutBack(t, 1.6f));

            if (optionLabels.Length > to && optionLabels[to] != null)
            {
                var rt = optionLabels[to].rectTransform;
                CNETween.To(this, "label", 0.2f, t =>
                {
                    float s = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
                    rt.localScale = new Vector3(s, s, 1f);
                }, CNEEase.Linear, () => rt.localScale = Vector3.one);
            }
        }

        IEnumerator Ticks(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                CNEUIAudio.Play(CNEUISound.KnobTick);
                yield return new WaitForSecondsRealtime(0.06f);
            }
            tickRoutine = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !IsInteractable()) return;
            SetIndex(index + 1, true, true);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsInteractable()) return;
            SetIndex(index + 1, true, true);
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (IsInteractable())
            {
                if (eventData.moveDir == MoveDirection.Left) { SetIndex(index - 1, true, true); eventData.Use(); return; }
                if (eventData.moveDir == MoveDirection.Right) { SetIndex(index + 1, true, true); eventData.Use(); return; }
            }
            base.OnMove(eventData);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsInteractable()) CNEUIAudio.Play(CNEUISound.Hover);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            if (!(eventData is PointerEventData) && !CNEButtonFeedback.SilentSelect && IsInteractable()) CNEUIAudio.Play(CNEUISound.Hover);
        }
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZeroHour.Phase2.Calls
{
    public enum FeedTone
    {
        Info,     // Sevk, ekip olay yerinde
        Alert,    // Yeni olay
        Success,  // Kurtarıldı
        Loss,     // Kayıp
        Neutral   // Asılsız ihbar, helikopter dönüşü
    }

    /// <summary>
    /// Sağ alttaki telefon: olay günlüğünü (sevk, yaralı, kayıp, kurtarılan) mesaj balonları olarak gösterir.
    /// Yeni mesaj alttan "bip" sesiyle belirir, eskiler yukarı kayar ve soluklaşır.
    /// </summary>
    public class PhoneFeed : MonoBehaviour
    {
        public static PhoneFeed Instance { get; private set; }

        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private TMP_Text statusClockText;
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Range(0f, 1f)] private float notifyVolume = 0.35f;
        [SerializeField] private int maxMessages = 6;
        [SerializeField] private float fontSize = 19f;

        private class Bubble
        {
            public RectTransform rect;
            public CanvasGroup group;
            public TMP_Text text;
            public TMP_Text time;
            public string key;
            public string englishFormat;
            public object[] args;
            public float age;
        }

        private readonly List<Bubble> bubbles = new List<Bubble>();
        private float lastNotifyTime = -10f;
        private bool layoutDirty;

        private const float Margin = 8f, Spacing = 7f;
        private const float PadLeft = 16f, PadRight = 10f, PadTop = 7f, PadBottom = 5f, TimeHeight = 14f;

        private static readonly Color BubbleColor = new Color(0.16f, 0.17f, 0.18f, 0.96f);
        private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color TimeColor = new Color(0.55f, 0.58f, 0.62f);

        private void Awake()
        {
            Instance = this;
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            Loc.Changed += RelocalizeAll;
        }

        private void OnDisable()
        {
            Loc.Changed -= RelocalizeAll;
        }

        /// <summary>
        /// Telefona mesaj düşürür. Aynı anahtarlı mesaj en alttaysa yenisi onun yerine yazılır (örn. artan kayıp sayısı).
        /// Metin İngilizce kalıp + değerler olarak saklanır; dil değişince tüm mesajlar yeni dilde yeniden yazılır.
        /// Değerlerden metin olanlar (ör. ekip adı) da çevrilir.
        /// </summary>
        public static void Post(FeedTone tone, string key, string englishFormat, params object[] args)
        {
            if (Instance != null) Instance.Add(tone, key, englishFormat, args);
        }

        private static string Render(string englishFormat, object[] args)
        {
            object[] localized = new object[args.Length];
            for (int i = 0; i < args.Length; i++) localized[i] = args[i] is string s ? Loc.T(s) : args[i];
            return Loc.F(englishFormat, localized);
        }

        private void RelocalizeAll()
        {
            foreach (Bubble b in bubbles) b.text.text = Render(b.englishFormat, b.args);
            layoutDirty = true; // Türkçe metin daha uzun / kısa olabilir: balon yükseklikleri yeniden hesaplanır
        }

        private void Add(FeedTone tone, string key, string englishFormat, object[] args)
        {
            if (content == null) return;

            string message = Render(englishFormat, args);

            string clock = IncidentBoard.Instance != null ? IncidentBoard.Instance.GetClockTime() : "";
            Bubble last = bubbles.Count > 0 ? bubbles[bubbles.Count - 1] : null;
            if (key != null && last != null && last.key == key)
            {
                last.text.text = message;
                last.englishFormat = englishFormat;
                last.args = args;
                last.time.text = clock;
                last.age = 0f;
                layoutDirty = true;
                Notify();
                return;
            }

            Bubble bubble = CreateBubble(message, ToneColor(tone), clock);
            bubble.key = key;
            bubble.englishFormat = englishFormat;
            bubble.args = args;
            bubbles.Add(bubble);

            while (bubbles.Count > maxMessages)
            {
                bubbles[0].rect.gameObject.SetActive(false); // Bu karedeki düzene girmesin
                Destroy(bubbles[0].rect.gameObject);
                bubbles.RemoveAt(0);
            }
            layoutDirty = true;
            Notify();
        }

        private void Notify()
        {
            // Aynı anda gelen mesajlar tek bip
            if (audioSource == null || Time.unscaledTime - lastNotifyTime < 0.25f) return;
            lastNotifyTime = Time.unscaledTime;
            audioSource.PlayOneShot(ProceduralCallAudio.Notify, notifyVolume);
        }

        private static Color ToneColor(FeedTone tone)
        {
            switch (tone)
            {
                case FeedTone.Info: return new Color(0.31f, 0.76f, 0.97f);
                case FeedTone.Alert: return new Color(1f, 0.62f, 0.2f);
                case FeedTone.Success: return new Color(0.47f, 0.88f, 0.69f);
                case FeedTone.Loss: return new Color(0.9f, 0.33f, 0.28f);
                default: return new Color(0.62f, 0.62f, 0.62f);
            }
        }

        private Bubble CreateBubble(string message, Color accent, string clock)
        {
            GameObject go = new GameObject("Message", typeof(RectTransform));
            go.layer = gameObject.layer;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(content, false);
            // Alt kenara yaslı, genişlik ekran kadar; yükseklik ve dikey konum LateUpdate'te elle hesaplanır
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(Margin, 0f);
            rt.offsetMax = new Vector2(-Margin, 0f);

            Image bg = go.AddComponent<Image>();
            bg.color = BubbleColor;
            bg.raycastTarget = false;

            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            // Sol renk şeridi: mesajın türü (sevk / yeni olay / kurtarma / kayıp)
            GameObject barGo = new GameObject("Accent", typeof(RectTransform));
            barGo.layer = go.layer;
            RectTransform bar = (RectTransform)barGo.transform;
            bar.SetParent(rt, false);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.sizeDelta = new Vector2(5f, 0f);
            bar.anchoredPosition = Vector2.zero;
            Image barImg = barGo.AddComponent<Image>();
            barImg.color = accent;
            barImg.raycastTarget = false;

            TMP_Text text = NewText("Text", rt, message, fontSize, TextColor, TextAlignmentOptions.TopLeft);
            text.textWrappingMode = TextWrappingModes.Normal;
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.offsetMin = new Vector2(PadLeft, 0f);
            textRt.offsetMax = new Vector2(-PadRight, -PadTop);

            TMP_Text time = NewText("Time", rt, clock, fontSize * 0.72f, TimeColor, TextAlignmentOptions.BottomRight);
            RectTransform timeRt = time.rectTransform;
            timeRt.anchorMin = new Vector2(0f, 0f);
            timeRt.anchorMax = new Vector2(1f, 0f);
            timeRt.pivot = new Vector2(0.5f, 0f);
            timeRt.offsetMin = new Vector2(PadLeft, PadBottom);
            timeRt.offsetMax = new Vector2(-PadRight, PadBottom + TimeHeight);

            rt.localScale = Vector3.one * 0.85f;
            return new Bubble { rect = rt, group = group, text = text, time = time };
        }

        private TMP_Text NewText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = value;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.richText = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void LateUpdate()
        {
            if (!layoutDirty || content == null) return;
            layoutDirty = false;

            // En yeni mesaj en altta; yukarı doğru dizilir, ekranı aşan eskiler üstten kırpılır (RectMask2D)
            float textWidth = content.rect.width - Margin * 2f - PadLeft - PadRight;
            float y = Margin;
            for (int i = bubbles.Count - 1; i >= 0; i--)
            {
                Bubble b = bubbles[i];
                float textHeight = b.text.GetPreferredValues(b.text.text, textWidth, 0f).y;
                b.text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
                float height = PadTop + textHeight + 2f + TimeHeight + PadBottom;
                b.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                b.rect.anchoredPosition = new Vector2(0f, y);
                y += height + Spacing;
            }
        }

        private void Update()
        {
            if (statusClockText != null && IncidentBoard.Instance != null) statusClockText.text = IncidentBoard.Instance.GetClockTime();

            // Yeni mesaj büyüyerek belirir; eski mesajlar soluklaşır (ayarlar açıkken de akıcı)
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < bubbles.Count; i++)
            {
                Bubble b = bubbles[i];
                b.age += dt;
                float pop = Mathf.Clamp01(b.age / 0.22f);
                float fromNewest = bubbles.Count - 1 - i;
                float targetAlpha = fromNewest < 3 ? 1f : Mathf.Max(0.35f, 1f - (fromNewest - 2) * 0.22f);
                b.group.alpha = Mathf.Min(pop, targetAlpha);
                b.rect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1f - (1f - pop) * (1f - pop));
            }
        }
    }
}

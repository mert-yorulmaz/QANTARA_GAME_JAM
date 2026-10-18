using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using ZeroHour.Phase2.Calls;
using ZeroHour.Shared;

namespace ZeroHour.MainMenu
{
    /// <summary>
    /// SIGN ana menüsü (Yağız'ın 00_main_menu tasarımı): döngüdeki şehir videosu, kayan "CANLI" haber bandı,
    /// alt vardiya dosyası çubuğu. VARDİYAYA BAŞLA ekranı karartıp Faz 1'i (ofis) açar.
    /// Ayarlar düğmesi ortak ayarlar panelini OpenSettingsButton ile açar; ESC her sahnede olduğu gibi burada da çalışır.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Arka Plan")]
        [SerializeField] private RawImage background;
        [Tooltip("Video hazır olana kadar (veya açılamazsa) gösterilen sabit görüntü")]
        [SerializeField] private Texture fallbackImage;
        [SerializeField] private VideoPlayer video;

        [Header("Haber Bandı")]
        [SerializeField] private TMP_Text tickerText;
        [SerializeField] private float tickerSpeed = 90f;

        [Header("Düğmeler")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [Tooltip("Tıklama sesi çalan tüm düğmeler")]
        [SerializeField] private Button[] clickButtons;

        [Header("Ses")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private MusicVolume music;

        [Header("Geçiş")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeDuration = 0.8f;
        [Tooltip("VARDİYAYA BAŞLA ile açılan sahne (Faz 1)")]
        [SerializeField] private string firstScene = "Phase1_Office";

        // Haberler İngilizce anahtar; seçili dilde yazılır
        private static readonly string[] Headlines =
        {
            "Aftershocks expected tonight",
            "Looting rumors in the market district, unconfirmed",
            "All emergency lines staffed",
            "Smoke reported near Oak Street",
            "Residents urged to stay away from damaged buildings",
            "Building inspection reports under review after the quake"
        };

        private RenderTexture videoTarget;
        private float tickerLoopWidth;
        private bool starting;

        private void Awake()
        {
            // Faz 2 karnesinden (oyun durmuşken) dönülebilir; menü fareyle kullanılır
            Time.timeScale = 1f;
            RunLoadout.Clear(); // Yeni tur: önceki turun gece ekibi taşınmasın
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (background != null && fallbackImage != null) background.texture = fallbackImage;
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 1f; // Siyahtan açılır
                fadeOverlay.blocksRaycasts = false;
            }

            if (startButton != null) startButton.onClick.AddListener(StartShift);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            foreach (Button b in clickButtons)
                if (b != null) b.onClick.AddListener(PlayClick);
        }

        private void Start()
        {
            StartVideo();
            BuildTicker();
            StartCoroutine(Fade(1f, 0f));
        }

        private void OnEnable()
        {
            Loc.Changed += BuildTicker;
        }

        private void OnDisable()
        {
            Loc.Changed -= BuildTicker;
        }

        private void OnDestroy()
        {
            if (videoTarget != null) videoTarget.Release();
        }

        // ---------------- Arka plan videosu ----------------

        private void StartVideo()
        {
            if (video == null || video.clip == null) return;

            videoTarget = new RenderTexture((int)video.clip.width, (int)video.clip.height, 0);
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = videoTarget;
            video.isLooping = true;
            video.audioOutputMode = VideoAudioOutputMode.None;

            // Sabit görüntü, videonun ilk karesi gerçekten çizilene kadar ekranda kalır (siyah kare görünmesin)
            video.sendFrameReadyEvents = true;
            video.frameReady += OnFirstVideoFrame;
            video.prepareCompleted += vp => vp.Play();
            video.errorReceived += (vp, message) => Debug.LogWarning($"[MainMenu] Arka plan videosu oynatılamadı, sabit görüntü kullanılıyor: {message}");
            video.Prepare();
        }

        private void OnFirstVideoFrame(VideoPlayer vp, long frameIndex)
        {
            if (background != null) background.texture = videoTarget;
            vp.frameReady -= OnFirstVideoFrame;
            vp.sendFrameReadyEvents = false;
        }

        // ---------------- Haber bandı ----------------

        private void BuildTicker()
        {
            if (tickerText == null) return;

            string separator = "      ·      ";
            string once = "";
            foreach (string h in Headlines) once += Loc.T(h) + separator;

            // İki kez yazılır: ilk tur sola kayıp bitince başa sarar, boşluk görünmez
            tickerText.text = once + once;
            tickerText.ForceMeshUpdate();
            tickerLoopWidth = tickerText.GetPreferredValues(once).x;

            RectTransform rt = tickerText.rectTransform;
            rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
            rt.sizeDelta = new Vector2(tickerLoopWidth * 2f + 50f, rt.sizeDelta.y);
        }

        private void Update()
        {
            if (tickerText == null || tickerLoopWidth <= 0f) return;

            RectTransform rt = tickerText.rectTransform;
            float x = rt.anchoredPosition.x - tickerSpeed * Time.unscaledDeltaTime;
            if (x <= -tickerLoopWidth) x += tickerLoopWidth;
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
        }

        // ---------------- Düğmeler ----------------

        private void PlayClick()
        {
            if (sfxSource != null && clickClip != null) sfxSource.PlayOneShot(clickClip);
        }

        public void StartShift()
        {
            if (starting) return;
            starting = true;
            StartCoroutine(StartShiftRoutine());
        }

        private IEnumerator StartShiftRoutine()
        {
            yield return Fade(0f, 1f);
            SceneManager.LoadScene(firstScene);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // Ekran kararır / açılır; müzik de aynı oranda kısılır / açılır
        private IEnumerator Fade(float from, float to)
        {
            if (fadeOverlay != null) fadeOverlay.blocksRaycasts = true;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                float a = Mathf.Lerp(from, to, t / fadeDuration);
                if (fadeOverlay != null) fadeOverlay.alpha = a;
                if (music != null) music.Fade = 1f - a;
                yield return null;
            }
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = to;
                fadeOverlay.blocksRaycasts = to > 0.5f;
            }
            if (music != null) music.Fade = 1f - to;
        }
    }
}

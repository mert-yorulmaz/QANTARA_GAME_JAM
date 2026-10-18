using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ZeroHour.Phase2.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("--- PANEL ROOT ---")]
        [Tooltip("Ayarlar penceresinin ana GameObject'i (Açılıp kapanacak panel)")]
        [SerializeField] private GameObject settingsPanelRoot;

        [Header("--- 1. SES (SOUND) AYARI ---")]
        [Tooltip("Ses efektleri seviye Slider'ı (0.0 - 1.0)")]
        [SerializeField] private Slider soundSlider;
        [Tooltip("Ses yüzdesi Text'i (Örn: 70%)")]
        [SerializeField] private TMP_Text soundPercentText;

        [Header("--- 2. MÜZİK (MUSIC) AYARI ---")]
        [Tooltip("Müzik seviye Slider'ı (0.0 - 1.0)")]
        [SerializeField] private Slider musicSlider;
        [Tooltip("Müzik yüzdesi Text'i (Örn: 50%)")]
        [SerializeField] private TMP_Text musicPercentText;

        [Header("--- 3. DİL (LANGUAGE) AYARI ---")]
        [Tooltip("Mevcut dili gösteren Text (ENGLISH / TÜRKÇE)")]
        [SerializeField] private TMP_Text languageText;
        [Tooltip("Önceki dil butonu (<)")]
        [SerializeField] private Button prevLanguageButton;
        [Tooltip("Sonraki dil butonu (>)")]
        [SerializeField] private Button nextLanguageButton;

        [Header("--- 4. TAM EKRAN (FULLSCREEN) AYARI ---")]
        [Tooltip("Tam ekran açık/kapalı durumunu gösteren Text veya Buton")]
        [SerializeField] private Button fullscreenOffButton;
        [SerializeField] private Button fullscreenOnButton;
        [SerializeField] private Image fullscreenOffHighlight;
        [SerializeField] private Image fullscreenOnHighlight;

        [Header("--- 5. AKSİYON BUTONLARI ---")]
        [Tooltip("Varsayılan ayarlara sıfırla butonu")]
        [SerializeField] private Button defaultsButton;
        [Tooltip("Geri / Kapat butonu (BACK)")]
        [SerializeField] private Button backButton;
        [Tooltip("Sağ üstteki kapatma (X) butonu")]
        [SerializeField] private Button closeXButton;

        // Ayar değerleri
        private float currentSoundVolume = 0.70f;
        private float currentMusicVolume = 0.50f;
        private int currentLanguageIndex = 0; // 0 = ENGLISH, 1 = TÜRKÇE
        private readonly string[] availableLanguages = new string[] { "ENGLISH", "TÜRKÇE" };
        private bool isFullscreen = true;

        // PlayerPrefs Anahtarları
        private const string PrefSound = "ZH_SoundVol";
        private const string PrefMusic = "ZH_MusicVol";
        private const string PrefLang = "ZH_Language";
        private const string PrefFullscreen = "ZH_Fullscreen";

        private void Awake()
        {
            LoadSavedSettings();
        }

        private void Start()
        {
            // Buton Listener'larını bağla
            if (soundSlider != null)
                soundSlider.onValueChanged.AddListener(SetSoundVolume);

            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(SetMusicVolume);

            if (prevLanguageButton != null)
                prevLanguageButton.onClick.AddListener(() => ChangeLanguage(-1));

            if (nextLanguageButton != null)
                nextLanguageButton.onClick.AddListener(() => ChangeLanguage(1));

            if (fullscreenOffButton != null)
                fullscreenOffButton.onClick.AddListener(() => SetFullscreen(false));

            if (fullscreenOnButton != null)
                fullscreenOnButton.onClick.AddListener(() => SetFullscreen(true));

            if (defaultsButton != null)
                defaultsButton.onClick.AddListener(ResetToDefaults);

            if (backButton != null)
                backButton.onClick.AddListener(CloseSettings);

            if (closeXButton != null)
                closeXButton.onClick.AddListener(CloseSettings);

            UpdateAllUIElements();

            // Başlangıçta paneli kapalı tut (isteğe bağlı)
            if (settingsPanelRoot != null && settingsPanelRoot.activeSelf)
            {
                // Menü sahnesi değilse varsayılan kapalı kalabilir
            }
        }

        private void Update()
        {
            // ESC tuşuna basıldığında Ayarlar panelini aç / kapat
            if (IsEscapePressed() && !IsEscapeUsedByIncidentFile())
            {
                ToggleSettings();
            }
        }

        // Kaza Dosyası açıkken ESC sadece dosyayı kapatır, ayarları açmaz
        private static bool IsEscapeUsedByIncidentFile()
        {
            var file = ZeroHour.Phase2.Calls.IncidentFileUI.Instance;
            if (file != null && file.IsOpen) return true;
            return ZeroHour.Phase2.Calls.IncidentFileUI.EscConsumedFrame == Time.frameCount;
        }

        private bool IsEscapePressed()
        {
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
            }
            catch { }

            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch { }

            return false;
        }

        public void OpenSettings()
        {
            if (settingsPanelRoot != null)
            {
                if (!settingsPanelRoot.activeSelf)
                {
                    // Ayarlar açıkken oyun durur
                    timeScaleBeforeOpen = Time.timeScale;
                    Time.timeScale = 0f;
                }
                settingsPanelRoot.SetActive(true);
                UpdateAllUIElements();
            }
        }

        public void CloseSettings()
        {
            SaveSettings();
            if (settingsPanelRoot != null)
            {
                if (settingsPanelRoot.activeSelf) Time.timeScale = timeScaleBeforeOpen;
                settingsPanelRoot.SetActive(false);
            }
        }

        private float timeScaleBeforeOpen = 1f;

        public void ToggleSettings()
        {
            if (settingsPanelRoot != null)
            {
                bool isOpen = settingsPanelRoot.activeSelf;
                if (isOpen) CloseSettings();
                else OpenSettings();
            }
        }

        // --- SES FONKSİYONLARI ---
        public void SetSoundVolume(float volume)
        {
            currentSoundVolume = Mathf.Clamp01(volume);
            if (soundPercentText != null)
            {
                soundPercentText.text = $"{Mathf.RoundToInt(currentSoundVolume * 100f)}%";
            }
            // Global ses seviyesini ayarla
            AudioListener.volume = currentSoundVolume;
        }

        public void SetMusicVolume(float volume)
        {
            currentMusicVolume = Mathf.Clamp01(volume);
            if (musicPercentText != null)
            {
                musicPercentText.text = $"{Mathf.RoundToInt(currentMusicVolume * 100f)}%";
            }
            // Varsa müzik AudioSource'ları buradan yönetilebilir
        }

        // --- DİL FONKSİYONLARI ---
        public void ChangeLanguage(int direction)
        {
            currentLanguageIndex += direction;
            if (currentLanguageIndex >= availableLanguages.Length) currentLanguageIndex = 0;
            if (currentLanguageIndex < 0) currentLanguageIndex = availableLanguages.Length - 1;

            UpdateLanguageUI();
        }

        private void UpdateLanguageUI()
        {
            if (languageText != null)
            {
                languageText.text = availableLanguages[currentLanguageIndex];
            }
        }

        // --- TAM EKRAN FONKSİYONLARI ---
        public void SetFullscreen(bool enable)
        {
            isFullscreen = enable;
            Screen.fullScreen = isFullscreen;

            UpdateFullscreenUI();
        }

        private void UpdateFullscreenUI()
        {
            if (fullscreenOffHighlight != null)
                fullscreenOffHighlight.gameObject.SetActive(!isFullscreen);

            if (fullscreenOnHighlight != null)
                fullscreenOnHighlight.gameObject.SetActive(isFullscreen);
        }

        // --- VARSAYILANLARA SIFIRLA (DEFAULTS) ---
        public void ResetToDefaults()
        {
            currentSoundVolume = 0.70f;
            currentMusicVolume = 0.50f;
            currentLanguageIndex = 0; // ENGLISH
            isFullscreen = true;

            Screen.fullScreen = true;
            AudioListener.volume = currentSoundVolume;

            UpdateAllUIElements();
            SaveSettings();
            Debug.Log("[Settings] Ayarlar varsayılana sıfırlandı: Sound %70, Music %50, English, Fullscreen ON.");
        }

        private void UpdateAllUIElements()
        {
            if (soundSlider != null) soundSlider.value = currentSoundVolume;
            if (soundPercentText != null) soundPercentText.text = $"{Mathf.RoundToInt(currentSoundVolume * 100f)}%";

            if (musicSlider != null) musicSlider.value = currentMusicVolume;
            if (musicPercentText != null) musicPercentText.text = $"{Mathf.RoundToInt(currentMusicVolume * 100f)}%";

            UpdateLanguageUI();
            UpdateFullscreenUI();
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(PrefSound, currentSoundVolume);
            PlayerPrefs.SetFloat(PrefMusic, currentMusicVolume);
            PlayerPrefs.SetInt(PrefLang, currentLanguageIndex);
            PlayerPrefs.SetInt(PrefFullscreen, isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSavedSettings()
        {
            currentSoundVolume = PlayerPrefs.GetFloat(PrefSound, 0.70f);
            currentMusicVolume = PlayerPrefs.GetFloat(PrefMusic, 0.50f);
            currentLanguageIndex = PlayerPrefs.GetInt(PrefLang, 0);
            isFullscreen = PlayerPrefs.GetInt(PrefFullscreen, 1) == 1;

            AudioListener.volume = currentSoundVolume;
        }
    }
}

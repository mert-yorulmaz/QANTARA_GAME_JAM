using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using ZeroHour.Shared;

namespace ZeroHour.Phase2.UI
{
    /// <summary>
    /// Ayarlar / duraklatma menüsü. Sahneden bağımsız tek kopya: Resources/GlobalSettings prefab'ı oyun açılınca
    /// bir kez yüklenir ve sahne değişse de kalır (Faz 1 ofis, Faz 2 gece vardiyası, ana menü). Her sahnede ESC ile açılır.
    /// Diğer scriptlerin ESC'si önce çalışsın diye (bilgisayardan kalkma, olay dosyasını kapatma) bu script en son çalışır.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SettingsUI : MonoBehaviour
    {
        public static SettingsUI Instance { get; private set; }

        private const string PrefabResourceName = "GlobalSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGlobalInstance()
        {
            if (Instance != null) return;
            GameObject prefab = Resources.Load<GameObject>(PrefabResourceName);
            if (prefab == null)
            {
                Debug.LogWarning("[Settings] Resources/GlobalSettings bulunamadı: ZeroHour kurulum menüsünü (8.) çalıştırın.");
                return;
            }
            GameObject instance = Instantiate(prefab);
            instance.name = PrefabResourceName;
        }

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

        [Header("--- 6. ANA MENÜYE DÖNÜŞ ---")]
        [Tooltip("Oyundan ana menüye döner (ilerleme kaybolur: ilk tık onay ister). Ana menünün kendisinde gizlenir.")]
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private TMP_Text mainMenuLabel;
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private float confirmSeconds = 3f;

        private float mainMenuConfirmUntil = -1f;

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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
            LoadSavedSettings();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            GamePause.IsMenuOpen = false;
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

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);

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
            // Onay süresi dolduysa düğme eski yazısına döner
            if (mainMenuConfirmUntil >= 0f && Time.unscaledTime > mainMenuConfirmUntil)
            {
                mainMenuConfirmUntil = -1f;
                UpdateMainMenuButton();
            }

            if (IsEscapePressed() && !GamePause.EscapeConsumedThisFrame && !IsEscapeUsedByIncidentFile())
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

                    // Faz 1 imleci kilitler (birinci şahıs); menüde fare görünsün, kapanınca eski hâline dönsün
                    cursorLockBeforeOpen = Cursor.lockState;
                    cursorVisibleBeforeOpen = Cursor.visible;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    EnsureEventSystem();
                    GamePause.IsMenuOpen = true;
                }
                settingsPanelRoot.SetActive(true);
                mainMenuConfirmUntil = -1f;
                UpdateAllUIElements();
            }
        }

        public void CloseSettings()
        {
            SaveSettings();
            if (settingsPanelRoot != null)
            {
                if (settingsPanelRoot.activeSelf)
                {
                    Time.timeScale = timeScaleBeforeOpen;
                    Cursor.lockState = cursorLockBeforeOpen;
                    Cursor.visible = cursorVisibleBeforeOpen;
                    GamePause.IsMenuOpen = false;
                }
                settingsPanelRoot.SetActive(false);
            }
        }

        private float timeScaleBeforeOpen = 1f;
        private CursorLockMode cursorLockBeforeOpen = CursorLockMode.None;
        private bool cursorVisibleBeforeOpen = true;

        public bool IsOpen => settingsPanelRoot != null && settingsPanelRoot.activeSelf;

        // Butonlar tıklanabilsin: sahnede EventSystem yoksa (ör. yeni bir menü sahnesi) bir tane oluştur
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(null);
        }

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
            // Müzik çalan kaynaklar (MusicVolume bileşeni) bu seviyeyi her karede uygular
            MusicVolume.Level = currentMusicVolume;
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

            // Oyundaki tüm metinleri seçili dile çevir (0 = ENGLISH, 1 = TÜRKÇE)
            ZeroHour.Phase2.Calls.Loc.SetLanguage(currentLanguageIndex);
            UpdateMainMenuButton();
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
            MusicVolume.Level = currentMusicVolume;

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
            UpdateMainMenuButton();
        }

        // --- ANA MENÜYE DÖNÜŞ ---
        private void OnMainMenuClicked()
        {
            // İlk tık onay ister (oyun ortasında ilerleme kaybolur), ikinci tık menüye döner
            if (mainMenuConfirmUntil < 0f)
            {
                mainMenuConfirmUntil = Time.unscaledTime + confirmSeconds;
                UpdateMainMenuButton();
                return;
            }

            mainMenuConfirmUntil = -1f;
            CloseSettings();
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        }

        private void UpdateMainMenuButton()
        {
            if (mainMenuButton == null) return;
            mainMenuButton.gameObject.SetActive(SceneManager.GetActiveScene().name != mainMenuScene);
            if (mainMenuLabel != null)
                mainMenuLabel.text = ZeroHour.Phase2.Calls.Loc.T(mainMenuConfirmUntil >= 0f ? "SURE?" : "MAIN MENU");
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
            MusicVolume.Level = currentMusicVolume;
        }
    }
}

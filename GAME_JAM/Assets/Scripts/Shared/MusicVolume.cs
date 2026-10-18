using UnityEngine;

namespace ZeroHour.Shared
{
    /// <summary>
    /// Müzik çalan AudioSource'a eklenir: ayarlardaki müzik seviyesine göre sesini ayarlar (ana menü, Faz 1 arka plan müziği).
    /// Genel ses (AudioListener.volume) bunun üstüne ayrıca uygulanır.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicVolume : MonoBehaviour
    {
        /// <summary>SettingsUI ile aynı PlayerPrefs anahtarı.</summary>
        public const string PrefKey = "ZH_MusicVol";
        public const float DefaultLevel = 0.5f;

        private static float level = -1f;

        /// <summary>Müzik seviyesi (0–1). Ayarlar paneli hiç açılmadıysa kayıtlı değer okunur.</summary>
        public static float Level
        {
            get
            {
                if (level < 0f) level = PlayerPrefs.GetFloat(PrefKey, DefaultLevel);
                return level;
            }
            set => level = Mathf.Clamp01(value);
        }

        [Tooltip("Müzik seviyesi %100 iken AudioSource sesi. -1 ise sahnedeki değeri alır.")]
        [SerializeField] private float fullVolume = -1f;

        private AudioSource source;

        /// <summary>Sahne geçişinde müziği yumuşakça kısmak için (0 = sessiz, 1 = ayarlardaki seviye).</summary>
        public float Fade { get; set; } = 1f;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (fullVolume < 0f) fullVolume = source.volume;
            Update();
        }

        private void Update()
        {
            source.volume = fullVolume * Level * Fade;
        }
    }
}

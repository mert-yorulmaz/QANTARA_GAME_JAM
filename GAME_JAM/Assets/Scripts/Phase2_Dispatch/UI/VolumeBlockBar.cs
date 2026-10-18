using UnityEngine;
using UnityEngine.UI;

namespace ZeroHour.Phase2.UI
{
    /// <summary>
    /// Yağız'ın ses çubuğu: 10 blok (volume_block_filled / volume_block_empty).
    /// Görünmez bir Slider'ın değerini %10'luk adımlara yuvarlar ve blokları boyar.
    /// SettingsUI Slider'ı dinlemeye devam eder.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class VolumeBlockBar : MonoBehaviour
    {
        [SerializeField] private Image[] blocks;
        [SerializeField] private Sprite filledSprite;
        [SerializeField] private Sprite emptySprite;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void Start()
        {
            Refresh(slider.value);
        }

        private void OnValueChanged(float value)
        {
            float snapped = Mathf.Round(value * blocks.Length) / blocks.Length;
            if (!Mathf.Approximately(snapped, value))
            {
                slider.value = snapped; // Tekrar tetiklenir; SettingsUI yüzdeyi yuvarlanmış değerle günceller
                return;
            }
            Refresh(value);
        }

        private void Refresh(float value)
        {
            int filled = Mathf.RoundToInt(value * blocks.Length);
            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i] != null) blocks[i].sprite = i < filled ? filledSprite : emptySprite;
            }
        }
    }
}

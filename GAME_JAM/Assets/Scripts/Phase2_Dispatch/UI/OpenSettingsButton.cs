using UnityEngine;
using UnityEngine.UI;

namespace ZeroHour.Phase2.UI
{
    /// <summary>
    /// Ayarlar paneli sahneden bağımsız (Resources/GlobalSettings) olduğu için butonlar ona sahne referansıyla bağlanamaz;
    /// bu bileşen tıklanınca o anki SettingsUI'ı açar (vardiya HUD'ındaki dişli, ileride ana menü).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OpenSettingsButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (SettingsUI.Instance != null) SettingsUI.Instance.OpenSettings();
            });
        }
    }
}

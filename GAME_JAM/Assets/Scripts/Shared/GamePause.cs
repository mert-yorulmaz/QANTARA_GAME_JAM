using UnityEngine;

namespace ZeroHour.Shared
{
    /// <summary>
    /// Fazlar arası ortak durum: ayarlar (duraklatma) menüsü açık mı, bu karede ESC başka bir iş için kullanıldı mı.
    /// Faz 1 ve Faz 2 scriptleri menü açıkken girdi okumaz; ESC'yi kendi işi için kullanan script
    /// (bilgisayardan kalkma, olay dosyasını kapatma) ConsumeEscape() çağırır, ayarlar o karede açılmaz.
    /// </summary>
    public static class GamePause
    {
        /// <summary>Ayarlar menüsü açık: oyun durdu, sahnedeki girdiler beklemeli.</summary>
        public static bool IsMenuOpen { get; internal set; }

        private static int escapeConsumedFrame = -1;

        public static void ConsumeEscape()
        {
            escapeConsumedFrame = Time.frameCount;
        }

        public static bool EscapeConsumedThisFrame => escapeConsumedFrame == Time.frameCount;
    }
}

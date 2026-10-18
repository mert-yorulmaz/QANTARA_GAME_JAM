using TMPro;
using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>Sabit UI yazısı: İngilizce kaynak metni seçili dile çevirir, dil değişince günceller.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string english;

        private TMP_Text text;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            if (string.IsNullOrEmpty(english)) english = text.text;
        }

        private void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Loc.Changed -= Apply;
        }

        private void Apply()
        {
            if (text != null) text.text = Loc.T(english);
        }
    }
}

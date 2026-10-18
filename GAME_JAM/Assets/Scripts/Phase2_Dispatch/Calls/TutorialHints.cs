using TMPro;
using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// İlk kez oynayan için adım adım ipucu bandı (üst orta). Oyuncu adımı yapınca bir sonrakine geçer:
    /// çağrıyı aç → rozete tıkla → ekip seç ve gönder → ekipler birlikte daha hızlı çalışır.
    /// </summary>
    public class TutorialHints : MonoBehaviour
    {
        private enum Step { Welcome, OpenMarker, Dispatch, Teamwork, Done }

        [SerializeField] private CanvasGroup banner;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private float teamworkSeconds = 6f;

        private Step step = Step.Welcome;
        private float stepTimer;

        private static readonly string WelcomeHint = "Answer incoming calls (bottom left). Each call puts a marker on the map.";
        private static readonly string OpenMarkerHint = "Click the marker on the map to open the incident file.";
        private static readonly string DispatchHint = "Choose units with [+] (see 'Possible units') and press DISPATCH.";
        private static readonly string TeamworkHint = "The yellow ring must close before the white one runs out. Helicopters arrive first!";

        private void Start()
        {
            SetStep(Step.Welcome);
        }

        private void OnEnable()
        {
            Loc.Changed += RefreshText;
        }

        private void OnDisable()
        {
            Loc.Changed -= RefreshText;
        }

        private void Update()
        {
            stepTimer += Time.unscaledDeltaTime;
            IncidentBoard board = IncidentBoard.Instance;

            // Vardiya bitince ipucu kalmasın (karne ekranı)
            if (ShiftClock.Instance != null && ShiftClock.Instance.IsOver && step != Step.Done) SetStep(Step.Done);

            switch (step)
            {
                case Step.Welcome:
                    // İlk rozet çıkınca (çağrı dinlenince) bir sonraki adım
                    if (board != null && board.ActiveIncidents.Count > 0 && stepTimer > 1f) SetStep(Step.OpenMarker);
                    break;

                case Step.OpenMarker:
                    if (IncidentFileUI.Instance != null && IncidentFileUI.Instance.IsOpen) SetStep(Step.Dispatch);
                    break;

                case Step.Dispatch:
                    if (AnyUnitsDispatched(board)) SetStep(Step.Teamwork);
                    break;

                case Step.Teamwork:
                    if (stepTimer > teamworkSeconds) SetStep(Step.Done);
                    break;
            }

            // Yumuşak aç / kapa
            if (banner != null)
            {
                float target = step == Step.Done ? 0f : 1f;
                banner.alpha = Mathf.MoveTowards(banner.alpha, target, Time.unscaledDeltaTime * 3f);
                if (step == Step.Done && banner.alpha <= 0f) gameObject.SetActive(false);
            }
        }

        private static bool AnyUnitsDispatched(IncidentBoard board)
        {
            if (board == null) return false;
            foreach (IncidentReport r in board.ActiveIncidents)
            {
                if (r.status == CallIncidentStatus.Responding || r.status == CallIncidentStatus.Working) return true;
            }
            return false;
        }

        private void SetStep(Step next)
        {
            step = next;
            stepTimer = 0f;
            RefreshText();
        }

        private void RefreshText()
        {
            if (hintText == null) return;
            switch (step)
            {
                case Step.Welcome: hintText.text = Loc.T(WelcomeHint); break;
                case Step.OpenMarker: hintText.text = Loc.T(OpenMarkerHint); break;
                case Step.Dispatch: hintText.text = Loc.T(DispatchHint); break;
                case Step.Teamwork: hintText.text = Loc.T(TeamworkHint); break;
            }
        }
    }
}

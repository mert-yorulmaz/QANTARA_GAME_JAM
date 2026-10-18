using UnityEngine;
using UnityEngine.UI;
using ZeroHour.Phase2.Core;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Gelen çağrı döngüsü: her 10–15 sn'de telefon çalar (call_card_ringing).
    /// ACCEPT -> 3 sn boğuk telsiz dinlenir (call_card_listening + call_progress_fill) -> haritada olay rozeti çıkar.
    /// DECLINE veya cevapsız kalma -> CityStatsManager.OnCallDismissed(5).
    /// </summary>
    public class IncomingCallController : MonoBehaviour
    {
        private enum CallState { Idle, Ringing, Listening }

        [Header("Çağrı Kartları")]
        [SerializeField] private CanvasGroup ringingCard;
        [SerializeField] private CanvasGroup listeningCard;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private Image progressFill;
        [SerializeField] private Graphic ringingIconBlink;

        [Header("Zamanlama")]
        [SerializeField] private float firstCallDelay = 4f;
        [SerializeField] private float minInterval = 20f;
        [SerializeField] private float maxInterval = 30f;
        [Tooltip("Haritada ekip bekleyen her olay için bir sonraki çağrıya eklenen süre (sn)")]
        [SerializeField] private float extraDelayPerWaitingIncident = 6f;
        [Tooltip("Bu kadar süre açılmayan çağrı cevapsız sayılır")]
        [SerializeField] private float ringTimeout = 12f;
        [SerializeField] private float listenDuration = 3f;

        [Header("Ceza")]
        [SerializeField] private int declinePenalty = 5;

        [Header("Ses")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Range(0f, 1f)] private float ringVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float mumbleVolume = 0.9f;

        private CallState state = CallState.Idle;
        private float timer;
        private AudioClip ringClip;
        private AudioClip mumbleClip;
        private int callCounter;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            ringClip = ProceduralCallAudio.CreateRingClip();

            if (acceptButton != null) acceptButton.onClick.AddListener(AcceptCall);
            if (declineButton != null) declineButton.onClick.AddListener(DeclineCall);
        }

        private void Start()
        {
            ShowCard(ringingCard, false);
            ShowCard(listeningCard, false);
            timer = firstCallDelay;
        }

        private void Update()
        {
            switch (state)
            {
                case CallState.Idle:
                    timer -= Time.deltaTime;
                    if (timer <= 0f)
                    {
                        // Kaza dosyası açıkken telefon çalmaz; kapasite / boş bina yoksa çağrı ertelenir
                        bool fileOpen = IncidentFileUI.Instance != null && IncidentFileUI.Instance.IsOpen;
                        bool canCreate = IncidentBoard.Instance != null && IncidentBoard.Instance.CanCreateIncident();
                        if (!fileOpen && canCreate) StartRinging();
                        else timer = 3f;
                    }
                    break;

                case CallState.Ringing:
                    timer += Time.deltaTime;

                    // Telefon zili gibi yanıp sönme
                    float blink = Mathf.Abs(Mathf.Sin(timer * 5f));
                    ringingCard.alpha = 0.75f + 0.25f * blink;
                    if (ringingIconBlink != null)
                    {
                        Color c = ringingIconBlink.color;
                        c.a = 0.45f * blink;
                        ringingIconBlink.color = c;
                    }

                    if (timer >= ringTimeout)
                    {
                        Debug.Log("<color=orange>[Calls] 📵 Çağrı cevapsız kaldı.</color>");
                        DeclineCall();
                    }
                    break;

                case CallState.Listening:
                    timer += Time.deltaTime;
                    if (progressFill != null) progressFill.fillAmount = Mathf.Clamp01(timer / listenDuration);

                    if (timer >= listenDuration) FinishListening();
                    break;
            }
        }

        private void StartRinging()
        {
            state = CallState.Ringing;
            timer = 0f;
            callCounter++;

            ShowCard(listeningCard, false);
            ShowCard(ringingCard, true);

            audioSource.clip = ringClip;
            audioSource.loop = true;
            audioSource.volume = ringVolume;
            audioSource.Play();
        }

        public void AcceptCall()
        {
            if (state != CallState.Ringing) return;

            state = CallState.Listening;
            timer = 0f;

            ShowCard(ringingCard, false);
            ShowCard(listeningCard, true);
            if (progressFill != null) progressFill.fillAmount = 0f;

            audioSource.Stop();
            audioSource.loop = false;
            if (mumbleClip != null) Destroy(mumbleClip);
            mumbleClip = ProceduralCallAudio.CreateMumbleClip(listenDuration, callCounter * 7919);
            audioSource.clip = mumbleClip;
            audioSource.volume = mumbleVolume;
            audioSource.Play();
        }

        public void DeclineCall()
        {
            if (state != CallState.Ringing) return;

            if (CityStatsManager.Instance != null) CityStatsManager.Instance.OnCallDismissed(declinePenalty);
            EndCall();
        }

        private void FinishListening()
        {
            if (IncidentBoard.Instance != null) IncidentBoard.Instance.CreateRandomIncident();
            EndCall();
        }

        private void EndCall()
        {
            state = CallState.Idle;
            int waiting = IncidentBoard.Instance != null ? IncidentBoard.Instance.WaitingCount : 0;
            timer = Random.Range(minInterval, maxInterval) + waiting * extraDelayPerWaitingIncident;

            audioSource.Stop();
            ShowCard(ringingCard, false);
            ShowCard(listeningCard, false);
        }

        private static void ShowCard(CanvasGroup card, bool visible)
        {
            if (card == null) return;
            card.alpha = visible ? 1f : 0f;
            card.interactable = visible;
            card.blocksRaycasts = visible;
            card.gameObject.SetActive(visible);
        }
    }
}

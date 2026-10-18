using System;
using UnityEngine;

// Mesai saati: masaya ilk oturuşta 08:00'de başlar, 17:00'de zil çalar.
// 08:00-17:00 aralığı gerçek zamanda realSecondsPerShift saniye sürer.
public class ShiftClock : MonoBehaviour
{
    public int startHour = 8;
    public int endHour = 17;
    public float realSecondsPerShift = 120f;
    [Tooltip("Saat yalnızca bu obje açıkken (oyuncu bilgisayar başındayken) ilerler; masadan kalkınca zaman durur.")]
    public GameObject runOnlyWhileActive;
    [Tooltip("Açıksa mesai 17:00'de kendiliğinden biter. Kapalıysa mesai son dosya incelenince biter (EndShift).")]
    public bool endsAtEndHour = false;
    [Tooltip("Mesai saatle bitmiyorsa saat bu kadar dakika kala durur (dosyalar bitmeden 17:00 görünmesin).")]
    public float holdMinutesBeforeEnd = 15f;
    [Tooltip("dd MMM yyyy, örn. 10 Mar 2026")]
    public string date = "10 Mar 2026";

    [Header("Bell")]
    [Tooltip("Boş bırakılırsa zil sesi koddan üretilir.")]
    public AudioClip bellClip;
    public AudioSource audioSource;
    [Range(0f, 1f)] public float bellVolume = 0.5f;

    public event Action ShiftStarted;
    public event Action ShiftEnded;

    public bool HasStarted { get; private set; }
    public bool IsOver { get; private set; }

    float elapsed;

    public float ShiftMinutes => (endHour - startHour) * 60f;

    // Günün dakikası (08:00 -> 480).
    public float GameMinutes =>
        startHour * 60f + Mathf.Clamp01(elapsed / Mathf.Max(0.01f, realSecondsPerShift)) * ShiftMinutes;

    public string TimeText
    {
        get
        {
            int minutes = Mathf.FloorToInt(GameMinutes);
            return $"{minutes / 60 % 24:00}:{minutes % 60:00}";
        }
    }

    public string DateText =>
        InspectionRules.TryParseDate(date, out DateTime d) ? d.ToString("dd.MM.yyyy") : date;

    // 0 sabah, 1 öğle, 2 öğleden sonra, 3 akşam.
    public int Period
    {
        get
        {
            float hour = GameMinutes / 60f;
            if (IsOver || hour >= endHour) return 3;
            if (hour < 12f) return 0;
            if (hour < 14f) return 1;
            return 2;
        }
    }

    public string PeriodName
    {
        get
        {
            switch (Period)
            {
                case 0: return "Morning";
                case 1: return "Noon";
                case 2: return "Afternoon";
                default: return "Evening";
            }
        }
    }

    public string HoursText => $"{startHour:00}:00 – {endHour:00}:00";

    public void StartShift()
    {
        if (HasStarted)
            return;

        HasStarted = true;
        elapsed = 0f;
        ShiftStarted?.Invoke();
    }

    void Update()
    {
        if (!HasStarted || IsOver)
            return;

        // Masadan kalkınca zaman geçmez
        if (runOnlyWhileActive != null && !runOnlyWhileActive.activeInHierarchy)
            return;

        elapsed += Time.deltaTime;

        if (!endsAtEndHour)
        {
            // Mesai dosyalar bitince biter; saat 17:00'ye varmadan bekler
            float hold = realSecondsPerShift * Mathf.Clamp01(1f - holdMinutesBeforeEnd / Mathf.Max(1f, ShiftMinutes));
            elapsed = Mathf.Min(elapsed, hold);
            return;
        }

        if (elapsed >= realSecondsPerShift)
            EndShift();
    }

    // Mesaiyi bitirir: saat 17:00 olur, zil çalar (son dosya incelenince InspectionDesk çağırır)
    public void EndShift()
    {
        if (!HasStarted || IsOver)
            return;

        elapsed = realSecondsPerShift;
        IsOver = true;
        RingBell();
        ShiftEnded?.Invoke();
    }

    public void RingBell()
    {
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        if (bellClip == null)
            bellClip = SynthesizeBell();

        audioSource.PlayOneShot(bellClip, bellVolume);
    }

    // Okul zili: metal kısmi frekanslar + çekiç titreşimi, sonunda sönüm.
    static AudioClip SynthesizeBell()
    {
        const int rate = 44100;
        const float ring = 2.6f, decay = 0.9f;
        int samples = Mathf.CeilToInt((ring + decay) * rate);
        var data = new float[samples];

        float[] partials = { 1f, 2.02f, 2.76f, 5.4f };
        float[] amplitudes = { 1f, 0.5f, 0.35f, 0.18f };
        const float baseFrequency = 930f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)rate;
            float tone = 0f;
            for (int p = 0; p < partials.Length; p++)
                tone += amplitudes[p] * Mathf.Sin(2f * Mathf.PI * baseFrequency * partials[p] * t);

            float hammer = t < ring ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 18f * t)) : 1f;
            float envelope = t < 0.01f ? t / 0.01f : t < ring ? 1f : Mathf.Exp(-(t - ring) * 5f);
            data[i] = tone * hammer * envelope * 0.25f;
        }

        var clip = AudioClip.Create("ShiftBell", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

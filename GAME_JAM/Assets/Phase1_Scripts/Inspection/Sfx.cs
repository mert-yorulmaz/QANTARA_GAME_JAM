using System;
using System.Collections.Generic;
using UnityEngine;

public enum SfxId
{
    Click, Notify, FolderOpen, FolderClose, Minimize,
    Stamp, Correct, Wrong, Paper, XRay, Tick, PageFlip, TaskDone,
    Boot, ChairSit, StandUp, Footstep, WakeBreath, DoorCreak, NightDrone
}

// Oyunun ses efektleri. Ses dosyası atanmamış efektler koddan sentezlenir.
// Sahnedeki mevcut ses kaynaklarına (müzik, saat) dokunmaz; kendi 2D kaynaklarını kullanır.
public class Sfx : MonoBehaviour
{
    [Serializable]
    public class Override
    {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 2f)] public float volume = 1f;
    }

    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Tooltip("Bir efekti gerçek bir ses dosyasıyla değiştirmek için.")]
    public List<Override> overrides = new List<Override>();

    static Sfx instance;

    AudioSource oneShots;
    AudioSource steps;
    readonly Dictionary<SfxId, AudioClip> cache = new Dictionary<SfxId, AudioClip>();

    // Efektlerin birbirine göre varsayılan yüksekliği.
    static float BaseVolume(SfxId id)
    {
        switch (id)
        {
            case SfxId.Click: return 0.35f;
            case SfxId.Tick: return 0.45f;
            case SfxId.Footstep: return 0.35f;
            case SfxId.WakeBreath: return 0.6f;
            case SfxId.NightDrone: return 0.7f;
            case SfxId.Stamp: return 0.9f;
            default: return 0.6f;
        }
    }

    void Awake()
    {
        instance = this;
        oneShots = CreateSource("SfxOneShots");
        steps = CreateSource("SfxFootsteps");
    }

    AudioSource CreateSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    public static void Play(SfxId id, float volume = 1f)
    {
        if (instance != null)
            instance.PlayOn(instance.oneShots, id, volume);
    }

    // Adımlar her seferinde biraz farklı perdede çalar.
    public static void Footstep()
    {
        if (instance == null)
            return;

        instance.steps.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
        instance.PlayOn(instance.steps, SfxId.Footstep, UnityEngine.Random.Range(0.8f, 1f));
    }

    void PlayOn(AudioSource source, SfxId id, float volume)
    {
        float overrideVolume = 1f;
        AudioClip clip = null;
        foreach (var o in overrides)
        {
            if (o.id == id && o.clip != null)
            {
                clip = o.clip;
                overrideVolume = o.volume;
                break;
            }
        }

        if (clip == null && !cache.TryGetValue(id, out clip))
        {
            clip = SfxSynth.Create(id);
            cache[id] = clip;
        }

        source.PlayOneShot(clip, masterVolume * BaseVolume(id) * overrideVolume * volume);
    }
}

// Basit ses sentezi: sinüs/kare tonlar, süzülmüş gürültü ve gıcırtı darbeleri.
static class SfxSynth
{
    const int Rate = 44100;
    static readonly System.Random rng = new System.Random(7);

    public static AudioClip Create(SfxId id)
    {
        float[] d;
        switch (id)
        {
            case SfxId.Click:
                d = Buffer(0.05f);
                Tone(d, 2400f, 1700f, 0f, 0.03f, 0.5f, 120f);
                Noise(d, 0f, 0.02f, 0.4f, 200f, 0.2f);
                break;
            case SfxId.Notify:
                d = Buffer(0.6f);
                Tone(d, 880f, 880f, 0f, 0.3f, 0.45f, 9f);
                Tone(d, 1320f, 1320f, 0.1f, 0.45f, 0.45f, 7f);
                break;
            case SfxId.FolderOpen:
                d = Buffer(0.3f);
                Noise(d, 0f, 0.24f, 0.6f, 7f, 0.85f, 0.06f);
                Tone(d, 480f, 900f, 0f, 0.16f, 0.12f, 14f);
                break;
            case SfxId.FolderClose:
                d = Buffer(0.22f);
                Noise(d, 0f, 0.18f, 0.5f, 10f, 0.85f, 0.02f);
                Tone(d, 820f, 430f, 0f, 0.13f, 0.12f, 16f);
                break;
            case SfxId.Minimize:
                d = Buffer(0.16f);
                Tone(d, 900f, 380f, 0f, 0.13f, 0.35f, 14f);
                break;
            case SfxId.Stamp:
                d = Buffer(0.35f);
                Tone(d, 120f, 50f, 0f, 0.3f, 1f, 13f);
                Tone(d, 240f, 110f, 0f, 0.08f, 0.35f, 40f);
                Noise(d, 0f, 0.06f, 0.8f, 55f, 0.5f);
                break;
            case SfxId.Correct:
                // Neşeli arpej + bozuk para parıltısı.
                d = Buffer(0.9f);
                Tone(d, 1046.5f, 1046.5f, 0f, 0.5f, 0.32f, 6f);
                Tone(d, 1318.5f, 1318.5f, 0.07f, 0.5f, 0.32f, 6f);
                Tone(d, 1568f, 1568f, 0.14f, 0.6f, 0.36f, 5f);
                Tone(d, 2093f, 2093f, 0.21f, 0.55f, 0.26f, 6f);
                for (int i = 0; i < 6; i++)
                    Tone(d, 3800f + i * 310f, 3800f + i * 310f, 0.24f + i * 0.035f, 0.18f, 0.06f, 22f);
                break;
            case SfxId.Wrong:
                // Alçak, iki vuruşlu uyarı vızıltısı.
                d = Buffer(0.6f);
                Tone(d, 150f, 140f, 0f, 0.2f, 0.32f, 4f, Wave.Square);
                Tone(d, 147f, 120f, 0.25f, 0.32f, 0.32f, 4f, Wave.Square);
                break;
            case SfxId.Paper:
                d = Buffer(0.4f);
                for (int i = 0; i < 5; i++)
                    Noise(d, i * 0.06f + (float)rng.NextDouble() * 0.02f, 0.07f, 0.35f, 30f, 0.55f, 0.005f);
                break;
            case SfxId.XRay:
                // Yükselen tarama + elektrik uğultusu.
                d = Buffer(0.6f);
                Tone(d, 300f, 1400f, 0f, 0.45f, 0.22f, 1.5f, Wave.Triangle);
                Tone(d, 60f, 60f, 0f, 0.55f, 0.18f, 2f, Wave.Square);
                break;
            case SfxId.Tick:
                d = Buffer(0.04f);
                Tone(d, 3200f, 2800f, 0f, 0.025f, 0.4f, 150f);
                Noise(d, 0f, 0.01f, 0.3f, 300f, 0f);
                break;
            case SfxId.PageFlip:
                d = Buffer(0.28f);
                Noise(d, 0f, 0.2f, 0.5f, 11f, 0.75f, 0.03f);
                Noise(d, 0.06f, 0.1f, 0.3f, 30f, 0.5f);
                break;
            case SfxId.TaskDone:
                d = Buffer(0.8f);
                Tone(d, 1568f, 1568f, 0f, 0.6f, 0.3f, 5f);
                Tone(d, 2093f, 2093f, 0.07f, 0.65f, 0.26f, 5f);
                break;
            case SfxId.Boot:
                // Bilgisayar açılış akoru.
                d = Buffer(1.9f);
                foreach (float f in new[] { 261.6f, 329.6f, 392f, 523.3f })
                    Tone(d, f, f, 0f, 1.8f, 0.14f, 1.4f, Wave.Sine, 0.25f);
                Tone(d, 1046.5f, 1046.5f, 0.35f, 1.2f, 0.06f, 2.5f, Wave.Sine, 0.1f);
                break;
            case SfxId.ChairSit:
                d = Buffer(0.6f);
                Tone(d, 95f, 55f, 0f, 0.25f, 0.6f, 12f);
                Noise(d, 0f, 0.12f, 0.25f, 20f, 0.8f);
                Creak(d, 0.06f, 0.4f, 0.25f, 55f);
                break;
            case SfxId.StandUp:
                d = Buffer(0.5f);
                Noise(d, 0f, 0.42f, 0.35f, 4f, 0.9f, 0.1f);
                Creak(d, 0.05f, 0.25f, 0.15f, 70f);
                break;
            case SfxId.Footstep:
                d = Buffer(0.14f);
                Tone(d, 100f, 60f, 0f, 0.1f, 0.6f, 35f);
                Noise(d, 0f, 0.05f, 0.3f, 60f, 0.6f);
                break;
            case SfxId.WakeBreath:
                // Derin bir nefes alıp verme.
                d = Buffer(2.2f);
                Breath(d, 0f, 1.0f, 0.5f);
                Breath(d, 1.1f, 1.05f, 0.35f);
                break;
            case SfxId.DoorCreak:
                d = Buffer(1.2f);
                Creak(d, 0f, 0.9f, 0.4f, 38f);
                Tone(d, 75f, 55f, 0.9f, 0.25f, 0.5f, 12f);
                break;
            case SfxId.NightDrone:
                // Gece vardiyasına geçerken karanlık, gergin bir uğultu.
                d = Buffer(4f);
                Tone(d, 55f, 55f, 0f, 3.9f, 0.3f, 0.35f, Wave.Sine, 1.2f);
                Tone(d, 82.4f, 82.4f, 0f, 3.9f, 0.2f, 0.35f, Wave.Sine, 1.2f);
                Tone(d, 110f, 108.8f, 0.4f, 3.5f, 0.12f, 0.4f, Wave.Triangle, 1.5f);
                Breath(d, 0.5f, 3.2f, 0.12f);
                break;
            default:
                d = Buffer(0.05f);
                break;
        }

        Normalize(d, 0.9f);
        var clip = AudioClip.Create("Sfx_" + id, d.Length, 1, Rate, false);
        clip.SetData(d, 0);
        return clip;
    }

    enum Wave { Sine, Square, Triangle }

    static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

    static void Tone(float[] d, float f0, float f1, float start, float duration, float amp, float decay,
        Wave wave = Wave.Sine, float attack = 0.004f)
    {
        int s0 = (int)(start * Rate), n = (int)(duration * Rate);
        double phase = 0;
        for (int i = 0; i < n && s0 + i < d.Length; i++)
        {
            float t = i / (float)Rate;
            float f = Mathf.Lerp(f0, f1, i / (float)n);
            phase += 2.0 * Math.PI * f / Rate;
            float s = (float)Math.Sin(phase);
            float v = wave == Wave.Square ? Mathf.Sign(s) * 0.6f
                : wave == Wave.Triangle ? (float)(2.0 / Math.PI * Math.Asin(s))
                : s;
            float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-t * decay) * Mathf.Min(1f, (n - i) / (0.005f * Rate));
            d[s0 + i] += v * amp * env;
        }
    }

    // Gürültü; smooth 0 = parlak, 1'e yakın = boğuk.
    static void Noise(float[] d, float start, float duration, float amp, float decay, float smooth, float attack = 0.002f)
    {
        int s0 = (int)(start * Rate), n = (int)(duration * Rate);
        float y = 0f;
        for (int i = 0; i < n && s0 + i < d.Length; i++)
        {
            float t = i / (float)Rate;
            y += ((float)(rng.NextDouble() * 2.0 - 1.0) - y) * (1f - smooth);
            float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-t * decay) * Mathf.Min(1f, (n - i) / (0.005f * Rate));
            d[s0 + i] += y * amp * env * (1f + smooth * 2f);
        }
    }

    // Yumuşak giriş-çıkışlı boğuk gürültü (nefes, rüzgâr).
    static void Breath(float[] d, float start, float duration, float amp)
    {
        int s0 = (int)(start * Rate), n = (int)(duration * Rate);
        float y = 0f, y2 = 0f;
        for (int i = 0; i < n && s0 + i < d.Length; i++)
        {
            y += ((float)(rng.NextDouble() * 2.0 - 1.0) - y) * 0.08f;
            y2 += (y - y2) * 0.3f;
            float env = Mathf.Pow(Mathf.Sin(Mathf.PI * i / n), 2f);
            d[s0 + i] += y2 * amp * env * 6f;
        }
    }

    // Gıcırtı: düzensiz aralıklı kısa darbeler (ahşap, menteşe).
    static void Creak(float[] d, float start, float duration, float amp, float rate)
    {
        int s0 = (int)(start * Rate), end = s0 + (int)(duration * Rate);
        int i = s0;
        while (i < end && i < d.Length)
        {
            float k = (i - s0) / (float)(end - s0);
            float env = Mathf.Sin(Mathf.PI * k);
            int pulse = (int)(0.004f * Rate);
            float f = 380f + 140f * (float)rng.NextDouble();
            for (int p = 0; p < pulse && i + p < d.Length; p++)
                d[i + p] += Mathf.Sin(2f * Mathf.PI * f * p / Rate) * Mathf.Exp(-p / (0.0012f * Rate)) * amp * env;

            float r = rate * (0.7f + 0.6f * (float)rng.NextDouble()) * (1f + 0.6f * Mathf.Sin(k * 9f));
            i += Mathf.Max(1, (int)(Rate / r));
        }
    }

    static void Normalize(float[] d, float peak)
    {
        float max = 0f;
        foreach (float v in d)
            max = Mathf.Max(max, Mathf.Abs(v));
        if (max <= 0f)
            return;

        float scale = peak / max;
        for (int i = 0; i < d.Length; i++)
            d[i] *= scale;
    }
}

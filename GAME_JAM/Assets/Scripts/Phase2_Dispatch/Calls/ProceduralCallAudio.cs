using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Projede ses dosyası olmadığı için telefon zili ve boğuk telsiz/mumble sesini kodla üretir.
    /// </summary>
    public static class ProceduralCallAudio
    {
        private const int SampleRate = 22050;

        /// <summary>Çift çalan klasik telefon zili (3 sn, döngüye uygun).</summary>
        public static AudioClip CreateRingClip()
        {
            int length = SampleRate * 3;
            float[] data = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                bool on = (t < 0.4f) || (t >= 0.6f && t < 1.0f);
                if (!on) continue;

                float local = t < 0.4f ? t : t - 0.6f;
                float env = Mathf.Clamp01(local / 0.01f) * Mathf.Clamp01((0.4f - local) / 0.02f);

                // 400 + 450 Hz karışımı ve 20 Hz titreşim (eski telefon zili)
                float tremolo = 0.6f + 0.4f * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 20f * t));
                float tone = Mathf.Sin(2f * Mathf.PI * 400f * t) + Mathf.Sin(2f * Mathf.PI * 450f * t);
                data[i] = tone * 0.18f * env * tremolo;
            }

            AudioClip clip = AudioClip.Create("Procedural_PhoneRing", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Telsiz hışırtısı arkasında anlaşılmayan konuşma (mumble).</summary>
        public static AudioClip CreateMumbleClip(float duration, int seed)
        {
            int length = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[length];
            System.Random rng = new System.Random(seed);

            float phase = 0f;
            float lowpass = 0f;
            float noiseLowpass = 0f;
            float syllableTimer = 0f;
            float syllableLength = 0.18f;
            float pitch = 130f;
            float targetPitch = 130f;

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;

                // Hece zarfı: konuşma ritmi
                syllableTimer += 1f / SampleRate;
                if (syllableTimer >= syllableLength)
                {
                    syllableTimer = 0f;
                    syllableLength = 0.09f + (float)rng.NextDouble() * 0.22f;
                    targetPitch = 105f + (float)rng.NextDouble() * 70f;
                }
                pitch = Mathf.Lerp(pitch, targetPitch, 0.0008f);
                float syllableEnv = Mathf.Sin(Mathf.PI * Mathf.Clamp01(syllableTimer / syllableLength));
                syllableEnv *= syllableEnv;

                // Ses telleri: harmonikli kaynak
                phase += 2f * Mathf.PI * pitch / SampleRate;
                float voice = Mathf.Sin(phase) + 0.5f * Mathf.Sin(2f * phase) + 0.3f * Mathf.Sin(3f * phase) + 0.15f * Mathf.Sin(5f * phase);

                // Boğuk telsiz: alçak geçiren filtre
                lowpass += (voice - lowpass) * 0.08f;

                // Telsiz hışırtısı
                float white = (float)rng.NextDouble() * 2f - 1f;
                noiseLowpass += (white - noiseLowpass) * 0.35f;

                float fade = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((duration - t) / 0.08f);
                data[i] = (lowpass * syllableEnv * 0.32f + noiseLowpass * 0.05f) * fade;
            }

            AudioClip clip = AudioClip.Create("Procedural_RadioMumble", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ==========================================
        // AFET SESLERİ (bir kez üretilip önbellekte tutulur)
        // ==========================================

        private static AudioClip rumble, explosion, crackle, rain, alarm;

        /// <summary>Deprem gürlemesi: derin, hızla sönen uğultu (1.6 sn).</summary>
        public static AudioClip Rumble => rumble != null ? rumble : (rumble = CreateNoiseClip("Procedural_QuakeRumble", 1.6f, 0.02f, 0.9f, 11, (t, d) => Mathf.Clamp01(t / 0.08f) * Mathf.Pow(1f - t / d, 1.8f) * (0.75f + 0.25f * Mathf.Sin(t * 37f))));

        /// <summary>Patlama: sert başlangıç, kısa kuyruk (1.1 sn).</summary>
        public static AudioClip Explosion => explosion != null ? explosion : (explosion = CreateNoiseClip("Procedural_Explosion", 1.1f, 0.06f, 1f, 23, (t, d) => Mathf.Clamp01(t / 0.005f) * Mathf.Exp(-t * 4.5f)));

        /// <summary>Yangın çıtırtısı (döngü, 3 sn).</summary>
        public static AudioClip Crackle => crackle != null ? crackle : (crackle = CreateCrackleClip());

        /// <summary>Yağmur (döngü, 3 sn): sürekli hışırtı.</summary>
        public static AudioClip Rain => rain != null ? rain : (rain = CreateNoiseClip("Procedural_Rain", 3f, 0.55f, 0.35f, 37, (t, d) => 1f));

        /// <summary>Dükkan alarmı (döngü, 1 sn): iki tonlu bip.</summary>
        public static AudioClip Alarm => alarm != null ? alarm : (alarm = CreateAlarmClip());

        /// <param name="brightness">0 = çok boğuk (alçak geçiren), 1 = ham gürültü</param>
        private static AudioClip CreateNoiseClip(string name, float duration, float brightness, float volume, int seed, System.Func<float, float, float> envelope)
        {
            int length = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[length];
            System.Random rng = new System.Random(seed);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                lp += (white - lp) * brightness;
                lp2 += (lp - lp2) * brightness;
                data[i] = Mathf.Clamp(lp2 * envelope(t, duration) * volume * (brightness < 0.1f ? 6f : 1f), -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateCrackleClip()
        {
            const float Duration = 3f;
            int length = Mathf.CeilToInt(SampleRate * Duration);
            float[] data = new float[length];
            System.Random rng = new System.Random(5);
            float roar = 0f;
            float pop = 0f;
            for (int i = 0; i < length; i++)
            {
                float white = (float)rng.NextDouble() * 2f - 1f;
                roar += (white - roar) * 0.04f;                    // alevin boğuk uğultusu
                if (rng.NextDouble() < 0.0009) pop = 1f;          // rastgele çıtırtı
                pop *= 0.985f;
                data[i] = roar * 0.9f + white * pop * 0.5f;
            }
            AudioClip clip = AudioClip.Create("Procedural_FireCrackle", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateAlarmClip()
        {
            const float Duration = 1f;
            int length = Mathf.CeilToInt(SampleRate * Duration);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                float freq = t < 0.5f ? 950f : 720f;
                float local = Mathf.Repeat(t, 0.5f);
                float env = Mathf.Clamp01(local / 0.01f) * Mathf.Clamp01((0.42f - local) / 0.02f);
                data[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.12f * env;
            }
            AudioClip clip = AudioClip.Create("Procedural_ShopAlarm", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

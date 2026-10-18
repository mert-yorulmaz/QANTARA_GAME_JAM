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
                lowpass += (voice - lowpass) * 0.12f;

                // Telsiz hışırtısı
                float white = (float)rng.NextDouble() * 2f - 1f;
                noiseLowpass += (white - noiseLowpass) * 0.35f;

                float fade = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((duration - t) / 0.08f);
                data[i] = (lowpass * syllableEnv * 0.32f + noiseLowpass * 0.05f) * fade;
            }

            // Telefonda az duyuluyordu: tepe seviyesi 0.9'a çekilir
            float peak = 0.0001f;
            for (int i = 0; i < length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float gain = 0.9f / peak;
            for (int i = 0; i < length; i++) data[i] *= gain;

            AudioClip clip = AudioClip.Create("Procedural_RadioMumble", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ==========================================
        // AFET SESLERİ (bir kez üretilip önbellekte tutulur)
        // ==========================================

        private static AudioClip rumble, explosion, crackle, rain, alarm, earthquake, notify;

        /// <summary>Telefona mesaj düştü: kısa iki tonlu bip (0.2 sn).</summary>
        public static AudioClip Notify => notify != null ? notify : (notify = CreateNotifyClip());

        private static AudioClip CreateNotifyClip()
        {
            const float Duration = 0.2f;
            int length = Mathf.CeilToInt(SampleRate * Duration);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                float local = t < 0.09f ? t : t - 0.1f;
                if (t >= 0.09f && t < 0.1f) continue;
                float freq = t < 0.09f ? 880f : 1320f;
                float env = Mathf.Clamp01(local / 0.005f) * Mathf.Exp(-local * 28f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f * env;
            }
            AudioClip clip = AudioClip.Create("Procedural_PhoneNotify", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Deprem: duyulabilir gürleme + taş sürtünmesi + düşen moloz tıkırtıları (2.8 sn). Laptop hoparlöründe de duyulur.</summary>
        public static AudioClip Earthquake => earthquake != null ? earthquake : (earthquake = CreateEarthquakeClip());

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

        private static AudioClip CreateEarthquakeClip()
        {
            const float Duration = 2.8f;
            int length = Mathf.CeilToInt(SampleRate * Duration);
            float[] data = new float[length];
            System.Random rng = new System.Random(71);

            float deep = 0f, deep2 = 0f;      // derin gürleme (büyük hoparlörler için)
            float midHi = 0f, midLo = 0f;     // 100–500 Hz bandı: küçük hoparlörde duyulan "homurtu"
            float wobble = 0f, wobbleTarget = 1f;
            float clack = 0f, clackLp = 0f;   // moloz tıkırtısı
            float peak = 0f;

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                float white = (float)rng.NextDouble() * 2f - 1f;

                deep += (white - deep) * 0.012f;
                deep2 += (deep - deep2) * 0.012f;

                midHi += (white - midHi) * 0.11f;
                midLo += (white - midLo) * 0.018f;
                float grind = midHi - midLo;

                // Düzensiz sarsıntı dalgaları
                if (i % 900 == 0) wobbleTarget = 0.45f + (float)rng.NextDouble() * 0.75f;
                wobble += (wobbleTarget - wobble) * 0.0015f;

                // Ortada yoğunlaşan, rastgele düşen moloz
                float clackDensity = 0.0012f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / Duration));
                if (rng.NextDouble() < clackDensity) clack = 0.6f + (float)rng.NextDouble() * 0.4f;
                clack *= 0.9965f;
                clackLp += (white * clack - clackLp) * 0.3f;

                // Zarf: hızlı yükseliş, 0.7 sn'de tepe, uzun sönüş
                float env = Mathf.Clamp01(t / 0.25f) * Mathf.Pow(Mathf.Clamp01(1f - (t - 0.7f) / (Duration - 0.7f)), t > 0.7f ? 1.4f : 0f);

                float s = (deep2 * 9f + grind * 2.2f) * wobble * env + clackLp * 0.55f;
                data[i] = s;
                peak = Mathf.Max(peak, Mathf.Abs(s));
            }

            // Normalleştir: her sistemde aynı yükseklikte
            float gain = peak > 0.0001f ? 0.92f / peak : 1f;
            for (int i = 0; i < length; i++) data[i] *= gain;

            AudioClip clip = AudioClip.Create("Procedural_Earthquake", length, 1, SampleRate, false);
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
            // ==========================================
        // ARAÇ SESLERİ (döngülü; bir kez üretilir)
        // ==========================================

        private static AudioClip ambulanceSiren, policeSiren, fireSiren, helicopterRotor;

        /// <summary>Ambulans: yavaş inip çıkan uzun siren (4 sn).</summary>
        public static AudioClip AmbulanceSiren => ambulanceSiren != null ? ambulanceSiren : (ambulanceSiren = CreateSirenClip("Procedural_AmbulanceSiren", 4f, 0.2f,
            t => 650f + 700f * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * t / 4f))));

        /// <summary>Polis: hızlı yükselip düşen siren.</summary>
        public static AudioClip PoliceSiren => policeSiren != null ? policeSiren : (policeSiren = CreateSirenClip("Procedural_PoliceSiren", 3.2f, 0.18f,
            t => 750f + 750f * Mathf.PingPong(t / 0.16f, 1f)));

        /// <summary>İtfaiye: iki tonlu "dat-dit" (2.4 sn).</summary>
        public static AudioClip FireSiren => fireSiren != null ? fireSiren : (fireSiren = CreateSirenClip("Procedural_FireSiren", 2.4f, 0.2f,
            t => Mathf.Repeat(t, 1.2f) < 0.6f ? 580f : 435f));

        /// <summary>Helikopter: pervane vuruşu + türbin uğultusu (2 sn).</summary>
        public static AudioClip HelicopterRotor => helicopterRotor != null ? helicopterRotor : (helicopterRotor = CreateRotorClip());

        private static AudioClip CreateSirenClip(string name, float duration, float volume, System.Func<float, float> frequencyAt)
        {
            int length = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[length];
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                phase += 2f * Mathf.PI * frequencyAt(t) / SampleRate;
                float tone = Mathf.Sin(phase) + 0.35f * Mathf.Sin(2f * phase) + 0.2f * Mathf.Sin(3f * phase);
                // Döngü dikişinde tık olmasın
                float edge = Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((duration - t) / 0.01f);
                data[i] = tone * volume * edge;
            }
            AudioClip clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateRotorClip()
        {
            const float Duration = 2f;
            const float BladeRate = 17f; // 2 sn'de tam 34 vuruş: döngü dikişsiz
            int length = Mathf.CeilToInt(SampleRate * Duration);
            float[] data = new float[length];
            System.Random rng = new System.Random(71);
            float noise = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                noise += (white - noise) * 0.06f;
                float thump = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * BladeRate * t), 6f);
                float whine = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.04f;
                data[i] = Mathf.Clamp(noise * 3.2f * (0.25f + thump) + whine, -1f, 1f) * 0.5f;
            }
            AudioClip clip = AudioClip.Create("Procedural_HelicopterRotor", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

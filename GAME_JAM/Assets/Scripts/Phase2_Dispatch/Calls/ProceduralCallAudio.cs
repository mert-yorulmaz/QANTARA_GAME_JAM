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
    }
}

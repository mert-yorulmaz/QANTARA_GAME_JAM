using UnityEngine;
using ZeroHour.Phase2.Calls;
using ZeroHour.Phase2.Data;

namespace ZeroHour.Phase2.Units
{
    /// <summary>
    /// Araç sesi: görevdeki kara aracı hareket ederken siren çalar (olay yerinde park edince susar),
    /// helikopterin pervanesi görev boyunca döner. AStarMovementTester her araca ekler.
    /// Oyun durunca (ayarlar menüsü, vardiya sonu) ses de durur.
    /// </summary>
    public class UnitAudio : MonoBehaviour
    {
        private const float SirenVolume = 0.22f;
        private const float RotorVolume = 0.3f;
        private const float MovingSpeed = 0.6f;

        private AStarMovementTester fleet;
        private UnitType type;
        private AudioSource source;
        private Vector3 lastPosition;
        private float movingUntil;
        private bool paused;

        public void Initialize(AStarMovementTester owner, UnitType unitType)
        {
            fleet = owner;
            type = unitType;
            lastPosition = transform.position;

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.dopplerLevel = 0f;
            // Yukarıdan bakan kamerada uzaktaki araç da biraz duyulsun, yakındaki daha yüksek
            source.spatialBlend = 0.6f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 25f;
            source.maxDistance = 300f;
        }

        private void Update()
        {
            if (source == null) return;

            bool frozen = Time.timeScale == 0f || (Calls.ShiftClock.Instance != null && Calls.ShiftClock.Instance.IsOver);
            if (frozen)
            {
                if (source.isPlaying)
                {
                    source.Pause();
                    paused = true;
                }
                return;
            }

            // Hareket: son karedeki yer değiştirmeden (kısa durmalarda siren titremesin diye 0.4 sn tutulur)
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            if ((transform.position - lastPosition).magnitude / dt > MovingSpeed) movingUntil = Time.time + 0.4f;
            lastPosition = transform.position;

            if (!ShouldPlay())
            {
                if (source.isPlaying || paused) source.Stop();
                paused = false;
                return;
            }

            if (paused)
            {
                source.UnPause();
                paused = false;
                return;
            }

            if (!source.isPlaying)
            {
                source.clip = ClipFor(type);
                source.volume = type == UnitType.RescueHelicopter ? RotorVolume : SirenVolume;
                // Aynı anda çıkan araçların sirenleri üst üste binmesin
                source.time = Random.Range(0f, source.clip.length * 0.9f);
                source.Play();
            }
        }

        private bool ShouldPlay()
        {
            if (fleet == null || !gameObject.activeInHierarchy || !fleet.IsVehicleOnMission(gameObject)) return false;
            if (type == UnitType.RescueHelicopter) return true;
            return Time.time < movingUntil;
        }

        private static AudioClip ClipFor(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Ambulance: return ProceduralCallAudio.AmbulanceSiren;
                case UnitType.PoliceCruiser: return ProceduralCallAudio.PoliceSiren;
                case UnitType.FireEngine: return ProceduralCallAudio.FireSiren;
                default: return ProceduralCallAudio.HelicopterRotor;
            }
        }
    }
}

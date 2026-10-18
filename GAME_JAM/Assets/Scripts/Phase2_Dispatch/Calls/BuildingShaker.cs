using System.Collections.Generic;
using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Deprem bölgesindeki binaları sallar ve bitince tam olarak eski konum / dönüşlerine koyar.
    /// Genlik, binaların NavMeshObstacle hareket eşiğinin (0.1 m) altında tutulur; araç yolları yeniden oyulmaz.
    /// </summary>
    public class BuildingShaker : MonoBehaviour
    {
        private const float MaxAmplitude = 0.09f;   // metre
        private const float MaxTiltDegrees = 0.8f;

        private class ShakeJob
        {
            public Transform target;
            public Vector3 basePosition;
            public Quaternion baseRotation;
            public float elapsed;
            public float duration;
            public float amplitude;
            public float seed;
        }

        private static BuildingShaker instance;
        private readonly Dictionary<Transform, ShakeJob> jobs = new Dictionary<Transform, ShakeJob>();
        private readonly List<Transform> finished = new List<Transform>();
        private static readonly List<Transform> buildingRoots = new List<Transform>();

        private static BuildingShaker Get()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[ZeroHour_BuildingShaker]");
                instance = go.AddComponent<BuildingShaker>();
            }
            return instance;
        }

        /// <summary>Merkezden belli yarıçaptaki tüm binaları sallar (deprem bölgesi). Merkeze yakın olan daha çok sallanır.</summary>
        public static void ShakeArea(Vector3 center, float radius, float duration, float amplitude)
        {
            CacheBuildings();
            foreach (Transform b in buildingRoots)
            {
                if (b == null) continue;
                Vector3 d = b.position - center;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > radius) continue;

                float falloff = 1f - (dist / radius) * 0.6f;
                Shake(b, duration * Random.Range(0.85f, 1.15f), amplitude * falloff);
            }
        }

        public static void Shake(Transform building, float duration, float amplitude)
        {
            if (building == null) return;
            BuildingShaker shaker = Get();

            if (shaker.jobs.TryGetValue(building, out ShakeJob job))
            {
                // Süren sarsıntıyı uzat; temel konum ilk kaydedilen konum kalır
                job.duration = Mathf.Max(job.duration - job.elapsed, duration) + job.elapsed;
                job.amplitude = Mathf.Max(job.amplitude, amplitude);
                return;
            }

            shaker.jobs[building] = new ShakeJob
            {
                target = building,
                basePosition = building.position,
                baseRotation = building.rotation,
                duration = duration,
                amplitude = Mathf.Min(amplitude, MaxAmplitude),
                seed = Random.value * 100f
            };
        }

        private static void CacheBuildings()
        {
            buildingRoots.RemoveAll(t => t == null);
            if (buildingRoots.Count > 0) return;

            foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.activeInHierarchy && go.name.StartsWith("Building")) buildingRoots.Add(go.transform);
            }
        }

        private void LateUpdate()
        {
            finished.Clear();
            foreach (ShakeJob job in jobs.Values)
            {
                if (job.target == null) { finished.Add(job.target); continue; }

                job.elapsed += Time.deltaTime;
                if (job.elapsed >= job.duration)
                {
                    job.target.SetPositionAndRotation(job.basePosition, job.baseRotation);
                    finished.Add(job.target);
                    continue;
                }

                float fade = 1f - job.elapsed / job.duration;
                float t = Time.time * 18f + job.seed;
                float amp = Mathf.Min(job.amplitude, MaxAmplitude) * fade;
                Vector3 offset = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, 0f, Mathf.PerlinNoise(0.7f, t) - 0.5f) * (2f * amp);
                float tilt = (Mathf.PerlinNoise(t * 0.8f, 2.1f) - 0.5f) * 2f * MaxTiltDegrees * fade * (amp / MaxAmplitude);

                job.target.SetPositionAndRotation(job.basePosition + offset, job.baseRotation * Quaternion.Euler(tilt, 0f, -tilt * 0.7f));
            }

            foreach (Transform t in finished) jobs.Remove(t);
        }

        private void OnDisable()
        {
            // Sahne kapanırken / oyun dururken hiçbir bina kaymış kalmasın
            foreach (ShakeJob job in jobs.Values)
            {
                if (job.target != null) job.target.SetPositionAndRotation(job.basePosition, job.baseRotation);
            }
            jobs.Clear();
        }
    }
}

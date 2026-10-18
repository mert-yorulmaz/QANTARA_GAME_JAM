using UnityEngine;
using ZeroHour.Phase2.Calls;

namespace ZeroHour.Phase2.Units
{
    /// <summary>
    /// Görevdeki aracın tepe lambası: kara araçlarında kırmızı / mavi dönüşümlü çakar,
    /// helikopterde kırmızı uçuş ışığı. Araç üssüne dönüp park edince söner.
    /// </summary>
    public class EmergencyLightBar : MonoBehaviour
    {
        private const float FlashRate = 3f;   // saniyede çakma
        private const float LightIntensity = 7f;
        private const float LightRange = 14f;

        private static readonly Color Red = new Color(1f, 0.12f, 0.1f);
        private static readonly Color Blue = new Color(0.15f, 0.4f, 1f);

        private AStarMovementTester fleet;
        private bool isHelicopter;
        private Light lightA;
        private Light lightB;
        private ParticleSystemRenderer haloA;
        private ParticleSystemRenderer haloB;

        public void Initialize(AStarMovementTester owner, bool helicopter)
        {
            fleet = owner;
            isHelicopter = helicopter;

            // Aracın tepesi (çocuk mesh'lerin sınırlarından)
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(transform.position, Vector3.one * 2f);
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer) continue;
                b.Encapsulate(renderers[i].bounds);
            }
            Vector3 top = transform.InverseTransformPoint(new Vector3(b.center.x, b.max.y + 0.35f, b.center.z));
            Vector3 side = Vector3.right * Mathf.Min(0.6f, b.extents.x * 0.4f);

            lightA = CreateLamp("LightBar_A", top - side, Red, out haloA);
            lightB = CreateLamp("LightBar_B", top + side, isHelicopter ? Red : Blue, out haloB);
            SetVisible(false, false);
        }

        private Light CreateLamp(string name, Vector3 localPosition, Color color, out ParticleSystemRenderer halo)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;

            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = LightRange;
            l.intensity = LightIntensity;
            l.shadows = LightShadows.None;

            halo = null;
            DisasterFxLibrary lib = DisasterFxLibrary.Instance;
            if (lib == null || lib.additive == null) return l;

            // Uzaktan görünen parlak hale (tek kalıcı parçacık)
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 100000f;
            main.startSpeed = 0f;
            main.startSize = 3.2f;
            main.startColor = new Color(color.r, color.g, color.b, 0.9f);
            main.maxParticles = 1;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var shape = ps.shape;
            shape.enabled = false;
            halo = go.GetComponent<ParticleSystemRenderer>();
            halo.sharedMaterial = lib.additive;
            halo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return l;
        }

        private void Update()
        {
            bool busy = fleet != null && fleet.IsVehicleOnMission(gameObject);
            if (!busy)
            {
                SetVisible(false, false);
                return;
            }

            // Kara aracı: kırmızı / mavi dönüşümlü; helikopter: kısa kırmızı çakma
            float phase = Mathf.Repeat(Time.time * FlashRate, 1f);
            if (isHelicopter) SetVisible(phase < 0.15f, phase < 0.15f);
            else SetVisible(phase < 0.5f, phase >= 0.5f);
        }

        private void SetVisible(bool a, bool b)
        {
            if (lightA != null) lightA.enabled = a;
            if (lightB != null) lightB.enabled = b;
            if (haloA != null) haloA.enabled = a;
            if (haloB != null) haloB.enabled = b;
        }
    }
}

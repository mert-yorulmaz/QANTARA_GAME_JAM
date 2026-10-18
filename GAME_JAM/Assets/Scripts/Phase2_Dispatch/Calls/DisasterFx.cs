using System.Collections.Generic;
using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Bir olayın görsel efekti (her afete özel):
    ///  • Yangın: alev, duman sütunu, kıvılcım, titreyen turuncu ışık
    ///  • Deprem çökmesi: bölgedeki binaların sallanması, kamera sarsıntısı, toz bulutu, moloz, artçı sarsıntılar
    ///  • Gaz patlaması: patlama flaşı, ateş topu, şok dalgası, güçlü sarsıntı, ardından yangın
    ///  • Sel: binanın çevresinde yükselen su, sıçrama, yağmur
    ///  • Yağma / asayiş: yanan barikat, yanıp sönen kırmızı alarm ışığı
    /// Olay kapanınca Stop() ile söner ve kendini yok eder. Binaların materyallerine dokunmaz.
    /// </summary>
    public class DisasterFx : MonoBehaviour
    {
        private const float FadeOutSeconds = 6f;

        private IncidentReport report;
        private DisasterFxLibrary lib;
        private Bounds bounds;
        private float scale = 1f;

        private readonly List<ParticleSystem> loops = new List<ParticleSystem>();
        private readonly List<Light> flickerLights = new List<Light>();
        private readonly List<float> lightBaseIntensity = new List<float>();
        private Light alarmLight;
        private ParticleSystemRenderer alarmHalo;
        private Light flashLight;
        private float flashTimer;
        private Transform water;
        private float waterLevel;
        private float waterTarget;
        private ParticleSystem aftershockDust;
        private float nextAftershock;
        private bool stopping;
        private float stopTimer;

        public static DisasterFx Spawn(IncidentReport report)
        {
            DisasterFxLibrary library = DisasterFxLibrary.Instance;
            if (library == null || report == null) return null;

            GameObject go = new GameObject($"DisasterFx_{report.caseNumber}_{report.kind}");
            go.transform.position = report.sitePosition;
            DisasterFx fx = go.AddComponent<DisasterFx>();
            fx.Build(report, library);
            return fx;
        }

        public void Stop()
        {
            if (stopping) return;
            stopping = true;
            foreach (ParticleSystem ps in loops)
            {
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            waterTarget = -0.6f;
            Destroy(gameObject, FadeOutSeconds);
        }

        // ==========================================
        // KURULUM
        // ==========================================

        private void Build(IncidentReport r, DisasterFxLibrary library)
        {
            report = r;
            lib = library;
            bounds = r.buildingBounds;
            float footprint = Mathf.Min(bounds.size.x, bounds.size.z);
            scale = Mathf.Clamp(footprint / 10f, 0.7f, 2.2f);

            switch (r.kind)
            {
                case CallIncidentKind.Fire: BuildFire(1f); break;
                case CallIncidentKind.Collapse: BuildCollapse(); break;
                case CallIncidentKind.GasExplosion: BuildGasExplosion(); break;
                case CallIncidentKind.Flood: BuildFlood(); break;
                default: BuildUnrest(); break;
            }
        }

        private Vector3 RoofLocal(float yOffset = 0f) => new Vector3(bounds.center.x, bounds.max.y + yOffset, bounds.center.z) - transform.position;
        private Vector3 MidLocal() => bounds.center - transform.position;

        // ---------- YANGIN ----------
        private void BuildFire(float intensity)
        {
            Vector3 roof = RoofLocal(-0.3f);
            Vector3 box = new Vector3(bounds.size.x * 0.7f, 0.6f, bounds.size.z * 0.7f);

            // Alevler binanın üst yarısını sarar (rozetin arkasında kalmasın, cepheden de görünsün)
            Vector3 upperHalf = new Vector3(bounds.center.x, bounds.max.y - bounds.size.y * 0.25f, bounds.center.z) - transform.position;
            ParticleSystem flames = MakeSystem("Flames", upperHalf, lib.additive);
            var main = flames.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * scale, 5.5f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(3f * scale, 6.5f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.45f, 0.1f));
            main.gravityModifier = -0.2f;
            main.maxParticles = 400;
            var emission = flames.emission;
            emission.rateOverTime = 70f * scale * intensity;
            var shape = flames.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(bounds.size.x * 0.95f, bounds.size.y * 0.5f, bounds.size.z * 0.95f);
            SetColorFade(flames, new Color(1f, 0.9f, 0.5f), new Color(1f, 0.35f, 0.05f), 0.95f);
            SetSizeCurve(flames, 1f, 0.2f);
            Play(flames, true);

            ParticleSystem smoke = MakeSystem("Smoke", roof + Vector3.up * 2f, lib.alphaBlended);
            main = smoke.main;
            // Uzaktan (oyun kamerasından) görünen yüksek, koyu duman sütunu
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(6f * scale, 12f * scale);
            // Gece haritası koyu: duman ateşle aydınlanmış açık gri olsun ki zemine karışmasın
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.4f, 0.39f, 0.85f), new Color(0.6f, 0.57f, 0.55f, 0.7f));
            main.gravityModifier = -0.05f;
            main.maxParticles = 300;
            emission = smoke.emission;
            emission.rateOverTime = 14f * scale * intensity;
            shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box * 0.6f;
            var vel = smoke.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            SetColorFade(smoke, Color.white, Color.white, 0.8f);
            SetSizeCurve(smoke, 0.6f, 2.2f);
            Play(smoke, true);

            ParticleSystem embers = MakeSystem("Embers", roof, lib.additive);
            main = embers.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.2f), new Color(1f, 0.4f, 0.1f));
            main.gravityModifier = 0.25f;
            emission = embers.emission;
            emission.rateOverTime = 14f * scale * intensity;
            shape = embers.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;
            SetColorFade(embers, Color.white, Color.white, 1f);
            Play(embers, true);

            AddFlickerLight("FireGlow", roof + Vector3.up * 1.5f, new Color(1f, 0.5f, 0.15f), 10f * intensity, 35f * scale);
        }

        // ---------- DEPREM ÇÖKMESİ ----------
        private void BuildCollapse()
        {
            BuildingShaker.ShakeArea(report.building.position, 35f, 2.6f, 0.09f);
            CameraShake.Shake(0.7f, 1.2f);

            ParticleSystem dust = MakeSystem("Dust", MidLocal(), lib.alphaBlended);
            var main = dust.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(6f * scale, 14f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.65f, 0.52f, 0.75f), new Color(0.55f, 0.5f, 0.42f, 0.65f));
            main.gravityModifier = 0.02f;
            main.maxParticles = 250;
            var emission = dust.emission;
            emission.rateOverTime = 5f * scale;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(50 * scale)) });
            var shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            SetColorFade(dust, Color.white, Color.white, 0.85f);
            SetSizeCurve(dust, 0.7f, 1.8f);
            Play(dust, true);
            aftershockDust = dust;

            ParticleSystem rubble = MakeSystem("Debris", RoofLocal(-1f), lib.debris);
            main = rubble.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.33f, 0.3f), new Color(0.6f, 0.57f, 0.52f));
            main.gravityModifier = 2.5f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            emission = rubble.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(40 * scale)) });
            shape = rubble.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(bounds.size.x, 1f, bounds.size.z);
            Play(rubble, false);

            nextAftershock = Time.time + Random.Range(7f, 12f);
        }

        // ---------- GAZ PATLAMASI ----------
        private void BuildGasExplosion()
        {
            CameraShake.Shake(1.4f, 1.4f);
            BuildingShaker.ShakeArea(report.building.position, 22f, 1.6f, 0.09f);

            Vector3 mid = MidLocal();
            ParticleSystem fireball = MakeSystem("Fireball", mid, lib.additive);
            var main = fireball.main;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f * scale, 13f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(5f * scale, 11f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.7f), new Color(1f, 0.55f, 0.15f));
            var emission = fireball.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(35 * scale)) });
            var shape = fireball.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.6f;
            SetColorFade(fireball, Color.white, new Color(1f, 0.25f, 0.05f), 1f);
            SetSizeCurve(fireball, 0.6f, 1.6f);
            Play(fireball, false);

            ParticleSystem shock = MakeSystem("Shockwave", new Vector3(mid.x, 0.4f, mid.z), lib.additive);
            main = shock.main;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = 0.6f;
            main.startSpeed = 0f;
            main.startSize = 45f * scale;
            main.startColor = new Color(1f, 0.85f, 0.6f, 0.8f);
            emission = shock.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            shape = shock.shape;
            shape.enabled = false;
            shock.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            SetColorFade(shock, Color.white, Color.white, 1f);
            SetSizeCurve(shock, 0.05f, 1f);
            Play(shock, false);

            flashLight = AddLight("ExplosionFlash", mid, new Color(1f, 0.75f, 0.4f), 30f, 60f * scale);
            flashTimer = 0.8f;

            // Patlamanın ardından bina yanmaya devam eder (daha yoğun duman)
            BuildFire(1.25f);
        }

        // ---------- SEL ----------
        private void BuildFlood()
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "FloodWater";
            Destroy(disc.GetComponent<Collider>()); // Araçların yolunu / tıklamayı etkilemesin
            disc.transform.SetParent(transform, false);
            float diameter = Mathf.Max(bounds.size.x, bounds.size.z) * 1.9f + 6f;
            disc.transform.localScale = new Vector3(diameter, 0.5f, diameter);
            disc.transform.localPosition = new Vector3(bounds.center.x - transform.position.x, -0.6f, bounds.center.z - transform.position.z);
            MeshRenderer mr = disc.GetComponent<MeshRenderer>();
            mr.sharedMaterial = lib.water;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            water = disc.transform;
            waterLevel = -0.6f;
            waterTarget = 0.35f;

            ParticleSystem splash = MakeSystem("Splash", new Vector3(disc.transform.localPosition.x, 0.6f, disc.transform.localPosition.z), lib.alphaBlended);
            var main = splash.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.88f, 1f, 0.8f), new Color(0.55f, 0.75f, 1f, 0.6f));
            main.gravityModifier = 1.2f;
            var emission = splash.emission;
            emission.rateOverTime = 25f * scale;
            var shape = splash.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = diameter * 0.45f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            SetColorFade(splash, Color.white, Color.white, 1f);
            Play(splash, true);

            ParticleSystem rain = MakeSystem("Rain", RoofLocal(18f), lib.additive);
            main = rain.main;
            main.startLifetime = 1.4f;
            main.startSpeed = 0f;
            main.startSize = 0.12f;
            main.startColor = new Color(0.65f, 0.8f, 1f, 0.45f);
            main.gravityModifier = 3f;
            main.maxParticles = 600;
            emission = rain.emission;
            emission.rateOverTime = 220f;
            shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(diameter * 1.4f, 0.5f, diameter * 1.4f);
            ParticleSystemRenderer rainRenderer = rain.GetComponent<ParticleSystemRenderer>();
            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rainRenderer.velocityScale = 0.08f;
            rainRenderer.lengthScale = 2f;
            Play(rain, true);
        }

        // ---------- YAĞMA / ASAYİŞ ----------
        private void BuildUnrest()
        {
            // Binanın önündeki sokakta yanan barikat (en yakın asfalt noktası; yoksa binanın yanı)
            Vector3 street = new Vector3(bounds.center.x + bounds.extents.x + 2.5f, 0.3f, bounds.center.z);
            if (UnityEngine.AI.NavMesh.SamplePosition(bounds.center, out UnityEngine.AI.NavMeshHit roadHit, 40f, UnityEngine.AI.NavMesh.AllAreas))
            {
                street = roadHit.position + Vector3.up * 0.3f;
            }
            Vector3 barricade = street - transform.position;
            ParticleSystem flames = MakeSystem("BarricadeFire", barricade, lib.additive);
            var main = flames.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.45f, 0.1f));
            main.gravityModifier = -0.2f;
            var emission = flames.emission;
            emission.rateOverTime = 22f;
            var shape = flames.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3f, 0.4f, 1.2f);
            SetColorFade(flames, new Color(1f, 0.9f, 0.5f), new Color(1f, 0.35f, 0.05f), 0.95f);
            SetSizeCurve(flames, 1f, 0.2f);
            Play(flames, true);

            ParticleSystem smoke = MakeSystem("BarricadeSmoke", barricade + Vector3.up, lib.alphaBlended);
            main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startColor = new Color(0.15f, 0.15f, 0.15f, 0.65f);
            emission = smoke.emission;
            emission.rateOverTime = 6f;
            SetColorFade(smoke, Color.white, Color.white, 0.8f);
            SetSizeCurve(smoke, 0.6f, 2f);
            Play(smoke, true);

            AddFlickerLight("BarricadeGlow", barricade + Vector3.up, new Color(1f, 0.5f, 0.15f), 5f, 16f);
            alarmLight = AddLight("AlarmLight", RoofLocal(1.5f), new Color(1f, 0.12f, 0.1f), 0f, 35f * scale);

            // Çatıda yanıp sönen kırmızı alarm halesi (uzaktan görünür)
            ParticleSystem halo = MakeSystem("AlarmHalo", RoofLocal(2f), lib.additive);
            main = halo.main;
            main.startLifetime = 100000f;
            main.startSpeed = 0f;
            main.startSize = 9f * scale;
            main.startColor = new Color(1f, 0.15f, 0.12f, 0.9f);
            main.maxParticles = 1;
            emission = halo.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            shape = halo.shape;
            shape.enabled = false;
            Play(halo, true);
            alarmHalo = halo.GetComponent<ParticleSystemRenderer>();
        }

        // ==========================================
        // GÜNCELLEME
        // ==========================================

        private void Update()
        {
            float dt = Time.deltaTime;

            // Titreyen ateş ışığı
            for (int i = 0; i < flickerLights.Count; i++)
            {
                if (flickerLights[i] == null) continue;
                float target = lightBaseIntensity[i] * (0.75f + 0.5f * Mathf.PerlinNoise(Time.time * 7f, i * 3.1f));
                flickerLights[i].intensity = stopping ? Mathf.MoveTowards(flickerLights[i].intensity, 0f, dt * 3f) : target;
            }

            // Patlama flaşı
            if (flashLight != null && flashTimer > 0f)
            {
                flashTimer -= dt;
                flashLight.intensity = Mathf.Max(0f, flashTimer / 0.8f) * 30f;
                if (flashTimer <= 0f) Destroy(flashLight.gameObject);
            }

            // Alarm ışığı: saniyede 2 kez yanıp söner
            if (alarmLight != null)
            {
                bool on = !stopping && Mathf.Repeat(Time.time * 2f, 1f) < 0.5f;
                alarmLight.intensity = on ? 25f : 0f;
                if (alarmHalo != null) alarmHalo.enabled = on;
            }

            // Sel suyu yükselir / çekilir
            if (water != null)
            {
                waterLevel = Mathf.MoveTowards(waterLevel, waterTarget, dt * 0.35f);
                Vector3 p = water.localPosition;
                p.y = waterLevel;
                water.localPosition = p;
            }

            // Artçı sarsıntılar (deprem)
            if (report != null && report.kind == CallIncidentKind.Collapse && !stopping && Time.time >= nextAftershock)
            {
                nextAftershock = Time.time + Random.Range(8f, 14f);
                BuildingShaker.Shake(report.building, 1.1f, 0.06f);
                CameraShake.Shake(0.18f, 0.6f);
                if (aftershockDust != null) aftershockDust.Emit(Mathf.RoundToInt(12 * scale));
            }
        }

        // ==========================================
        // YARDIMCILAR
        // ==========================================

        private ParticleSystem MakeSystem(string name, Vector3 localPosition, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = true;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        private void Play(ParticleSystem ps, bool looping)
        {
            var main = ps.main;
            main.loop = looping;
            ps.Play(true);
            if (looping) loops.Add(ps);
        }

        private static void SetColorFade(ParticleSystem ps, Color start, Color end, float startAlpha)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(startAlpha, 0.12f), new GradientAlphaKey(startAlpha * 0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        private static void SetSizeCurve(ParticleSystem ps, float startMultiplier, float endMultiplier)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, startMultiplier, 1f, endMultiplier));
        }

        private Light AddLight(string name, Vector3 localPosition, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        private void AddFlickerLight(string name, Vector3 localPosition, Color color, float intensity, float range)
        {
            flickerLights.Add(AddLight(name, localPosition, color, intensity, range));
            lightBaseIntensity.Add(intensity);
        }
    }
}

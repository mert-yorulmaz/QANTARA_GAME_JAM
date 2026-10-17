using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZeroHour.Phase2.Editor
{
    public static class TacticalNightAtmosphere
    {
        [MenuItem("ZeroHour/🌙 1. Set True 45° Isometric Camera")]
        public static void SetIsometricCamera()
        {
            Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                Undo.RecordObject(cam.transform, "Setup Isometric Camera");
                // True 45-degree corner isometric diamond view looking North-East
                cam.transform.position = new Vector3(-412f, 135f, -177f);
                cam.transform.rotation = Quaternion.Euler(35f, 45f, 0f);

                Undo.RecordObject(cam, "Setup Camera Background");
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.03f, 0.06f, 0.12f, 1f); // Deep C4I Command Center Navy
                Debug.Log("<color=cyan>[ZeroHour] Kamera tam 45 derece izometrik açıya getirildi! (Rot: 35, 45, 0 | Pos: -412, 135, -177)</color>");
            }
        }

        [MenuItem("ZeroHour/🌑 2. Fix Double Light & Set Moody Night (Karanlık Gece Modu)")]
        public static void ApplyMoodyNightLighting()
        {
            // NOT: Kullanıcının özel binalara (Hastane vb.) verdiği emission ayarlarına KESİNLİKLE DOKUNULMAZ!

            // 1. Çift Işık Sorununu Çöz (Sahnede 2 tane güneş vardı!)
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool primaryFound = false;

            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    Undo.RecordObject(l, "Adjust Directional Light");
                    Undo.RecordObject(l.gameObject, "Toggle Duplicate Light");

                    if (!primaryFound)
                    {
                        // Ana ışığı loş, tatlı bir gece ay ışığı yap
                        primaryFound = true;
                        l.gameObject.SetActive(true);
                        l.color = new Color(0.35f, 0.55f, 0.85f, 1f); // Gece Ay Işığı
                        l.intensity = 0.25f; // Çok aydınlık olmaması için loş gece seviyesi
                        l.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
                    }
                    else
                    {
                        // İkinci kopya ışığı kapat (ortamı fazla parlatan buydu!)
                        l.gameObject.SetActive(false);
                    }
                }
            }

            // 2. Ortam Işığını (Ambient) Loş Gece Seviyesine İndir
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.10f, 0.18f, 1f);

            // 3. Post-Processing ile Şehri Gece Moduna Karart (Böylece kullanıcının parlayan hastanesi parıldasın!)
            Volume volume = Object.FindAnyObjectByType<Volume>();
            if (volume != null && volume.profile != null)
            {
                var profile = volume.profile;

                // Color Adjustments
                if (!profile.TryGet(out ColorAdjustments colorAdjust))
                {
                    colorAdjust = profile.Add<ColorAdjustments>(true);
                }
                colorAdjust.active = true;
                colorAdjust.colorFilter.overrideState = true;
                colorAdjust.colorFilter.value = new Color(0.70f, 0.85f, 1.0f, 1f);
                colorAdjust.contrast.overrideState = true;
                colorAdjust.contrast.value = 20f;
                colorAdjust.postExposure.overrideState = true;
                colorAdjust.postExposure.value = -0.6f; // Şehri karartır, parlayan binaları öne çıkarır!
                colorAdjust.saturation.overrideState = true;
                colorAdjust.saturation.value = -15f;

                // Bloom (Kullanıcının yaptığı parlayan binaları enfes şekilde parlatır)
                if (!profile.TryGet(out Bloom bloom))
                {
                    bloom = profile.Add<Bloom>(true);
                }
                bloom.active = true;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 1.6f;
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 0.8f;
                bloom.tint.overrideState = true;
                bloom.tint.value = new Color(0.4f, 0.95f, 1f, 1f);

                // Vignette: Yumuşak kenar
                if (!profile.TryGet(out Vignette vignette))
                {
                    vignette = profile.Add<Vignette>(true);
                }
                vignette.active = true;
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0.22f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.35f;

                EditorUtility.SetDirty(volume);
                EditorUtility.SetDirty(profile);
            }

            Debug.Log("<color=cyan>[ZeroHour] 🌑 Çift ışık kapatıldı, loş gece ayarlandı!</color>");
        }

        [MenuItem("ZeroHour/✨ Boost All Emission Intensity (+1)")]
        public static void BoostEmissionIntensity()
        {
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SimplePoly City - Low Poly Assets/Materials" });
            int count = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.HasProperty("_EmissionColor"))
                {
                    Color current = mat.GetColor("_EmissionColor");
                    // +1 HDR intensity equals 2x in linear space
                    Color boosted = new Color(current.r * 2f, current.g * 2f, current.b * 2f, current.a);
                    mat.SetColor("_EmissionColor", boosted);
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[ZeroHour] {count} materyalin emission intensity değeri +1 artırıldı!</color>");
        }

        [MenuItem("ZeroHour/🌙 Dim All Emission Intensity (-1)")]
        public static void DimEmissionIntensity()
        {
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SimplePoly City - Low Poly Assets/Materials" });
            int count = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.HasProperty("_EmissionColor"))
                {
                    Color current = mat.GetColor("_EmissionColor");
                    Color dimmed = new Color(current.r * 0.5f, current.g * 0.5f, current.b * 0.5f, current.a);
                    mat.SetColor("_EmissionColor", dimmed);
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[ZeroHour] {count} materyalin emission intensity değeri -1 düşürüldü!</color>");
        }
    }
}

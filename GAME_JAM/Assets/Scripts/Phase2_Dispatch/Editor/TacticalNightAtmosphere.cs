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
                cam.backgroundColor = new Color(0.04f, 0.08f, 0.14f, 1f); // Deep C4I Command Center Navy
                Debug.Log("<color=cyan>[ZeroHour] Kamera tam 45 derece izometrik açıya getirildi! (Rot: 35, 45, 0 | Pos: -412, 135, -177)</color>");
            }
        }

        [MenuItem("ZeroHour/✨ 2. Apply Subtle Turquoise (Cam Göbeği) Emission to All Buildings")]
        public static void ApplyTurquoiseEmission()
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/SimplePoly City - Low Poly Assets/Materials" });
            int count = 0;

            Color turquoiseGlow = new Color(0.05f, 0.40f, 0.50f, 1.0f); // Subtle Cam Göbeği
            Color roadGlow = new Color(0.02f, 0.12f, 0.18f, 1.0f);

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    mat.shader = urpLitShader;
                    mat.EnableKeyword("_EMISSION");
                    
                    bool isRoad = mat.name.Contains("Road");
                    Color targetColor = isRoad ? roadGlow : turquoiseGlow;
                    
                    mat.SetColor("_EmissionColor", targetColor);
                    if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null)
                    {
                        mat.SetTexture("_EmissionMap", mat.GetTexture("_BaseMap"));
                    }
                    
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[ZeroHour] {count} binaya tatlı, hafif turkuaz (cam göbeği) emission eklendi!</color>");
        }

        [MenuItem("ZeroHour/🌟 3. Setup Complete Night Tactical Dispatch Environment")]
        public static void ApplyFullNightAtmosphere()
        {
            SetIsometricCamera();
            ApplyTurquoiseEmission();

            // Setup Crisp Moonlight
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    Undo.RecordObject(l, "Night Directional Light");
                    l.color = new Color(0.70f, 0.88f, 1.0f, 1f); // Soft Ice Blue Moonlight
                    l.intensity = 1.0f;
                    l.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
                }
            }

            RenderSettings.fog = false; // Keep city crisp
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.28f, 0.40f, 1f); // Soft ambient fill

            // Setup Volume
            Volume volume = Object.FindAnyObjectByType<Volume>();
            if (volume != null && volume.profile != null)
            {
                var profile = volume.profile;

                if (!profile.TryGet(out ColorAdjustments colorAdjust))
                {
                    colorAdjust = profile.Add<ColorAdjustments>(true);
                }
                colorAdjust.active = true;
                colorAdjust.colorFilter.overrideState = true;
                colorAdjust.colorFilter.value = new Color(0.75f, 0.92f, 1.0f, 1f);
                colorAdjust.contrast.overrideState = true;
                colorAdjust.contrast.value = 18f;
                colorAdjust.postExposure.overrideState = true;
                colorAdjust.postExposure.value = 0.15f;
                colorAdjust.saturation.overrideState = true;
                colorAdjust.saturation.value = -10f;

                if (!profile.TryGet(out Bloom bloom))
                {
                    bloom = profile.Add<Bloom>(true);
                }
                bloom.active = true;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 1.2f;
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 0.85f;
                bloom.tint.overrideState = true;
                bloom.tint.value = new Color(0.2f, 0.95f, 1f, 1f); // Cyan bloom glow

                if (!profile.TryGet(out Vignette vignette))
                {
                    vignette = profile.Add<Vignette>(true);
                }
                vignette.active = true;
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0.2f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.35f;

                EditorUtility.SetDirty(volume);
                EditorUtility.SetDirty(profile);
            }

            Debug.Log("<color=cyan>[ZeroHour] 🚀 Bütün gece taktik ortamı, turkuaz emission ve izometrik kamera kusursuz kuruldu!</color>");
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace ZeroHour.Phase2.Editor
{
    public static class CityStreetLighting
    {
        private const string LightingParentName = "[City_Street_Lighting]";

        [MenuItem("ZeroHour/5. Setup Street Lights and Vehicle Headlights")]
        [MenuItem("Tools/ZeroHour/Setup City Lights")]
        public static void SetupCityLights()
        {
            Debug.Log("<color=yellow>[ZeroHour] 💡 Şehir sokak lambaları, araç farları ve gece aydınlatması kuruluyor...</color>");

            // 1. Ay Işığı / Gece Ambiyansı Ayarı (Harita gece gibi karanlık olsun ki sokak lambaları ve farlar parıldasın)
            ConfigureNightAtmosphere();

            // 2. Sokak Lambalarına Işık ve Parlayan Ampul Ekle
            SetupStreetLamps();

            // 3. Trafik Işıklarını Canlandır
            SetupTrafficSignals();

            // 4. Araçlara Yolu Aydınlatan Çift Far Ekle
            SetupVehicleHeadlights();

            Debug.Log("<color=green>[ZeroHour] 🌟 Tüm sokak lambaları, trafik ışıkları ve araç farları başarıyla aktif edildi! Şehir gerçek bir gece kriz haritasına dönüştü.</color>");
        }

        [MenuItem("ZeroHour/Remove Generated Street Lights")]
        public static void ClearCityLights()
        {
            GameObject parent = GameObject.Find(LightingParentName);
            if (parent != null)
            {
                Undo.DestroyObjectImmediate(parent);
                Debug.Log("[ZeroHour] Eklenen sokak ışıkları temizlendi.");
            }

            // Araç farlarını temizle
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int clearedFars = 0;
            foreach (var go in allObjects)
            {
                Transform far = go.transform.Find("Vehicle_Headlights");
                if (far != null)
                {
                    Undo.DestroyObjectImmediate(far.gameObject);
                    clearedFars++;
                }
            }
            Debug.Log($"[ZeroHour] {clearedFars} araç farı temizlendi.");
        }

        private static void ConfigureNightAtmosphere()
        {
            // Kullanıcının bina materyallerine KESİNLİKLE DOKUNULMAZ!
            // Sadece yönlü ışığı (Ay ışığını) tatlı bir gece seviyesine çekeriz.
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    Undo.RecordObject(l, "Set Night Moonlight");
                    l.color = new Color(0.35f, 0.5f, 0.75f, 1f); // Soğuk Ay Işığı mavisi
                    l.intensity = 0.45f; // Sokak lambalarının ve farların yolları aydınlatabilmesi için hafif loş
                    EditorUtility.SetDirty(l);
                    break;
                }
            }

            // Ortam ışığını derin gece mavisi yap
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.08f, 0.16f, 1f);
        }

        private static void SetupStreetLamps()
        {
            GameObject parent = GameObject.Find(LightingParentName);
            if (parent == null)
            {
                parent = new GameObject(LightingParentName);
                Undo.RegisterCreatedObjectUndo(parent, "Create Lighting Parent");
            }

            // Sahnede dağınık olan tüm Props_Street Light objelerini bul
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int count = 0;

            // Sıcak altın sarısı sokak lambası rengi (2700K sıcak sodyum lamba)
            Color streetLampColor = new Color(1.0f, 0.82f, 0.52f, 1f);

            // Ampul parlaması için ortak materyal
            Material bulbMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bulbMat.name = "StreetLamp_Bulb_Glow";
            bulbMat.color = streetLampColor;
            bulbMat.EnableKeyword("_EMISSION");
            bulbMat.SetColor("_EmissionColor", streetLampColor * 2.5f);

            foreach (var go in allObjects)
            {
                if (go.name.StartsWith("Props_Street Light"))
                {
                    // Zaten ışık eklenmiş mi kontrol et
                    if (go.transform.Find("Lamp_Light_Source") != null) continue;

                    // Işık ve ampul için alt obje
                    GameObject lightObj = new GameObject("Lamp_Light_Source");
                    lightObj.transform.SetParent(go.transform, false);
                    lightObj.transform.localPosition = new Vector3(0f, 3.7f, 0.85f); // Direğin lamba kafası hizası

                    // 1. Caddeyi aydınlatan Point Light
                    Light ptLight = lightObj.AddComponent<Light>();
                    ptLight.type = LightType.Point;
                    ptLight.color = streetLampColor;
                    ptLight.range = 8.5f; // Yolu ve kaldırımı aydınlatacak mesafe
                    ptLight.intensity = 2.0f;
                    ptLight.shadows = LightShadows.None; // Performans için shadow kapalı

                    // 2. Yukarıdan bakıldığında yanan sarı ampul küresi
                    GameObject bulbVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bulbVisual.name = "Bulb_Mesh";
                    bulbVisual.transform.SetParent(lightObj.transform, false);
                    bulbVisual.transform.localScale = new Vector3(0.35f, 0.2f, 0.35f);
                    
                    // Fizik collider'ını kaldır
                    Collider col = bulbVisual.GetComponent<Collider>();
                    if (col != null) Object.DestroyImmediate(col);

                    MeshRenderer rend = bulbVisual.GetComponent<MeshRenderer>();
                    if (rend != null) rend.sharedMaterial = bulbMat;

                    count++;
                }
            }

            Debug.Log($"[ZeroHour] 💡 {count} adet sokak lambasına sıcak sarı yol aydınlatması ve ampul eklendi.");
        }

        private static void SetupTrafficSignals()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int count = 0;

            Color trafficGreen = new Color(0.1f, 1.0f, 0.4f, 1f);
            Color trafficRed = new Color(1.0f, 0.2f, 0.1f, 1f);

            foreach (var go in allObjects)
            {
                if (go.name.StartsWith("Props_Traffic Signal") && go.transform.Find("Traffic_Light_Source") == null)
                {
                    GameObject lightObj = new GameObject("Traffic_Light_Source");
                    lightObj.transform.SetParent(go.transform, false);
                    lightObj.transform.localPosition = new Vector3(0f, 2.5f, 0.5f);

                    Light ptLight = lightObj.AddComponent<Light>();
                    ptLight.type = LightType.Point;
                    ptLight.color = (count % 2 == 0) ? trafficGreen : trafficRed; // Kavşaklara yeşil/kırmızı canlılık
                    ptLight.range = 3.5f;
                    ptLight.intensity = 1.2f;

                    count++;
                }
            }

            Debug.Log($"[ZeroHour] 🚦 {count} adet trafik lambasına kavşak ışığı eklendi.");
        }

        private static void SetupVehicleHeadlights()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int count = 0;

            Color headlightColor = new Color(1.0f, 0.95f, 0.85f, 1f); // Parlak Halojen Far

            foreach (var go in allObjects)
            {
                string name = go.name.ToLower();
                // Acil durum araçları veya park etmiş araçlar
                if (name.StartsWith("vehicle_") || name.StartsWith("ambulance_") || name.StartsWith("police_") || name.StartsWith("fire_"))
                {
                    if (go.transform.Find("Vehicle_Headlights") != null) continue;

                    GameObject farGroup = new GameObject("Vehicle_Headlights");
                    farGroup.transform.SetParent(go.transform, false);
                    farGroup.transform.localPosition = Vector3.zero;

                    // Sol Far
                    CreateSpotlight(farGroup.transform, new Vector3(-0.42f, 0.45f, 1.15f), new Vector3(6f, -2f, 0f), headlightColor);

                    // Sağ Far
                    CreateSpotlight(farGroup.transform, new Vector3(0.42f, 0.45f, 1.15f), new Vector3(6f, 2f, 0f), headlightColor);

                    count++;
                }
            }

            Debug.Log($"[ZeroHour] 🚗 {count} adet araca yolu aydınlatan çift projektör far takıldı.");
        }

        private static void CreateSpotlight(Transform parent, Vector3 localPos, Vector3 localEuler, Color color)
        {
            GameObject spotObj = new GameObject("Headlight");
            spotObj.transform.SetParent(parent, false);
            spotObj.transform.localPosition = localPos;
            spotObj.transform.localEulerAngles = localEuler;

            Light spot = spotObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = color;
            spot.range = 10.0f; // Önündeki yolu 10 metre boyunca aydınlatır
            spot.spotAngle = 45f;
            spot.innerSpotAngle = 25f;
            spot.intensity = 2.5f;
            spot.shadows = LightShadows.None;
        }
    }
}

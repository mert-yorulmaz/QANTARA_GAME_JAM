using TMPro;
using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Olay yerinin üstünde kısa süre yükselip sönen dünya yazısı (ASILSIZ İHBAR, YAĞMA DOĞRULANDI).
    /// Kameraya döner, uzaklığa göre büyür; UI katmanında olduğu için polaroid fotoğrafa girmez.
    /// </summary>
    public class FloatingLabel : MonoBehaviour
    {
        private const float Lifetime = 3f;
        private const float RiseSpeed = 3f;
        private const float SizePerDistance = 0.035f;
        private const float LiftPerDistance = 0.14f; // Rozetin üstünde kalsın (rozet de uzaklıkla büyür)

        private TextMeshPro text;
        private Color baseColor;
        private Vector3 anchor;
        private Material overlayMaterial;
        private float age;

        private void OnDestroy()
        {
            if (overlayMaterial != null) Destroy(overlayMaterial);
        }

        public static FloatingLabel Spawn(Vector3 position, string message, Color color, TMP_FontAsset font)
        {
            GameObject go = new GameObject("FloatingLabel_" + message);
            go.transform.position = position;
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;

            FloatingLabel label = go.AddComponent<FloatingLabel>();
            label.text = go.AddComponent<TextMeshPro>();
            if (font != null) label.text.font = font;
            label.text.text = message;
            label.text.fontSize = 10f;
            label.text.fontStyle = FontStyles.Bold;
            label.text.alignment = TextAlignmentOptions.Center;
            label.text.textWrappingMode = TextWrappingModes.NoWrap;
            label.text.outlineWidth = 0.25f;
            label.text.outlineColor = new Color32(20, 16, 12, 255);
            label.text.color = color;
            label.baseColor = color;
            label.anchor = position;
            label.text.sortingOrder = 200; // Duman / rozet gibi saydam objelerin önünde çizilsin
            // Binaların arkasında kalmasın: derinlik testi kapalı (yalnız bu yazının materyal kopyası)
            label.overlayMaterial = label.text.fontMaterial;
            label.overlayMaterial.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
            label.LateUpdate();
            return label;
        }

        private void LateUpdate()
        {
            age += Time.deltaTime;

            Camera cam = Camera.main;
            float distance = cam != null ? Vector3.Distance(cam.transform.position, anchor) : 50f;
            transform.position = anchor + Vector3.up * (distance * LiftPerDistance + RiseSpeed * age);
            if (cam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
                transform.localScale = Vector3.one * Mathf.Max(1f, distance * SizePerDistance);
            }

            Color c = baseColor;
            c.a = baseColor.a * Mathf.Clamp01((Lifetime - age) / 0.8f);
            text.color = c;

            if (age >= Lifetime) Destroy(gameObject);
        }
    }
}

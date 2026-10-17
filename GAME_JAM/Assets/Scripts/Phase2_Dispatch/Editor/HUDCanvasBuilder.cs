using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using ZeroHour.Phase2.UI;

namespace ZeroHour.Phase2.Editor
{
    public static class HUDCanvasBuilder
    {
        [MenuItem("ZeroHour/🎨 7. Auto-Build Top HUD Canvas (Tek Tıkla Oluştur)")]
        public static void BuildHUDCanvas()
        {
            // 1. EventSystem Kontrolü
            EnsureEventSystem();

            // 2. Canvas Oluştur veya Bul
            GameObject canvasObj = GameObject.Find("[ZeroHour_HUD_Canvas]");
            if (canvasObj != null)
            {
                if (EditorUtility.DisplayDialog("Canvas Zaten Var", "Sahnede [ZeroHour_HUD_Canvas] zaten var. Yeniden sıfırdan oluşturulsun mu?", "Evet Yenile", "İptal"))
                {
                    Undo.DestroyObjectImmediate(canvasObj);
                }
                else
                {
                    Selection.activeGameObject = canvasObj;
                    return;
                }
            }

            canvasObj = new GameObject("[ZeroHour_HUD_Canvas]");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUD Canvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            DispatchHUD dispatchHUD = canvasObj.AddComponent<DispatchHUD>();

            // Renk paleti (Yağız'ın tasarımına uygun siber / koyu gri tonlar)
            Color panelBgColor = new Color(0.12f, 0.13f, 0.15f, 0.92f);
            Color headerCyanColor = new Color(0.55f, 0.85f, 0.95f, 1f);
            Color labelLightGrey = new Color(0.85f, 0.88f, 0.9f, 1f);
            Color textCountWhite = Color.white;

            // ==========================================
            // SOL ÜST: UNITS PANELİ
            // ==========================================
            GameObject unitsPanel = CreateUIPanel("Panel_Units", canvasObj.transform, panelBgColor);
            RectTransform unitsRect = unitsPanel.GetComponent<RectTransform>();
            unitsRect.anchorMin = new Vector2(0f, 1f);
            unitsRect.anchorMax = new Vector2(0f, 1f);
            unitsRect.pivot = new Vector2(0f, 1f);
            unitsRect.anchoredPosition = new Vector2(25f, -25f);
            unitsRect.sizeDelta = new Vector2(560f, 95f);

            // UNITS Başlık Text'i
            TextMeshProUGUI unitsHeader = CreateTMPText("Text_Header", unitsPanel.transform, "UNITS", 18f, headerCyanColor, FontStyles.Bold);
            RectTransform headerRect = unitsHeader.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = new Vector2(16f, -10f);
            headerRect.sizeDelta = new Vector2(120f, 24f);

            // 4 Birim Kolonları (Police, Ambulance, Fire Truck, Helicopter)
            float colStartX = 15f;
            float colWidth = 130f;
            float colY = -35f;

            // 1. POLICE
            var policeTexts = CreateUnitColumn("Unit_Police", unitsPanel.transform, "Police", "3", new Color(0.3f, 0.65f, 1f), new Vector2(colStartX + 0 * colWidth, colY));
            // 2. AMBULANCE
            var ambTexts = CreateUnitColumn("Unit_Ambulance", unitsPanel.transform, "Ambulance", "1", new Color(1f, 0.35f, 0.45f), new Vector2(colStartX + 1 * colWidth, colY));
            // 3. FIRE TRUCK
            var fireTexts = CreateUnitColumn("Unit_FireTruck", unitsPanel.transform, "Fire Truck", "1", new Color(1f, 0.55f, 0.2f), new Vector2(colStartX + 2 * colWidth, colY));
            // 4. HELICOPTER
            var heliTexts = CreateUnitColumn("Unit_Helicopter", unitsPanel.transform, "Helicopter", "1", new Color(1f, 0.85f, 0.25f), new Vector2(colStartX + 3 * colWidth, colY));

            // ==========================================
            // SAĞ ÜST: NÜFUS PANELİ (150 / 150)
            // ==========================================
            GameObject popPanel = CreateUIPanel("Panel_Population", canvasObj.transform, panelBgColor);
            RectTransform popRect = popPanel.GetComponent<RectTransform>();
            popRect.anchorMin = new Vector2(1f, 1f);
            popRect.anchorMax = new Vector2(1f, 1f);
            popRect.pivot = new Vector2(1f, 1f);
            popRect.anchoredPosition = new Vector2(-25f, -25f);
            popRect.sizeDelta = new Vector2(230f, 75f);

            // Sivil İkon Kutusu (Yeşil Simge)
            GameObject personIconObj = CreateUIPanel("Icon_Person", popPanel.transform, new Color(0.35f, 0.95f, 0.7f, 1f));
            RectTransform iconRect = personIconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(18f, 0f);
            iconRect.sizeDelta = new Vector2(28f, 28f);

            // 150 / 150 Text'i
            TextMeshProUGUI popText = CreateTMPText("Text_Population", popPanel.transform, "150 / 150", 28f, new Color(0.45f, 1f, 0.75f), FontStyles.Bold);
            RectTransform popTextRect = popText.rectTransform;
            popTextRect.anchorMin = new Vector2(0f, 0f);
            popTextRect.anchorMax = new Vector2(1f, 1f);
            popTextRect.offsetMin = new Vector2(58f, 0f);
            popTextRect.offsetMax = new Vector2(-12f, 0f);
            popText.alignment = TextAlignmentOptions.MidlineLeft;

            // ==========================================
            // DISPATCH HUD SCRIPTİNİ BAĞLA (SERIALIZE ET)
            // ==========================================
            SerializedObject so = new SerializedObject(dispatchHUD);
            so.FindProperty("policeCountText").objectReferenceValue = policeTexts.countText;
            so.FindProperty("ambulanceCountText").objectReferenceValue = ambTexts.countText;
            so.FindProperty("fireTruckCountText").objectReferenceValue = fireTexts.countText;
            so.FindProperty("helicopterCountText").objectReferenceValue = heliTexts.countText;
            so.FindProperty("populationText").objectReferenceValue = popText;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = canvasObj;
            Debug.Log("<color=green>[ZeroHour] 🎨 Top HUD Canvas başarıyla oluşturuldu! Units barı ve 150/150 nüfus sayacı bağlandı.</color>");
        }

        private static (TextMeshProUGUI labelText, TextMeshProUGUI countText) CreateUnitColumn(string colName, Transform parent, string label, string initialCount, Color iconColor, Vector2 anchoredPos)
        {
            GameObject colObj = new GameObject(colName, typeof(RectTransform));
            colObj.transform.SetParent(parent, false);

            RectTransform colRect = colObj.GetComponent<RectTransform>();
            colRect.anchorMin = new Vector2(0f, 1f);
            colRect.anchorMax = new Vector2(0f, 1f);
            colRect.pivot = new Vector2(0f, 1f);
            colRect.anchoredPosition = anchoredPos;
            colRect.sizeDelta = new Vector2(125f, 50f);

            // Küçük renkli simge kutusu
            GameObject iconObj = CreateUIPanel("Icon", colObj.transform, iconColor);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, 0f);
            iconRect.sizeDelta = new Vector2(16f, 16f);

            // İsim etiketi (Örn: Police)
            TextMeshProUGUI labelText = CreateTMPText("Label", colObj.transform, label, 14f, new Color(0.85f, 0.88f, 0.9f), FontStyles.Normal);
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(22f, 0f);
            labelRect.sizeDelta = new Vector2(100f, 18f);

            // Alttaki Sayı Text'i (Örn: 3)
            TextMeshProUGUI countText = CreateTMPText("Count", colObj.transform, initialCount, 22f, Color.white, FontStyles.Bold);
            RectTransform countRect = countText.rectTransform;
            countRect.anchorMin = new Vector2(0f, 0f);
            countRect.anchorMax = new Vector2(1f, 0f);
            countRect.pivot = new Vector2(0f, 0f);
            countRect.anchoredPosition = new Vector2(22f, 2f);
            countRect.sizeDelta = new Vector2(100f, 26f);

            return (labelText, countText);
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Color bgColor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);

            Image img = obj.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;

            return obj;
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, float fontSize, Color color, FontStyles style)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;

            return tmp;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }
        }
    }
}

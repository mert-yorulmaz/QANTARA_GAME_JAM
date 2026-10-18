using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Units;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Rozete tıklanınca açılan sarı Kaza Dosyası (Damage Control Line · Incident File).
    /// Polaroid fotoğraf, adres, yaralı sayısı ve [-] 0 [+] birim seçicileri; DISPATCH ve DISMISS CALL.
    /// </summary>
    public class IncidentFileUI : MonoBehaviour
    {
        public static IncidentFileUI Instance { get; private set; }

        /// <summary>ESC bu karede Kaza Dosyasını kapatmak için kullanıldıysa kare numarası.</summary>
        public static int EscConsumedFrame { get; private set; } = -1;

        [Serializable]
        public class UnitRow
        {
            public UnitType unitType;
            public TMP_Text distanceText;
            public TMP_Text freeText;
            public TMP_Text countText;
            public Button minusButton;
            public Button plusButton;
            [NonSerialized] public int selected;
        }

        /// <summary>Seçilen her helikopter için bir bölme: ambulans / polis / itfaiye rolünden biri seçilir.</summary>
        [Serializable]
        public class HeliRoleSlot
        {
            public GameObject root;
            public Button ambulanceButton;
            public Button policeButton;
            public Button fireButton;
            public Graphic ambulanceSelected;
            public Graphic policeSelected;
            public Graphic fireSelected;
            [NonSerialized] public UnitType role;
        }

        [Header("Kök")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button backdropButton;

        [Header("Başlık ve Bilgiler")]
        [SerializeField] private TMP_Text caseTabText;
        [SerializeField] private TMP_Text callInfoText;
        [SerializeField] private TMP_Text stampText;
        [SerializeField] private TMP_Text incidentValueText;
        [SerializeField] private TMP_Text addressValueText;
        [SerializeField] private TMP_Text affectedValueText;
        [SerializeField] private TMP_Text notesValueText;

        [Header("Polaroid")]
        [SerializeField] private RawImage photoImage;
        [SerializeField] private TMP_Text photoCaptionText;
        [SerializeField] private Vector2Int photoResolution = new Vector2Int(1112, 692);

        [Header("Birim Tablosu")]
        [SerializeField] private List<UnitRow> unitRows = new List<UnitRow>();

        [Header("Joker Helikopter Rolleri")]
        [SerializeField] private List<HeliRoleSlot> heliRoleSlots = new List<HeliRoleSlot>();

        [Header("Butonlar")]
        [SerializeField] private Button dismissButton;
        [SerializeField] private Button dispatchButton;
        [SerializeField] private Graphic dispatchActiveFill;
        [SerializeField] private TMP_Text dispatchLabel;

        [Header("Mesafe Eşikleri (m)")]
        [SerializeField] private float nearDistance = 120f;
        [SerializeField] private float midDistance = 240f;

        private static readonly Color NearColor = new Color(0.13f, 0.48f, 0.30f);
        private static readonly Color MidColor = new Color(0.62f, 0.44f, 0.08f);
        private static readonly Color FarColor = new Color(0.70f, 0.15f, 0.12f);
        private static readonly Color InkColor = new Color(0.165f, 0.137f, 0.106f);
        private static readonly Color CreamColor = new Color(0.93f, 0.89f, 0.78f);

        private IncidentReport current;
        private Camera photoCamera;
        private RenderTexture photoTexture;
        private readonly List<Renderer> hiddenForPhoto = new List<Renderer>();

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            Instance = this;

            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (dismissButton != null) dismissButton.onClick.AddListener(OnDismissClicked);
            if (dispatchButton != null) dispatchButton.onClick.AddListener(OnDispatchClicked);

            foreach (UnitRow row in unitRows)
            {
                UnitRow captured = row;
                if (row.minusButton != null) row.minusButton.onClick.AddListener(() => ChangeCount(captured, -1));
                if (row.plusButton != null) row.plusButton.onClick.AddListener(() => ChangeCount(captured, +1));
            }

            foreach (HeliRoleSlot slot in heliRoleSlots)
            {
                HeliRoleSlot captured = slot;
                if (slot.ambulanceButton != null) slot.ambulanceButton.onClick.AddListener(() => SetHeliRole(captured, UnitType.Ambulance));
                if (slot.policeButton != null) slot.policeButton.onClick.AddListener(() => SetHeliRole(captured, UnitType.PoliceCruiser));
                if (slot.fireButton != null) slot.fireButton.onClick.AddListener(() => SetHeliRole(captured, UnitType.FireEngine));
            }

            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (photoTexture != null) photoTexture.Release();
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += HideMarkersForPhoto;
            RenderPipelineManager.endCameraRendering += RestoreMarkersAfterPhoto;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= HideMarkersForPhoto;
            RenderPipelineManager.endCameraRendering -= RestoreMarkersAfterPhoto;
        }

        // Polaroid'i olay rozetleri ve geri sayım kaplamasın: sadece foto kamerası çizerken gizle
        private void HideMarkersForPhoto(ScriptableRenderContext context, Camera cam)
        {
            if (cam != photoCamera || IncidentBoard.Instance == null) return;

            hiddenForPhoto.Clear();
            foreach (IncidentReport r in IncidentBoard.Instance.ActiveIncidents)
            {
                if (r.marker == null) continue;
                foreach (Renderer rend in r.marker.GetComponentsInChildren<Renderer>())
                {
                    if (!rend.enabled) continue;
                    rend.enabled = false;
                    hiddenForPhoto.Add(rend);
                }
            }
        }

        private void RestoreMarkersAfterPhoto(ScriptableRenderContext context, Camera cam)
        {
            if (cam != photoCamera) return;

            foreach (Renderer rend in hiddenForPhoto)
            {
                if (rend != null) rend.enabled = true;
            }
            hiddenForPhoto.Clear();
        }

        // ==========================================
        // AÇ / KAPAT
        // ==========================================

        public void Open(IncidentReport report)
        {
            if (report == null || report.status == CallIncidentStatus.Closed) return;

            current = report;
            foreach (UnitRow row in unitRows) row.selected = 0;

            // Helikopter rolü varsayılanı: olayı çözen ekip (yangın → itfaiye, çökme → ambulans)
            foreach (HeliRoleSlot slot in heliRoleSlots) slot.role = report.RequiredUnit;

            SetText(caseTabText, $"CASE #{report.caseNumber:0000}");
            SetText(stampText, report.urgent ? "URGENT" : "ROUTINE");
            SetText(incidentValueText, report.incidentTitle);
            SetText(addressValueText, report.address);
            SetText(affectedValueText, $"{report.affected} people · {report.injured} injured");
            SetText(notesValueText, $"{report.notes}\n<size=92%>{report.possibleUnitsText}</size>");
            SetText(photoCaptionText, $"{report.streetShort} · {report.callTime}");

            foreach (UnitRow row in unitRows)
            {
                SetDistance(row);
            }

            root.SetActive(true);
            Refresh();
            StartCoroutine(TakePhoto(report));
        }

        public void Close()
        {
            current = null;
            if (root != null) root.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen) return;

            if (current == null || current.status == CallIncidentStatus.Closed)
            {
                Close();
                return;
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                EscConsumedFrame = Time.frameCount; // Aynı ESC ayarlar menüsünü açmasın
                Close();
                return;
            }

            Refresh();
        }

        // ==========================================
        // TABLO VE BUTON DURUMU
        // ==========================================

        private void Refresh()
        {
            bool waiting = current.status == CallIncidentStatus.Waiting;
            AStarMovementTester fleet = AStarMovementTester.Instance;
            int totalSelected = 0;

            int helisSelected = 0;
            foreach (UnitRow row in unitRows)
            {
                int free = fleet != null ? fleet.GetAvailableCount(row.unitType) : 0;
                // Helikopter sayısı rol bölmesi sayısını aşamaz
                int max = row.unitType == UnitType.RescueHelicopter ? Mathf.Min(free, heliRoleSlots.Count) : free;
                row.selected = Mathf.Clamp(row.selected, 0, max);
                if (row.unitType == UnitType.RescueHelicopter) helisSelected = row.selected;
                totalSelected += row.selected;

                SetText(row.freeText, free.ToString());
                SetText(row.countText, row.selected.ToString());
                if (row.minusButton != null) row.minusButton.interactable = waiting && row.selected > 0;
                if (row.plusButton != null) row.plusButton.interactable = waiting && row.selected < max;
            }

            RefreshHeliSlots(waiting ? helisSelected : 0);

            string timeLeft = waiting
                ? $" · {Mathf.CeilToInt(Mathf.Max(0f, current.timeRemaining))}s left"
                : current.status == CallIncidentStatus.Working ? " · UNITS ON SCENE" : " · UNITS EN ROUTE";
            SetText(callInfoText, $"Call {current.callTime} · Line {current.line}{timeLeft}");

            bool canDispatch = waiting && totalSelected > 0;
            if (dispatchButton != null) dispatchButton.interactable = canDispatch;
            if (dispatchActiveFill != null) dispatchActiveFill.enabled = canDispatch;
            if (dispatchLabel != null) dispatchLabel.color = canDispatch ? CreamColor : new Color(CreamColor.r, CreamColor.g, CreamColor.b, 0.8f);
            if (dismissButton != null) dismissButton.interactable = waiting;
        }

        private void RefreshHeliSlots(int visibleCount)
        {
            for (int i = 0; i < heliRoleSlots.Count; i++)
            {
                HeliRoleSlot slot = heliRoleSlots[i];
                bool visible = i < visibleCount;
                if (slot.root != null && slot.root.activeSelf != visible) slot.root.SetActive(visible);
                if (!visible) continue;

                SetSelected(slot.ambulanceSelected, slot.role == UnitType.Ambulance);
                SetSelected(slot.policeSelected, slot.role == UnitType.PoliceCruiser);
                SetSelected(slot.fireSelected, slot.role == UnitType.FireEngine);
            }
        }

        private static void SetSelected(Graphic highlight, bool selected)
        {
            if (highlight != null) highlight.enabled = selected;
        }

        private void SetHeliRole(HeliRoleSlot slot, UnitType role)
        {
            if (current == null || current.status != CallIncidentStatus.Waiting) return;
            slot.role = role;
            Refresh();
        }

        private void ChangeCount(UnitRow row, int delta)
        {
            if (current == null || current.status != CallIncidentStatus.Waiting) return;
            row.selected += delta;
            Refresh();
        }

        private void SetDistance(UnitRow row)
        {
            if (row.distanceText == null) return;

            AStarMovementTester fleet = AStarMovementTester.Instance;
            if (fleet == null || !fleet.TryGetHomePosition(row.unitType, out Vector3 home))
            {
                row.distanceText.text = "—";
                row.distanceText.color = InkColor;
                return;
            }

            Vector3 a = home; a.y = 0f;
            Vector3 b = current.sitePosition; b.y = 0f;
            float d = Vector3.Distance(a, b);

            if (d <= nearDistance) { row.distanceText.text = "NEAR"; row.distanceText.color = NearColor; }
            else if (d <= midDistance) { row.distanceText.text = "MID"; row.distanceText.color = MidColor; }
            else { row.distanceText.text = "FAR"; row.distanceText.color = FarColor; }
        }

        private void OnDispatchClicked()
        {
            if (current == null || IncidentBoard.Instance == null) return;

            List<UnitType> units = new List<UnitType>();
            List<UnitType> heliRoles = new List<UnitType>();
            foreach (UnitRow row in unitRows)
            {
                for (int i = 0; i < row.selected; i++)
                {
                    units.Add(row.unitType);
                    if (row.unitType == UnitType.RescueHelicopter && i < heliRoleSlots.Count) heliRoles.Add(heliRoleSlots[i].role);
                }
            }

            if (IncidentBoard.Instance.Dispatch(current, units, heliRoles) > 0) Close();
        }

        private void OnDismissClicked()
        {
            if (current == null || IncidentBoard.Instance == null) return;
            IncidentBoard.Instance.Dismiss(current);
            Close();
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }

        // ==========================================
        // POLAROID: Olay binasının anlık fotoğrafı
        // ==========================================

        private IEnumerator TakePhoto(IncidentReport report)
        {
            Camera main = Camera.main;
            if (photoImage == null || main == null) yield break;

            if (photoTexture == null)
            {
                photoTexture = new RenderTexture(photoResolution.x, photoResolution.y, 24) { name = "IncidentPhoto_RT" };
                photoTexture.Create();
                photoImage.texture = photoTexture;
            }

            if (photoCamera == null)
            {
                GameObject camObj = new GameObject("IncidentPhotoCamera");
                camObj.transform.SetParent(transform, false);
                photoCamera = camObj.AddComponent<Camera>();
                photoCamera.enabled = false;
            }

            photoCamera.CopyFrom(main);
            photoCamera.targetTexture = photoTexture;
            photoCamera.fieldOfView = 32f;
            photoCamera.depth = main.depth - 10f;

            Bounds bounds = report.buildingBounds;
            float frameSize = Mathf.Max(bounds.extents.magnitude, 8f);
            Vector3 forward = main.transform.forward;
            photoCamera.transform.SetPositionAndRotation(bounds.center - forward * (frameSize * 4.2f + 20f), main.transform.rotation);

            // URP: kamerayı tek kare açıp RenderTexture'a çizdir
            photoCamera.enabled = true;
            yield return null;
            yield return new WaitForEndOfFrame();
            if (photoCamera != null) photoCamera.enabled = false;
        }
    }
}

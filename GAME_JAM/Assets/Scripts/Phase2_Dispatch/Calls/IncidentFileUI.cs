using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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

            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (photoTexture != null) photoTexture.Release();
        }

        // ==========================================
        // AÇ / KAPAT
        // ==========================================

        public void Open(IncidentReport report)
        {
            if (report == null || report.status == CallIncidentStatus.Closed) return;

            current = report;
            foreach (UnitRow row in unitRows) row.selected = 0;

            SetText(caseTabText, $"CASE #{report.caseNumber:0000}");
            SetText(stampText, report.urgent ? "URGENT" : "ROUTINE");
            SetText(incidentValueText, report.incidentTitle);
            SetText(addressValueText, report.address);
            SetText(affectedValueText, $"{report.affected} people · {report.injured} injured");
            SetText(notesValueText, report.notes);
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

            foreach (UnitRow row in unitRows)
            {
                int free = fleet != null ? fleet.GetAvailableCount(row.unitType) : 0;
                row.selected = Mathf.Clamp(row.selected, 0, free);
                totalSelected += row.selected;

                SetText(row.freeText, free.ToString());
                SetText(row.countText, row.selected.ToString());
                if (row.minusButton != null) row.minusButton.interactable = waiting && row.selected > 0;
                if (row.plusButton != null) row.plusButton.interactable = waiting && row.selected < free;
            }

            string timeLeft = waiting
                ? $" · {Mathf.CeilToInt(Mathf.Max(0f, current.timeRemaining))}s left"
                : " · UNITS EN ROUTE";
            SetText(callInfoText, $"Call {current.callTime} · Line {current.line}{timeLeft}");

            bool canDispatch = waiting && totalSelected > 0;
            if (dispatchButton != null) dispatchButton.interactable = canDispatch;
            if (dispatchActiveFill != null) dispatchActiveFill.enabled = canDispatch;
            if (dispatchLabel != null) dispatchLabel.color = canDispatch ? CreamColor : new Color(CreamColor.r, CreamColor.g, CreamColor.b, 0.8f);
            if (dismissButton != null) dismissButton.interactable = waiting;
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
            foreach (UnitRow row in unitRows)
            {
                for (int i = 0; i < row.selected; i++) units.Add(row.unitType);
            }

            if (IncidentBoard.Instance.Dispatch(current, units) > 0) Close();
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

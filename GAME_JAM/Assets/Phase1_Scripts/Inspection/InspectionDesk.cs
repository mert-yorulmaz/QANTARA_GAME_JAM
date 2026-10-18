using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Phase 1 - Inspection Desk (BIA OS).
// Masaya ilk oturuşta 08:00'de mesai başlar; dosyalar kural kitabına göre onaylanır ya da reddedilir.
// 17:00'de zil çalar ve mesai biter.
public class InspectionDesk : MonoBehaviour
{
    [Header("Shift")]
    public List<InspectionCase> cases = new List<InspectionCase>();
    [Tooltip("Bu tarihten önce açılmış dosyalar Kural 1.1'e göre eskidir (dd MMM yyyy).")]
    public string outdatedBefore = "01 Sep 2025";
    public ShiftClock clock;
    public BiaDesktop desktop;

    [Header("Money")]
    public int correctReward = 200;
    public int wrongPenalty = 100;

    [Header("Windows")]
    public Win7Window caseWindow;
    public Win7Window archiveWindow;
    public Win7Window reportWindow;
    public TMP_Text caseStatus;
    public TMP_Text errorStatus;
    public TMP_Text archiveText;
    public TMP_Text reportText;

    [Header("File")]
    public GameObject fileContent;
    public TMP_Text tabLabel;
    public TMP_Text buildingText;
    public TMP_Text addressText;
    public TMP_Text filedText;
    public TMP_Text groundUseText;
    public TMP_Text notesText;
    public TMP_Text signedText;
    public TMP_Text wallReportText;
    public TMP_Text bagText;
    public TMP_Text sealText;
    public TMP_Text exitsText;
    public TMP_Text attachedText;
    public TMP_Text feedbackText;
    public TMP_Text summaryText;

    [Header("Photo / X-Ray")]
    public RawImage photo;
    public GameObject xrayGrid;
    public RawImage xrayMarks;
    public TMP_Text photoLabel;
    public Button xrayButton;
    public Sprite xrayOff;
    public Sprite xrayOn;
    public TMP_Text xrayLabel;
    [Tooltip("X-RAY butonunun çevresinde yanıp sönen çerçeve; dosyada X-RAY açılana kadar yanıp söner.")]
    public Image xrayGlow;
    public float xrayBlinkSpeed = 4f;

    // Dosyadaki bir alanın hangi kurala bağlı olduğu.
    // Kutucuğa tıklamak "kontrol edildi" işaretini açıp kapatır; alan adına tıklamak kural kitabında ilgili sayfayı açar.
    [Serializable]
    public class RuleCheck
    {
        public string field;
        [TextArea] public string hint;
        [Tooltip("Kural kitabında ilgili sayfa (0 = giriş).")]
        public int ruleBookPage;
        public Button tickButton;
        public Button labelButton;
        public Image tick;
    }

    [Header("Rule checks")]
    [Tooltip("Karar vermeden önce her alan kurallara göre kontrol edilmelidir (X-RAY de sayılır).")]
    public RuleCheck[] ruleChecks;
    public Sprite tickEmpty;
    public Sprite tickDone;
    public TMP_Text checkHint;
    public CanvasGroup approveGroup;
    public CanvasGroup rejectGroup;

    [Header("Verdict")]
    public Button approveButton;
    public Button rejectButton;
    public Button nextButton;
    public TMP_Text nextLabel;
    public RectTransform approvedStamp;
    public RectTransform rejectedStamp;
    public TMP_Text rejectedRules;

    [Header("Panels")]
    public RejectionSlip rejectionSlip;
    public RuleBook ruleBook;

    // Durum yazısı masaüstü zemininin üzerinde durur.
    static readonly Color OkColor = UIFactory.Hex("#7FE0B5");
    static readonly Color ErrorColor = UIFactory.Hex("#FF8A80");

    struct ArchiveEntry
    {
        public int caseNumber;
        public string building;
        public string verdict;
        public bool correct;
    }

    readonly List<ArchiveEntry> archive = new List<ArchiveEntry>();

    // Bakiye değişince farkla birlikte bildirilir (+200 / -100).
    public event Action<int> MoneyChanged;
    public int Money { get; private set; }
    public int ReviewedCount => archive.Count;
    public int CaseCount => cases.Count;

    int index;
    int errors;
    bool allReviewed;
    bool xrayActive;
    bool xrayUsed;
    bool[] checkedFields = new bool[0];
    bool decided;
    InspectionCase current;
    RuleViolation violations;
    DateTime outdatedCutoff;

    void Awake()
    {
        if (!InspectionRules.TryParseDate(outdatedBefore, out outdatedCutoff))
        {
            Debug.LogWarning($"InspectionDesk: '{outdatedBefore}' tarihi okunamadı ({InspectionRules.DateFormat} bekleniyor).");
            outdatedCutoff = DateTime.MinValue;
        }

        xrayButton.onClick.AddListener(() =>
        {
            SetXRay(!xrayActive);
            if (xrayActive)
                Sfx.Play(SfxId.XRay);
        });
        approveButton.onClick.AddListener(Approve);
        rejectButton.onClick.AddListener(() =>
        {
            Sfx.Play(SfxId.Paper);
            rejectionSlip.Open();
        });
        nextButton.onClick.AddListener(() =>
        {
            Sfx.Play(SfxId.Paper);
            Next();
        });

        checkedFields = new bool[ruleChecks.Length];
        for (int i = 0; i < ruleChecks.Length; i++)
        {
            int index = i;
            // Eksik referans (eski kurulum) masanın tamamını bozmasın.
            if (ruleChecks[i].tickButton != null)
                ruleChecks[i].tickButton.onClick.AddListener(() => ToggleCheck(index));
            if (ruleChecks[i].labelButton != null)
                ruleChecks[i].labelButton.onClick.AddListener(() => ShowRule(index));
        }

        rejectionSlip.Init();
        rejectionSlip.Confirmed += ConfirmRejection;
        rejectionSlip.Cancelled += rejectionSlip.Close;
        rejectionSlip.RuleBookRequested += ruleBook.Open;
        rejectionSlip.Close();

        ruleBook.Init();
        clock.ShiftEnded += EndShift;

        if (cases.Count == 0)
            cases = InspectionRules.DefaultCases();
        ShowCase(0);
        RefreshArchive();
    }

    void OnDestroy()
    {
        if (clock != null)
            clock.ShiftEnded -= EndShift;
    }

    void Update()
    {
        // Masa yalnızca oyuncu bilgisayara oturduğunda güncellenir; mesai ilk oturuşta başlar.
        // (Start kullanılmaz: sahne yüklenirken ekran bir kare açık olabilir.)
        if (!clock.HasStarted)
        {
            Sfx.Play(SfxId.Boot);
            clock.StartShift();
            desktop.Notify($"Shift started · {clock.TimeText}",
                "Good morning, inspector. New files are waiting in the Case Files folder.");
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (ruleBook.IsOpen)
                ruleBook.Close();
            else
                ruleBook.Open();
        }

        if (reportWindow.IsVisible)
            RefreshReport();

        UpdateXRayGlow();
    }

    void UpdateXRayGlow()
    {
        if (xrayGlow == null)
            return;

        var buttonImage = (Image)xrayButton.targetGraphic;
        bool blink = !xrayUsed && fileContent.activeSelf;
        if (xrayGlow.gameObject.activeSelf != blink)
            xrayGlow.gameObject.SetActive(blink);
        if (!blink)
        {
            buttonImage.color = Color.white;
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * xrayBlinkSpeed);
        var color = xrayGlow.color;
        color.a = 0.1f + 0.9f * pulse;
        xrayGlow.color = color;

        // Buton da aynı ritimle amber tona döner.
        buttonImage.color = Color.Lerp(Color.white, XRayPulseTint, pulse);
    }

    static readonly Color XRayPulseTint = new Color(1f, 0.86f, 0.45f, 1f);

    void ShowCase(int caseIndex)
    {
        index = caseIndex;
        current = cases[index];
        violations = InspectionRules.Evaluate(current, outdatedCutoff);

        tabLabel.text = $"CASE #{index + 1:0000}";
        buildingText.text = current.building;
        addressText.text = current.address;
        filedText.text = current.filed;
        groundUseText.text = InspectionRules.Display(current.groundUse);
        notesText.text = current.notes;
        signedText.text = current.signed ? "Yes" : "No";
        wallReportText.text = InspectionRules.WallReport(current);
        bagText.text = InspectionRules.Display(current.bag);
        sealText.text = current.seal ? "Yes" : "No";
        exitsText.text = InspectionRules.Display(current.exits);
        attachedText.text = InspectionRules.Display(current.attached);

        ReplaceTexture(photo, InspectionArt.BuildingPhoto(current.photoSeed));
        ReplaceTexture(xrayMarks, InspectionArt.XRayMarks(current));
        SetXRay(false);
        xrayUsed = false;
        decided = false;
        ResetChecks();

        fileContent.SetActive(true);
        summaryText.gameObject.SetActive(false);
        approvedStamp.gameObject.SetActive(false);
        rejectedStamp.gameObject.SetActive(false);
        approveButton.gameObject.SetActive(true);
        rejectButton.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(false);
        nextLabel.text = index == cases.Count - 1 ? "FINISH >" : "NEXT FILE >";
        feedbackText.text = "";
        rejectionSlip.Close();

        UpdateStatus();
    }

    static void ReplaceTexture(RawImage target, Texture2D texture)
    {
        if (target.texture != null)
            Destroy(target.texture);
        target.texture = texture;
    }

    void SetXRay(bool active)
    {
        xrayActive = active;
        if (active && !xrayUsed)
        {
            xrayUsed = true;
            if (!decided)
                ShowCheckHint("Rules 2.1 & 3.1 · Look for red damage marks and a yellow FE (fire extinguisher) mark.");
            UpdateDecisionLock();
        }
        photo.gameObject.SetActive(!active);
        xrayGrid.SetActive(active);
        xrayMarks.gameObject.SetActive(active);
        ((Image)xrayButton.targetGraphic).sprite = active ? xrayOn : xrayOff;
        xrayLabel.text = active ? "EXTERIOR VIEW" : "X-RAY SCAN";
        photoLabel.text = $"FILE {index + 1:00} · {(active ? "X-RAY" : "EXTERIOR")}";
    }

    // ---------- Kural kontrolü ----------

    int CheckedCount
    {
        get
        {
            int count = xrayUsed ? 1 : 0;
            foreach (bool c in checkedFields)
                if (c)
                    count++;
            return count;
        }
    }

    int CheckTotal => ruleChecks.Length + 1;
    bool AllChecked => CheckedCount >= CheckTotal;

    void ResetChecks()
    {
        for (int i = 0; i < ruleChecks.Length; i++)
        {
            checkedFields[i] = false;
            if (ruleChecks[i].tick != null)
                ruleChecks[i].tick.sprite = tickEmpty;
        }
        ShowCheckHint(null);
        UpdateDecisionLock();
    }

    // Kutucuk: tekrar tıklanınca işaret kalkar.
    void ToggleCheck(int i)
    {
        if (decided)
            return;

        Sfx.Play(SfxId.Tick);
        checkedFields[i] = !checkedFields[i];
        ruleChecks[i].tick.sprite = checkedFields[i] ? tickDone : tickEmpty;
        ShowCheckHint(checkedFields[i] ? ruleChecks[i].hint : null);
        UpdateDecisionLock();
    }

    // Alan adı: işaretlemeden kural kitabını ilgili kuralın sayfasında açar.
    void ShowRule(int i)
    {
        ruleBook.Open();
        ruleBook.Show(ruleChecks[i].ruleBookPage);
        if (!decided)
            ShowCheckHint(ruleChecks[i].hint);
    }

    void ShowCheckHint(string ruleHint)
    {
        if (checkHint == null)
            return;

        string progress = AllChecked
            ? "<color=#2E6B34>All rules checked. Make your decision.</color>"
            : $"<color=#A8241C>Check every field against the Rule Book before deciding  ({CheckedCount}/{CheckTotal})</color>";
        checkHint.text = string.IsNullOrEmpty(ruleHint) ? progress : ruleHint + "\n" + progress;
    }

    // Tüm alanlar ve X-RAY kontrol edilmeden APPROVE / REJECT kilitli.
    void UpdateDecisionLock()
    {
        bool unlocked = AllChecked;
        approveButton.interactable = unlocked;
        rejectButton.interactable = unlocked;
        if (approveGroup != null)
            approveGroup.alpha = unlocked ? 1f : 0.45f;
        if (rejectGroup != null)
            rejectGroup.alpha = unlocked ? 1f : 0.45f;

        // Hint satırındaki sayaç her kontrolde güncellenir; kural açıklaması korunur.
        if (checkHint != null && !decided)
        {
            string text = checkHint.text;
            int newline = text.IndexOf('\n');
            ShowCheckHint(newline > 0 ? text.Substring(0, newline) : null);
        }
    }

    void Approve()
    {
        bool correct = violations == RuleViolation.None;
        Resolve(correct, "APPROVED", correct
            ? "Correct. This file meets every regulation."
            : $"Wrong. This file breaks rule {InspectionRules.CodeList(violations)}.");
        Stamp(approvedStamp);
    }

    void ConfirmRejection(RuleViolation cited)
    {
        rejectionSlip.Close();

        string message;
        if (violations == RuleViolation.None)
        {
            message = "Wrong. This file meets every regulation.";
        }
        else if (cited == violations)
        {
            message = "Correct. Every violation cited.";
        }
        else
        {
            var missed = violations & ~cited;
            var extra = cited & ~violations;
            var parts = new List<string>();
            if (missed != RuleViolation.None)
                parts.Add($"You missed rule {InspectionRules.CodeList(missed)}");
            if (extra != RuleViolation.None)
                parts.Add($"{(parts.Count == 0 ? "Rule" : "rule")} {InspectionRules.CodeList(extra)} does not apply");
            message = "Wrong. " + string.Join("; ", parts) + ".";
        }

        Resolve(violations != RuleViolation.None && cited == violations, "REJECTED", message);
        rejectedRules.text = "RULE " + InspectionRules.CodeList(cited);
        Stamp(rejectedStamp);
    }

    void Resolve(bool correct, string verdict, string message)
    {
        decided = true;
        if (checkHint != null)
            checkHint.text = "";
        if (!correct)
            errors++;
        StartCoroutine(PlayResult(correct));

        int delta = correct ? correctReward : -wrongPenalty;
        Money += delta;
        MoneyChanged?.Invoke(delta);
        message += "  " + FormatDelta(delta);

        archive.Add(new ArchiveEntry
        {
            caseNumber = index + 1,
            building = current.building,
            verdict = verdict,
            correct = correct
        });
        RefreshArchive();

        feedbackText.text = message;
        feedbackText.color = correct ? UIFactory.Green : UIFactory.Red;
        approveButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(true);
        UpdateStatus();
    }

    void Next()
    {
        if (index < cases.Count - 1)
        {
            ShowCase(index + 1);
            return;
        }

        allReviewed = true;
        ShowSummary("ALL FILES REVIEWED", "Wait for the bell at 17:00, then head to your night shift.");
        desktop.Notify("All files reviewed", "No more files today. Wait for the bell at 17:00.");
    }

    void EndShift()
    {
        rejectionSlip.Close();
        ShowSummary("SHIFT OVER", "The bell has rung. Your night shift is next. Go to the door.");
        desktop.Notify($"Shift over · {clock.TimeText}", "The bell has rung. Your night shift is next. Leave your desk and go to the door.");
    }

    void ShowSummary(string heading, string message)
    {
        decided = true;
        if (checkHint != null)
            checkHint.text = "";
        fileContent.SetActive(false);
        approvedStamp.gameObject.SetActive(false);
        rejectedStamp.gameObject.SetActive(false);
        approveButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);

        tabLabel.text = "SHIFT REPORT";
        summaryText.gameObject.SetActive(true);
        summaryText.text = $"{heading}\n<size=55%>\nFILES REVIEWED  {archive.Count} / {cases.Count}\nERRORS  {errors}\nEARNINGS  {FormatMoney(Money)}</size>";
        feedbackText.text = message;
        feedbackText.color = errors == 0 ? UIFactory.Green : UIFactory.Red;
        UpdateStatus();
    }

    void UpdateStatus()
    {
        bool summary = allReviewed || clock.IsOver;
        caseStatus.text = summary
            ? $"{archive.Count} of {cases.Count} files reviewed"
            : $"Case {index + 1} of {cases.Count}";
        errorStatus.text = $"Errors: {errors}";
        errorStatus.color = errors == 0 ? OkColor : ErrorColor;
    }

    void RefreshArchive()
    {
        if (archive.Count == 0)
        {
            archiveText.text = "<color=#7A8594>No files archived yet.</color>";
            return;
        }

        var sb = new StringBuilder();
        foreach (var entry in archive)
        {
            string verdictColor = entry.verdict == "APPROVED" ? "#1E3A5F" : "#A8241C";
            string result = entry.correct ? "<color=#2E7D32>Correct</color>" : "<color=#C62828>Error</color>";
            sb.Append($"#{entry.caseNumber:0000}<pos=70>{entry.building}<pos=300><color={verdictColor}>{entry.verdict}</color><pos=430>{result}\n");
        }
        archiveText.text = sb.ToString();
    }

    void RefreshReport()
    {
        string status = clock.IsOver ? "<color=#C62828>Shift over</color>" : "<color=#2E7D32>On duty</color>";
        int correct = archive.Count - errors;
        reportText.text =
            $"<size=120%><b>Shift Report</b></size>\n<color=#5E6B7A>{clock.DateText} · Desk 4</color>\n\n" +
            $"Shift hours<pos=170>{clock.HoursText}\n" +
            $"Current time<pos=170>{clock.TimeText} ({clock.PeriodName})\n" +
            $"Status<pos=170>{status}\n\n" +
            $"Files reviewed<pos=170>{archive.Count} / {cases.Count}\n" +
            $"Correct<pos=170>{correct}\n" +
            $"Errors<pos=170>{errors}\n" +
            $"Earnings<pos=170>{FormatMoney(Money)}";
    }

    public static string FormatMoney(int amount) => (amount < 0 ? "-$" : "$") + Mathf.Abs(amount);

    public static string FormatDelta(int delta) => (delta >= 0 ? "+$" : "-$") + Mathf.Abs(delta);

    // Damga vuruşundan hemen sonra sonucun sesi: neşeli arpej ya da uyarı vızıltısı.
    static IEnumerator PlayResult(bool correct)
    {
        yield return new WaitForSeconds(0.18f);
        Sfx.Play(correct ? SfxId.Correct : SfxId.Wrong);
    }

    void Stamp(RectTransform stamp)
    {
        Sfx.Play(SfxId.Stamp);
        stamp.gameObject.SetActive(true);
        StartCoroutine(StampRoutine(stamp));
    }

    static IEnumerator StampRoutine(RectTransform stamp)
    {
        const float duration = 0.12f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            stamp.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, t / duration);
            yield return null;
        }
        stamp.localScale = Vector3.one;
    }
}

using System.Collections.Generic;
using UnityEngine;

// Oyuncuyu adım adım yönlendirir: tamamlanan görevler üstü çizili kalır, sıradaki görev vurgulanır.
// Kutu odada ekranın köşesinde, bilgisayardayken masaüstünde görünür.
public class TaskTracker : MonoBehaviour
{
    public struct Entry
    {
        public string text;
        public bool done;
    }

    public WakeUpSequence wakeUp;
    public ShiftClock clock;
    public InspectionDesk desk;
    public Win7Window caseWindow;
    public Win7Window ruleBookWindow;
    public DoorExit door;
    [Tooltip("Gece vardiyasına geçiş: duvardaki kriz monitörü (atanmışsa kapının yerine geçer).")]
    public CrisisMonitorExit monitor;
    [Tooltip("Mesai sonu ekip satın alma (atanmışsa monitörden önce görev olur).")]
    public FleetShop shop;
    [Tooltip("Oturunca açılan bilgisayar ekranı (ComputerPanel).")]
    public GameObject computerScreen;

    [Header("Views")]
    public TaskListView hudView;
    public TaskListView desktopView;

    readonly List<Entry> entries = new List<Entry>();
    bool caseOpened;
    bool ruleBookOpened;
    int doneCount = -1;
    bool? wasAtComputer;

    void Start()
    {
        caseWindow.Opened += () => caseOpened = true;
        ruleBookWindow.Opened += () => ruleBookOpened = true;
        desk.MoneyChanged += OnMoneyChanged;
    }

    void OnDestroy()
    {
        if (desk != null)
            desk.MoneyChanged -= OnMoneyChanged;
    }

    void Update()
    {
        bool atComputer = computerScreen != null && computerScreen.activeInHierarchy;
        bool over = clock.IsOver;
        int reviewed = desk.ReviewedCount, total = desk.CaseCount;

        entries.Clear();
        Add("Get up from the sofa", wakeUp == null || wakeUp.IsDone);
        Add("Go to the computer chair and press E", clock.HasStarted);
        Add("Open the Case Files folder", caseOpened || over);
        Add("Open the Rule Book folder to check the rules", ruleBookOpened || over);
        // Mesai saat 17:00'yi beklemez: son dosya incelenince biter
        Add(ZeroHour.Phase2.Calls.Loc.F("Review every case file ({0}/{1})", reviewed, total), reviewed >= total || over);
        if (shop != null)
            Add(atComputer ? "Buy your night crew in the NIGHT CREW window" : "Sit at the computer and buy your night crew", shop.Confirmed);
        if (monitor != null)
            Add(atComputer ? "Press E to stand up, then go to the crisis monitor on the wall" : "Look at the crisis monitor on the wall and press E",
                monitor.HasLeft);
        else
            Add(atComputer ? "Press E to stand up, then go to the door for your night shift" : "Walk to the door and press E to head to the night shift",
                door != null && door.HasLeft);

        // Sesler: görev tamamlandı, sandalyeye oturma / kalkma.
        int done = 0;
        foreach (var e in entries)
            if (e.done)
                done++;
        if (doneCount >= 0 && done > doneCount)
            Sfx.Play(SfxId.TaskDone);
        doneCount = done;

        if (wasAtComputer.HasValue && wasAtComputer.Value != atComputer)
            Sfx.Play(atComputer ? SfxId.ChairSit : SfxId.StandUp);
        wasAtComputer = atComputer;

        // Sadece tamamlananlar ve sıradaki görev gösterilir.
        int visible = entries.FindIndex(e => !e.done);
        if (visible >= 0)
            entries.RemoveRange(visible + 1, entries.Count - visible - 1);

        // Kutu taşmasın: tamamlananlardan yalnızca son birkaçı görünür
        const int MaxDone = 3;
        int doneShown = visible >= 0 ? visible : entries.Count;
        if (doneShown > MaxDone)
            entries.RemoveRange(0, doneShown - MaxDone);

        hudView.gameObject.SetActive(!atComputer);
        hudView.Render(entries);
        hudView.SetBalance(desk.Money);
        desktopView.Render(entries);
        desktopView.SetBalance(desk.Money);
    }

    // Metin İngilizce anahtar; seçili dilde gösterilir (ayarlardan dil değişince anında güncellenir)
    void Add(string text, bool done)
    {
        entries.Add(new Entry { text = ZeroHour.Phase2.Calls.Loc.T(text), done = done });
    }

    void OnMoneyChanged(int delta)
    {
        bool mistake = !desk.LastDecisionCorrect;
        hudView.ShowDelta(delta, mistake);
        desktopView.ShowDelta(delta, mistake);
    }
}

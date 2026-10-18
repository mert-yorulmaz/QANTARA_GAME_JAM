using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mesai sonu: bilgisayarda "NIGHT CREW" penceresi açılır. Oyuncu gündüz kazandığı parayla gece vardiyası ekibini kurar.
// Şehir her birimden 1 tane ücretsiz verir; fazlası satın alınır. Onaylanan ekip RunLoadout ile Faz 2'ye taşınır
// ve gece vardiyası bu araçlarla başlar. Ekip onaylanmadan duvardaki kriz monitörüne geçilemez.
public class FleetShop : MonoBehaviour
{
    [Serializable]
    public class Offer
    {
        public string name;
        public Sprite icon;
        [Tooltip("Ek birim fiyatı")]
        public int price = 100;
        [Tooltip("Şehrin ücretsiz verdiği adet")]
        public int baseCount = 1;
        [Tooltip("En fazla adet (Faz 2 filosu)")]
        public int maxCount = 3;

        [NonSerialized] public int count;
        [NonSerialized] public TMP_Text countText;
        [NonSerialized] public Button minus;
        [NonSerialized] public Button plus;
    }

    [Header("References")]
    public InspectionDesk desk;
    public BiaDesktop desktop;
    public ShiftClock clock;
    [Tooltip("Satın alma penceresi bu kâğıt pencerenin kopyasından kurulur (aynı görünüm).")]
    public Win7Window template;

    [Header("Units (Faz 2 sırası: polis, ambulans, itfaiye, helikopter)")]
    public Offer police = new Offer { name = "Police", price = 100, maxCount = 3 };
    public Offer ambulance = new Offer { name = "Ambulance", price = 100, maxCount = 3 };
    public Offer fireTruck = new Offer { name = "Fire Truck", price = 150, maxCount = 3 };
    public Offer helicopter = new Offer { name = "Helicopter", price = 200, maxCount = 2 };

    public bool Confirmed { get; private set; }
    public event Action CrewConfirmed;

    const float WindowWidth = 560f;
    const float WindowHeight = 480f;

    Offer[] offers;
    Win7Window window;
    TMP_Text budgetText;
    Button confirmButton;
    TMP_Text confirmLabel;
    TMP_FontAsset pixelFont;
    TMP_FontAsset bodyFont;
    TMP_FontAsset boldFont;

    int Budget => Mathf.Max(0, desk.Money);

    int Spent
    {
        get
        {
            int total = 0;
            foreach (var o in offers)
                total += (o.count - o.baseCount) * o.price;
            return total;
        }
    }

    void Awake()
    {
        // Yeni tur: önceki turun ekibi Faz 2'ye taşınmasın
        ZeroHour.Shared.RunLoadout.Clear();
        offers = new[] { police, ambulance, fireTruck, helicopter };
        foreach (var o in offers)
            o.count = o.baseCount;
    }

    void Start()
    {
        BuildWindow();
        clock.ShiftEnded += OpenShop;
        if (clock.IsOver)
            OpenShop();
    }

    void OnDestroy()
    {
        if (clock != null)
            clock.ShiftEnded -= OpenShop;
    }

    void OpenShop()
    {
        Refresh();
        window.Open();
    }

    // ---------------- Pencere ----------------

    void BuildWindow()
    {
        var root = Instantiate(template.gameObject, template.transform.parent);
        root.name = "NightCrewWindow";
        window = root.GetComponent<Win7Window>();
        window.title = "Night Crew";

        var rt = (RectTransform)root.transform;
        rt.localScale = Vector3.one;
        rt.sizeDelta = new Vector2(WindowWidth, WindowHeight);
        rt.anchoredPosition = new Vector2(330f, -40f);
        Resize(root.transform, "Paper", WindowWidth, WindowHeight);
        Resize(root.transform, "Rule", WindowWidth - 36f, -1f);
        Resize(root.transform, "Heading", WindowWidth - 150f, -1f);
        MoveX(root.transform, "Close", WindowWidth - 50f);
        MoveX(root.transform, "Minimize", WindowWidth - 88f);

        var heading = root.transform.Find("Heading").GetComponent<TMP_Text>();
        heading.text = "NIGHT CREW";
        pixelFont = heading.font;
        bodyFont = desk.notesText != null ? desk.notesText.font : pixelFont;
        boldFont = desk.buildingText != null ? desk.buildingText.font : bodyFont;

        var client = (RectTransform)root.transform.Find("Client");
        for (int i = client.childCount - 1; i >= 0; i--)
            Destroy(client.GetChild(i).gameObject);
        client.sizeDelta = new Vector2(WindowWidth - 40f, WindowHeight - 70f);
        BuildContent(client);

        // Görev çubuğu düğmesi: şablon pencerenin düğmesinin kopyası
        int templateIndex = Array.IndexOf(desktop.windows, template);
        var buttonCopy = Instantiate(desktop.taskButtons[templateIndex].gameObject, desktop.taskButtons[templateIndex].transform.parent);
        buttonCopy.name = "Task NightCrew";
        var taskButton = buttonCopy.GetComponent<TaskbarButton>();
        if (taskButton.label != null)
            taskButton.label.text = "Night Crew";
        desktop.AddWindow(window, taskButton);

        // Ekip onaylanmadan pencere kapatılamaz (kaybolmasın); küçültülebilir
        window.closeButton.gameObject.SetActive(false);
    }

    void BuildContent(RectTransform client)
    {
        float w = WindowWidth - 40f;
        UIFactory.Text("Intro", client, bodyFont, 16f, UIFactory.Ink,
            "Your shift earnings pay for tonight's crew. The city gives you <b>one of each unit for free</b>; buy more if you can afford them.",
            0f, 0f, w, 44f, TextAlignmentOptions.TopLeft, true).richText = true;

        UIFactory.Text("HeadUnit", client, pixelFont, 15f, UIFactory.Label, "UNIT", 0f, 56f, 200f, 20f, TextAlignmentOptions.Left);
        UIFactory.Text("HeadPrice", client, pixelFont, 15f, UIFactory.Label, "EXTRA", 230f, 56f, 100f, 20f, TextAlignmentOptions.Left);
        UIFactory.Text("HeadCrew", client, pixelFont, 15f, UIFactory.Label, "CREW", 360f, 56f, 160f, 20f, TextAlignmentOptions.Center);
        UIFactory.DashedLine("HeadLine", client, UIFactory.Border, 0f, 78f, w);

        for (int i = 0; i < offers.Length; i++)
            BuildRow(client, offers[i], 86f + i * 58f, w);

        UIFactory.DashedLine("BudgetLine", client, UIFactory.Border, 0f, 322f, w);
        budgetText = UIFactory.Text("Budget", client, boldFont, 17f, UIFactory.Ink, "", 0f, 330f, w, 26f, TextAlignmentOptions.Left);
        budgetText.richText = true;

        confirmButton = FlatButton("Confirm", client, w - 220f, 366f, 220f, 40f, UIFactory.Navy);
        confirmLabel = UIFactory.Text("Label", confirmButton.transform, pixelFont, 18f, Color.white, "CONFIRM CREW", 0f, 0f, 220f, 40f, TextAlignmentOptions.Center);
        confirmButton.onClick.AddListener(Confirm);
    }

    void BuildRow(RectTransform client, Offer offer, float y, float w)
    {
        if (offer.icon != null)
        {
            var icon = UIFactory.Image("Icon " + offer.name, client, offer.icon, 0f, y + 2f, 40f, 40f);
            icon.preserveAspect = true;
        }
        UIFactory.Text("Name " + offer.name, client, boldFont, 19f, UIFactory.Ink, offer.name, 52f, y + 2f, 170f, 24f, TextAlignmentOptions.Left);
        UIFactory.Text("Free " + offer.name, client, bodyFont, 14f, UIFactory.Label, $"{offer.baseCount} free · max {offer.maxCount}", 52f, y + 24f, 170f, 20f, TextAlignmentOptions.Left);
        UIFactory.Text("Price " + offer.name, client, boldFont, 19f, UIFactory.Ink, $"${offer.price}", 230f, y + 10f, 100f, 24f, TextAlignmentOptions.Left);

        offer.minus = FlatButton("Minus " + offer.name, client, 372f, y + 4f, 38f, 38f, UIFactory.Label);
        UIFactory.Text("Label", offer.minus.transform, pixelFont, 22f, Color.white, "-", 0f, 0f, 38f, 38f, TextAlignmentOptions.Center);
        offer.countText = UIFactory.Text("Count " + offer.name, client, pixelFont, 24f, UIFactory.Ink, "1", 414f, y + 4f, 52f, 38f, TextAlignmentOptions.Center);
        offer.plus = FlatButton("Plus " + offer.name, client, 470f, y + 4f, 38f, 38f, UIFactory.Navy);
        UIFactory.Text("Label", offer.plus.transform, pixelFont, 22f, Color.white, "+", 0f, 0f, 38f, 38f, TextAlignmentOptions.Center);

        offer.minus.onClick.AddListener(() => Change(offer, -1));
        offer.plus.onClick.AddListener(() => Change(offer, +1));

        if (offer != helicopter)
            UIFactory.DashedLine("Line " + offer.name, client, new Color(UIFactory.Border.r, UIFactory.Border.g, UIFactory.Border.b, 0.5f), 0f, y + 50f, w);
    }

    static Button FlatButton(string name, Transform parent, float x, float y, float w, float h, Color color)
    {
        var button = UIFactory.Button(name, parent, null, x, y, w, h);
        ((Image)button.targetGraphic).color = color;
        var colors = button.colors;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
        button.colors = colors;
        return button;
    }

    static void Resize(Transform root, string child, float width, float height)
    {
        var t = root.Find(child) as RectTransform;
        if (t == null) return;
        t.sizeDelta = new Vector2(width, height < 0f ? t.sizeDelta.y : height);
    }

    static void MoveX(Transform root, string child, float x)
    {
        var t = root.Find(child) as RectTransform;
        if (t == null) return;
        t.anchoredPosition = new Vector2(x, t.anchoredPosition.y);
    }

    // ---------------- Satın alma ----------------

    void Change(Offer offer, int step)
    {
        if (Confirmed)
            return;

        int next = offer.count + step;
        if (next < offer.baseCount || next > offer.maxCount)
            return;
        if (step > 0 && Spent + offer.price > Budget)
            return;

        offer.count = next; // Tık sesi masaüstünden (BiaDesktop) gelir
        Refresh();
    }

    void Refresh()
    {
        if (budgetText == null)
            return;

        int left = Budget - Spent;
        foreach (var o in offers)
        {
            o.countText.text = o.count.ToString();
            o.minus.interactable = !Confirmed && o.count > o.baseCount;
            o.plus.interactable = !Confirmed && o.count < o.maxCount && left >= o.price;
        }

        string earned = desk.Money < 0 ? $"<color=#A8241C>{InspectionDesk.FormatMoney(desk.Money)}</color> (counts as $0)" : InspectionDesk.FormatMoney(desk.Money);
        budgetText.text = $"Earnings {earned}   ·   Spent ${Spent}   ·   <color=#1E3A5F>Left ${left}</color>";
        confirmButton.interactable = !Confirmed;
        confirmLabel.text = Confirmed ? "CREW READY" : "CONFIRM CREW";
    }

    void Confirm()
    {
        if (Confirmed)
            return;

        Confirmed = true;
        ZeroHour.Shared.RunLoadout.Set(police.count, ambulance.count, fireTruck.count, helicopter.count);
        Sfx.Play(SfxId.Stamp);
        Refresh();
        ((Image)confirmButton.targetGraphic).color = UIFactory.Green;
        var colors = confirmButton.colors;
        colors.disabledColor = Color.white; // Onaylanınca soluk değil, yeşil "CREW READY" görünsün
        confirmButton.colors = colors;
        window.closeButton.gameObject.SetActive(true);

        desktop.Notify("Night crew ready",
            $"{police.count} police · {ambulance.count} ambulance · {fireTruck.count} fire truck · {helicopter.count} helicopter. Go to the crisis monitor on the wall.");
        CrewConfirmed?.Invoke();
    }
}

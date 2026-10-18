using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Kural kitabı (Regulations Handbook): 5 sayfa, sağda bölüm sekmeleri.
public class RuleBook : MonoBehaviour
{
    [Serializable]
    public class Rule
    {
        public string badge;
        public string title;
        [TextArea(2, 4)] public string body;
        public bool structural;
        public bool rejectTag = true;
    }

    [Serializable]
    public class Page
    {
        public string chapter;
        public string title;
        public List<Rule> rules = new List<Rule>();
    }

    public List<Page> pages = new List<Page>();

    [Tooltip("Kitabın açıldığı masaüstü penceresi.")]
    public Win7Window window;

    [Header("UI")]
    public TMP_Text chapterLabel;
    public TMP_Text chapterTitle;
    public TMP_Text pageIndicator;
    public RectTransform entriesRoot;
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;
    public Button[] tabs;
    public TMP_Text[] tabLabels;
    public Sprite[] tabActive;
    public Sprite[] tabInactive;

    [Header("Entry parts")]
    public Sprite badgeNavy;
    public Sprite badgeRed;
    public Sprite verdictTag;
    public TMP_FontAsset pixelFont;
    public TMP_FontAsset bodyFont;
    public TMP_FontAsset boldFont;

    const float EntryWidth = 710f;
    const float TextX = 60f;

    int page;

    public bool IsOpen => window.IsVisible;

    public void Init()
    {
        prevButton.onClick.AddListener(() => Show(page - 1));
        nextButton.onClick.AddListener(() => Show(page + 1));
        closeButton.onClick.AddListener(Close);
        window.Opened += () => Show(page);

        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i].onClick.AddListener(() => Show(index));
        }
    }

    public void Open()
    {
        window.Open();
    }

    public void Close()
    {
        window.Close();
    }

    public void Show(int index)
    {
        if (pages.Count == 0)
            return;

        int previous = page;
        page = Mathf.Clamp(index, 0, pages.Count - 1);
        if (page != previous && window.IsVisible)
            Sfx.Play(SfxId.PageFlip);
        var current = pages[page];

        chapterLabel.text = current.chapter;
        chapterTitle.text = current.title;
        pageIndicator.text = $"PAGE {page + 1} / {pages.Count}";

        SetNav(prevButton, page > 0);
        SetNav(nextButton, page < pages.Count - 1);

        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = i == page;
            ((Image)tabs[i].targetGraphic).sprite = active ? tabActive[i] : tabInactive[i];
            tabLabels[i].color = active ? UIFactory.Ink : UIFactory.Paper;
        }

        BuildEntries(current);
    }

    static void SetNav(Button button, bool enabled)
    {
        button.interactable = enabled;
        foreach (var graphic in button.GetComponentsInChildren<Graphic>())
        {
            var c = graphic.color;
            c.a = enabled ? 1f : 0.4f;
            graphic.color = c;
        }
    }

    void BuildEntries(Page current)
    {
        for (int i = entriesRoot.childCount - 1; i >= 0; i--)
            Destroy(entriesRoot.GetChild(i).gameObject);

        // Okunurluk: büyük ve kalın yazı; önemli ifadeler kural metninde <b>/<color> ile vurgulanır.
        // (Eskiden her kuralın altında duran "REJECT" etiketi kaldırıldı.)
        float y = 0f;
        foreach (var rule in current.rules)
        {
            var badge = UIFactory.Image("Badge " + rule.badge, entriesRoot, rule.structural ? badgeRed : badgeNavy, 0f, y, 52f, 32f);
            UIFactory.Text("Number", badge.transform, pixelFont, 19f, Color.white, rule.badge,
                0f, 0f, 52f, 32f, TextAlignmentOptions.Center);

            UIFactory.Text("Title", entriesRoot, boldFont, 23f, UIFactory.Ink, rule.title,
                TextX, y, EntryWidth - TextX, 32f, TextAlignmentOptions.Left);

            var body = UIFactory.Text("Body", entriesRoot, boldFont != null ? boldFont : bodyFont, 19f, UIFactory.Ink, rule.body,
                TextX, y + 35f, EntryWidth - TextX, 40f, TextAlignmentOptions.TopLeft, true);
            body.richText = true;
            float bodyHeight = body.GetPreferredValues(rule.body, EntryWidth - TextX, 0f).y;
            body.rectTransform.sizeDelta = new Vector2(EntryWidth - TextX, bodyHeight);
            y += 35f + bodyHeight;

            y += 10f;
            UIFactory.DashedLine("Separator", entriesRoot, UIFactory.Border, 0f, y, EntryWidth);
            y += 11f;
        }
    }

    public static List<Page> DefaultPages(string outdatedBefore)
    {
        return new List<Page>
        {
            new Page
            {
                chapter = "INTRODUCTION",
                title = "How to Inspect a File",
                rules =
                {
                    new Rule { badge = "A", title = "Review", rejectTag = false,
                        body = "Compare what the owner declared with the rules in this handbook. <b><color=#A8241C>The owner's notes never override a rule.</color></b>" },
                    new Rule { badge = "B", title = "Scan", rejectTag = false,
                        body = "Press <b><color=#1E3A5F>X-RAY SCAN</color></b> to see the floor plan. <b><color=#A8241C>Red marks</color></b> show structural damage. <b><color=#1E3A5F>Yellow FE marks</color></b> show fire extinguishers." },
                    new Rule { badge = "C", title = "Decide", rejectTag = false,
                        body = "Approve <b>only</b> if the file breaks <b>no rule</b>. To reject, mark <b><color=#A8241C>every rule</color></b> the file breaks on the rejection slip." }
                }
            },
            new Page
            {
                chapter = "CHAPTER 1",
                title = "Documentation",
                rules =
                {
                    new Rule { badge = "1.1", title = "Report Date",
                        body = $"Inspection files filed <b><color=#A8241C>before {outdatedBefore}</color></b> are outdated. An outdated report <b>cannot be approved</b>." },
                    new Rule { badge = "1.2", title = "Seal and Signature",
                        body = "Every file must be <b><color=#1E3A5F>signed by the owner</color></b> and carry the <b><color=#1E3A5F>official agency seal</color></b>. If either one is missing, the file is <b><color=#A8241C>invalid</color></b>." }
                }
            },
            new Page
            {
                chapter = "CHAPTER 2",
                title = "Structural Integrity (No Exceptions)",
                rules =
                {
                    new Rule { badge = "2.1", title = "Load-Bearing System", structural = true,
                        body = "If the report mentions <b><color=#A8241C>physical damage to columns or beams</color></b>, such as cuts, large cracks, thinning or deterioration, the building is <b><color=#A8241C>marked for demolition</color></b> regardless of its age." },
                    new Rule { badge = "2.2", title = "Commercial Ground Floors", structural = true,
                        body = "Buildings with a <b>business on the ground floor</b> (shop, market, car dealer, warehouse) must include a <b><color=#1E3A5F>Load-Bearing Wall Supplement</color></b>. If it is missing, the file <b><color=#A8241C>cannot be approved</color></b>." }
                }
            },
            new Page
            {
                chapter = "CHAPTER 3",
                title = "Life Safety",
                rules =
                {
                    new Rule { badge = "3.1", title = "Fire Extinguisher",
                        body = "The X-RAY floor plan must show <b><color=#1E3A5F>at least one fire extinguisher</color></b> (yellow FE mark)." },
                    new Rule { badge = "3.2", title = "Emergency Exits",
                        body = "All emergency exits must be <b><color=#1E3A5F>open and accessible</color></b>. <b><color=#A8241C>Locked or blocked exits</color></b> are grounds for rejection." },
                    new Rule { badge = "3.3", title = "Emergency Bag",
                        body = "The emergency bag must contain <b><color=#1E3A5F>water, a flashlight, a first aid kit and a whistle</color></b>. If <b><color=#A8241C>any item is missing</color></b>, the bag is incomplete." }
                }
            },
            new Page
            {
                chapter = "CHAPTER 4",
                title = "Integrity of the Inspector",
                rules =
                {
                    new Rule { badge = "4.1", title = "Bribery",
                        body = "<b><color=#A8241C>Money, gifts or envelopes</color></b> attached to a file are bribes. Reject the file and cite this rule. <b><color=#1E3A5F>Letters from architects or engineers are not bribes.</color></b>" }
                }
            }
        };
    }
}

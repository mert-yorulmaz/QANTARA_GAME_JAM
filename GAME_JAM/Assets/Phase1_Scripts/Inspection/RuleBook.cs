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

        float y = 0f;
        foreach (var rule in current.rules)
        {
            var badge = UIFactory.Image("Badge " + rule.badge, entriesRoot, rule.structural ? badgeRed : badgeNavy, 0f, y, 46f, 28f);
            UIFactory.Text("Number", badge.transform, pixelFont, 16f, Color.white, rule.badge,
                0f, 0f, 46f, 28f, TextAlignmentOptions.Center);

            UIFactory.Text("Title", entriesRoot, boldFont, 18f, UIFactory.Ink, rule.title,
                TextX, y, EntryWidth - TextX, 28f, TextAlignmentOptions.Left);

            var body = UIFactory.Text("Body", entriesRoot, bodyFont, 16f, UIFactory.Ink, rule.body,
                TextX, y + 27f, EntryWidth - TextX, 40f, TextAlignmentOptions.TopLeft, true);
            float bodyHeight = body.GetPreferredValues(rule.body, EntryWidth - TextX, 0f).y;
            body.rectTransform.sizeDelta = new Vector2(EntryWidth - TextX, bodyHeight);
            y += 27f + bodyHeight + 6f;

            if (rule.rejectTag)
            {
                var tag = UIFactory.Image("Verdict", entriesRoot, verdictTag, TextX, y, 102f, 22f);
                UIFactory.Text("Label", tag.transform, pixelFont, 16f, UIFactory.Red, "REJECT",
                    0f, 0f, 102f, 22f, TextAlignmentOptions.Center);
                y += 22f;
            }

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
                        body = "Compare what the owner declared with the rules in this handbook. The owner's notes never override a rule." },
                    new Rule { badge = "B", title = "Scan", rejectTag = false,
                        body = "Press X-RAY SCAN to see the floor plan. Red marks show structural damage. Yellow FE marks show fire extinguishers." },
                    new Rule { badge = "C", title = "Decide", rejectTag = false,
                        body = "Approve only if the file breaks no rule. To reject, mark every rule the file breaks on the rejection slip." }
                }
            },
            new Page
            {
                chapter = "CHAPTER 1",
                title = "Documentation",
                rules =
                {
                    new Rule { badge = "1.1", title = "Report Date",
                        body = $"Inspection files filed before {outdatedBefore} are outdated. An outdated report cannot be approved." },
                    new Rule { badge = "1.2", title = "Seal and Signature",
                        body = "Every file must be signed by the owner and carry the official agency seal. If either one is missing, the file is invalid." }
                }
            },
            new Page
            {
                chapter = "CHAPTER 2",
                title = "Structural Integrity (No Exceptions)",
                rules =
                {
                    new Rule { badge = "2.1", title = "Load-Bearing System", structural = true,
                        body = "If the report mentions physical damage to columns or beams, such as cuts, large cracks, thinning or deterioration, the building is marked for demolition regardless of its age." },
                    new Rule { badge = "2.2", title = "Commercial Ground Floors", structural = true,
                        body = "Buildings with a business on the ground floor (shop, market, car dealer, warehouse) must include a Load-Bearing Wall Supplement. If it is missing, the file cannot be approved." }
                }
            },
            new Page
            {
                chapter = "CHAPTER 3",
                title = "Life Safety",
                rules =
                {
                    new Rule { badge = "3.1", title = "Fire Extinguisher",
                        body = "The X-RAY floor plan must show at least one fire extinguisher (yellow FE mark)." },
                    new Rule { badge = "3.2", title = "Emergency Exits",
                        body = "All emergency exits must be open and accessible. Locked or blocked exits are grounds for rejection." },
                    new Rule { badge = "3.3", title = "Emergency Bag",
                        body = "The emergency bag must contain water, a flashlight, a first aid kit and a whistle. If any item is missing, the bag is incomplete." }
                }
            },
            new Page
            {
                chapter = "CHAPTER 4",
                title = "Integrity of the Inspector",
                rules =
                {
                    new Rule { badge = "4.1", title = "Bribery",
                        body = "Money, gifts or envelopes attached to a file are bribes. Reject the file and cite this rule. Letters from architects or engineers are not bribes." }
                }
            }
        };
    }
}

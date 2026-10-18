using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Görev kutusu: bakiye, para değişimi ve madde madde görevler.
public class TaskListView : MonoBehaviour
{
    public RectTransform background;
    public RectTransform rowsRoot;
    public TMP_Text balanceText;
    public TMP_Text deltaText;
    public Sprite checkEmpty;
    public Sprite checkDone;
    public TMP_FontAsset font;

    [Header("Layout")]
    public float fontSize = 18f;
    public float rowSpacing = 8f;
    [Tooltip("Birden fazla satıra kayan görevlerin altına eklenen boşluk (font boyutunun katı).")]
    public float wrappedExtraSpacing = 0.35f;
    public float bottomPadding = 14f;

    static readonly Color DoneColor = UIFactory.Hex("#8A9AA8");
    static readonly Color CurrentColor = UIFactory.Hex("#FFE08A");
    static readonly Color PositiveColor = UIFactory.Hex("#7FE0B5");
    static readonly Color NegativeColor = UIFactory.Hex("#FF8A80");

    string signature;
    int shownMoney = int.MinValue;
    Coroutine deltaRoutine;

    public void Render(List<TaskTracker.Entry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
            sb.Append(e.text).Append(e.done ? '1' : '0').Append('|');
        if (sb.ToString() == signature)
            return;
        signature = sb.ToString();

        for (int i = rowsRoot.childCount - 1; i >= 0; i--)
            Destroy(rowsRoot.GetChild(i).gameObject);

        float width = rowsRoot.rect.width;
        float textX = fontSize + 10f;
        float y = 0f;
        foreach (var e in entries)
        {
            string text = e.done ? $"<s>{e.text}</s>" : e.text;
            var label = UIFactory.Text("Task", rowsRoot, font, fontSize, e.done ? DoneColor : CurrentColor, text,
                textX, y, width - textX, fontSize * 1.4f, TextAlignmentOptions.TopLeft, true);
            float height = Mathf.Max(fontSize * 1.3f, label.GetPreferredValues(text, width - textX, 0f).y);
            label.rectTransform.sizeDelta = new Vector2(width - textX, height);

            UIFactory.Image("Check", rowsRoot, e.done ? checkDone : checkEmpty, 0f, y + 2f, fontSize, fontSize);
            bool wrapped = height > fontSize * 1.6f;
            y += height + rowSpacing + (wrapped ? fontSize * wrappedExtraSpacing : 0f);
        }

        // Kutu, görev sayısına göre uzar.
        float top = -rowsRoot.anchoredPosition.y;
        background.sizeDelta = new Vector2(background.sizeDelta.x, top + y - rowSpacing + bottomPadding);
    }

    public void SetBalance(int money)
    {
        if (money == shownMoney)
            return;

        shownMoney = money;
        balanceText.text = InspectionDesk.FormatMoney(money);
        balanceText.color = money < 0 ? NegativeColor : Color.white;
    }

    public void ShowDelta(int delta)
    {
        if (!isActiveAndEnabled)
            return;

        if (deltaRoutine != null)
            StopCoroutine(deltaRoutine);
        deltaRoutine = StartCoroutine(DeltaRoutine(delta));
    }

    IEnumerator DeltaRoutine(int delta)
    {
        var rt = deltaText.rectTransform;
        Vector2 start = rt.anchoredPosition;
        Color color = delta >= 0 ? PositiveColor : NegativeColor;
        deltaText.text = InspectionDesk.FormatDelta(delta);
        deltaText.gameObject.SetActive(true);

        const float duration = 1.6f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            rt.anchoredPosition = start + new Vector2(0f, 14f * k);
            color.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            deltaText.color = color;
            yield return null;
        }

        rt.anchoredPosition = start;
        deltaText.gameObject.SetActive(false);
        deltaRoutine = null;
    }
}

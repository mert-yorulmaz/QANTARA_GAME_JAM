using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ret formu: dosyanın çiğnediği kurallar seçilip onaylanır.
public class RejectionSlip : MonoBehaviour
{
    [Header("Chips (InspectionRules.All sırasıyla)")]
    public Button[] chips;
    public TMP_Text[] chipNumbers;
    public TMP_Text[] chipLabels;
    public Sprite chipNormal;
    public Sprite chipSelected;

    [Header("Footer")]
    public Button cancelButton;
    public Button ruleBookLink;
    public Button confirmButton;
    public TMP_Text confirmLabel;
    public TMP_Text citingText;
    public Sprite confirmDisabled;
    public Sprite confirmEnabled;

    public event Action<RuleViolation> Confirmed;
    public event Action Cancelled;
    public event Action RuleBookRequested;

    RuleViolation selected;

    public void Init()
    {
        for (int i = 0; i < chips.Length; i++)
        {
            int index = i;
            chips[i].onClick.AddListener(() => Toggle(index));
        }

        cancelButton.onClick.AddListener(() => Cancelled?.Invoke());
        ruleBookLink.onClick.AddListener(() => RuleBookRequested?.Invoke());
        confirmButton.onClick.AddListener(() => Confirmed?.Invoke(selected));
    }

    public void Open()
    {
        selected = RuleViolation.None;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    void Toggle(int index)
    {
        selected ^= InspectionRules.All[index];
        Sfx.Play(SfxId.Tick);
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < chips.Length; i++)
        {
            bool on = (selected & InspectionRules.All[i]) != 0;
            ((Image)chips[i].targetGraphic).sprite = on ? chipSelected : chipNormal;
            chipNumbers[i].color = on ? Color.white : UIFactory.Ink;
            chipLabels[i].color = on ? Color.white : UIFactory.Ink;
        }

        bool any = selected != RuleViolation.None;
        confirmButton.interactable = any;
        ((Image)confirmButton.targetGraphic).sprite = any ? confirmEnabled : confirmDisabled;
        confirmLabel.color = any ? Color.white : new Color(1f, 1f, 1f, 0.75f);
        citingText.text = any ? "Citing " + InspectionRules.CodeList(selected) : "";
    }
}

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 사용자의 입력과 상호작용을 시각적으로 처리하는 UI 컴포넌트
/// 버튼 클릭, 텍스트 표시 등 View 역할을 담당
/// 내부 로직은 DeckController에 위임
/// </summary>
public class DeckSelectUI : BaseUI
{
    [SerializeField] private TextMeshProUGUI deckButtonText;
    [SerializeField] private Button deckButton;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.gray;
    [SerializeField] private Color disabledColor = Color.gray;
    [SerializeField] private GameObject buttonLockedObject;
    [SerializeField] private List<Image> childImages;
    
    [SerializeField] private TooltipTrigger tooltipTrigger;

    public Button DeckButton => deckButton;
    public delegate void DeckSelectHandler();
    public DeckSelectHandler onDeckSelected;

    private void Start()
    {
        deckButton.onClick.AddListener(() => onDeckSelected?.Invoke());
        //deckButton.interactable = true;
    }

    public void SelectedUI(bool isSelected)
    {
        if (deckButton == null) return;
        //EventSystem.current?.SetSelectedGameObject(isSelected ? deckButton.gameObject : null);
        deckButton.image.color = isSelected ? selectedColor : normalColor;
        foreach (Image image in childImages)
        {
            image.color = isSelected ? selectedColor : normalColor;
        }
    }

    public void DisabledUI(bool isSelectable)
    {
        deckButton.interactable = isSelectable;
        foreach (Image image in childImages)
        {
            image.color = isSelectable ? normalColor : disabledColor;
        }
    }

    public void UpdateTexts(DeckData deckData)
    {
        deckButtonText.text = LocalizationSettings.StringDatabase.GetLocalizedString("DeckName", deckData.DeckName);
    }
    
    protected override void OnDisable()
    {
        EventSystem.current?.SetSelectedGameObject(null);
        base.OnDisable();
    }

    public void LockedButton(bool isLocked)
    {
        buttonLockedObject.SetActive(isLocked);
    }

    public void SetTooltip(string description) => tooltipTrigger?.SetData(null, description);
    public void SetTooltip(Func<string> description) => tooltipTrigger?.SetData(null, description);
    public void ClearTooltip() => tooltipTrigger?.ClearData();
}

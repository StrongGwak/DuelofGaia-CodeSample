using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class StageTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI unlockConditionText;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float paddingX = 2f;
    [SerializeField] private float paddingY = 2f;
    [SerializeField] private float spacing = 0f;
    [SerializeField] private float textPositionY = -10f;
    [SerializeField] private ThemeColorTable colorTable;
    
    private void Awake()
    {
        canvasGroup.alpha = 0;
    }
    
    private void Start()
    {
        StageManager.Instance.onStageHovered += Show;
        StageManager.Instance.onStageUnhovered += Hide;
    }
    
    private void Show(StageSelector stage, Vector3 position)
    {
        titleText.text = LocalizationSettings.StringDatabase.GetLocalizedString("StageName", stage.SceneType.ToString()).Replace("\n", " ");
        unlockConditionText.text = stage.IsDemoLocked
            ? LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Demo")
            : GetUnlockConditions(stage.StageUnlockCondition);
        
        titleText.ForceMeshUpdate();
        unlockConditionText.ForceMeshUpdate();

        float width  = Mathf.Max(titleText.renderedWidth, unlockConditionText.renderedWidth)
                       + paddingX * 2f;
        float height = paddingY + titleText.renderedHeight
                                         + spacing + unlockConditionText.renderedHeight + paddingY;

        rectTransform.sizeDelta = new Vector2(width, height);
        titleText.rectTransform.sizeDelta = new Vector2(width, titleText.renderedHeight);
        unlockConditionText.rectTransform.sizeDelta = new Vector2(width, unlockConditionText.renderedHeight);
                                                                                                                                                                                                                               
        float pivotX = position.x < Screen.width  * 0.5f ? 0f : 1f;
        float pivotY = position.y < Screen.height * 0.5f ? 0f : 1f;
        rectTransform.pivot = new Vector2(pivotX, pivotY);
        rectTransform.position = position;

        float textX = pivotX < 0.5f ? width / 2f : -width / 2f;

        if (pivotY > 0.5f)
        {
            titleText.rectTransform.localPosition = new Vector3(textX, -(paddingY + titleText.renderedHeight / 2f), 0);
            unlockConditionText.rectTransform.localPosition = new Vector3(textX, -(paddingY + titleText.renderedHeight + spacing + unlockConditionText.renderedHeight / 2f), 0);
        }
        else
        {
            titleText.rectTransform.localPosition = new Vector3(textX, height - paddingY - titleText.renderedHeight / 2f, 0);
            unlockConditionText.rectTransform.localPosition = new Vector3(textX, height - paddingY - titleText.renderedHeight - spacing - unlockConditionText.renderedHeight / 2f, 0);
        }

        canvasGroup.alpha = 1;
        
        //test
        titleText.rectTransform.localPosition -= new Vector3(0, textPositionY, 0);
        unlockConditionText.rectTransform.localPosition -= new Vector3(0, textPositionY, 0);
    }
    
    

    private string GetUnlockConditions(List<Define.Scene> unlockList)
    {
        var parts = new List<string>();
        foreach (Define.Scene unlock in unlockList)
        {
            string isClear = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Conquest")
                             + (SaveManager.Game.stageCleared.Get(unlock.ToString())
                                 ? LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Complete")
                                 : LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Incomplete"));
            parts.Add(LocalizationSettings.StringDatabase
                .GetLocalizedString("StageName", unlock.ToString())
                .Replace("\n", " ") + isClear);
        }
        return string.Join("\n", parts);
    }
    
    private void Hide()
    {
        canvasGroup.alpha = 0;
    }

    private void OnDestroy()
    {
        StageManager.Instance.onStageHovered -= Show;
        StageManager.Instance.onStageUnhovered -= Hide;
    }
}

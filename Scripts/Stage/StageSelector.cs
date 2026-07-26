using System.Collections.Generic;
using UnityEngine;

public class StageSelector : MonoBehaviour
{
    [SerializeField] private Define.Scene sceneType;
    [SerializeField] private bool isDefaultUnlocked;
    [SerializeField] private bool isDemoLocked;
    [SerializeField] private List<DeckData> deckDatas;
    [SerializeField] private List<Define.Scene> stageUnlockCondition = new();
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private GameObject advancedVFX;
    [SerializeField] private StageLightController lightController;
    [SerializeField] private PropAnimator propAnimator;
    
    private bool isSelect;
    private Vector3 defaultPosition;
    private bool isUnlocked;
    private Renderer[] renderers;

    public Define.Scene SceneType => sceneType;
    public bool IsDemoLocked => isDemoLocked;
    public List<DeckData> DeckDatas => deckDatas;
    public Transform CameraPosition => cameraPosition;
    public bool IsUnlocked => isUnlocked;
    public bool IsCleared => SaveManager.Game.stageCleared.Get(sceneType.ToString());
    public List<Define.Scene> StageUnlockCondition => stageUnlockCondition;

    private void Awake()
    {
        defaultPosition = transform.position;
        renderers = GetComponentsInChildren<Renderer>();
        if (isDemoLocked) return;
        if (stageUnlockCondition.Count == 0 || SaveManager.Game.stageCleared.Get(sceneType.ToString()) || isDefaultUnlocked)
        {
            isUnlocked = true;  // 듀얼 스테이지는 기본 해금
        }
        else
        {
            foreach (var condition in stageUnlockCondition)
            {
                // 클리어 안 된 링크 스테이지가 하나라도 있으면 잠금 유지
                if (!SaveManager.Game.stageCleared.Get(condition.ToString())) return;
            }
            // 모두 클리어됐을 때만 해금
            isUnlocked = true;
        }
    }

    private void Start()
    {
        if (!isUnlocked) SetLockedVisual();
    }
    
    private void SetLockedVisual()
    {
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f));
        block.SetColor("_Color", new Color(0.3f, 0.3f, 0.3f));
        block.SetColor("_TintColor", new Color(0.3f, 0.3f, 0.3f));
        foreach (var r in renderers)
        {
            for (int i = 0; i < r.sharedMaterials.Length; i++)
                r.SetPropertyBlock(block, i);  // 인덱스 없이 하면 첫 번째만 적용
        }
    }
    
    public void SelectStage()
    {
        if (isSelect || !isUnlocked)
        {
            return;
        }

        isSelect = true;
        AudioManager.Instance.PlayUISound(UISoundType.StageSelect);
        propAnimator?.PlaySelected(isSelect);
    }

    public void DeSelect()
    {
        if (isSelect)
        {
            isSelect = false;
        }
        AudioManager.Instance.PlayUISound(UISoundType.StageDeSelect);
        propAnimator?.PlaySelected(isSelect);
    }
    
    public void OnAdvancedVFX(bool changed)
    {
        if (advancedVFX != null)
            advancedVFX.SetActive(changed);
        lightController.SetAdvancedMode(changed);
        if (changed) lightController.SetLightPosition(advancedVFX.transform.position);
    }

    public void Move(bool selected)
    {
        if (selected)
        {
            Vector3 targetPos = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y + 1.5f, gameObject.transform.position.z);
            LeanTween.move(gameObject, targetPos, 1.5f);
        }
        else
        {
            LeanTween.move(gameObject, defaultPosition, 1.5f);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 덱 버튼 UI에서 발생한 이벤트를 처리하고 게임 로직을 실행하는 Controller
/// UI와 게임 시스템 사이의 중재자 역할
/// </summary>
public class DeckController : MonoBehaviour
{
    [SerializeField] private DeckData deckData;
    [SerializeField] private DeckSelectUI deckSelectUI;
    [SerializeField] private bool isDemoLocked;
    
    private StageDeckSelect stageDeckSelect;
    private bool isSelected;
    private bool isSelectable;
    private bool isInitialized;
    private bool forceLocked;   // 튜토리얼 랜드 선택 중 지정 덱 외 선택 차단용
    private void OnLocaleChanged(Locale _)
    {
        deckSelectUI.UpdateTexts(deckData);
        RefreshLockedTooltip();
    }

    private void RefreshLockedTooltip()
    {
        if (deckData.Type != DeckType.Dragon || SaveManager.Game.stageCleared.Get(deckData.Theme.ToString())) return;
        string theme = deckData.Theme.ToString();
        deckSelectUI.SetTooltip(() =>
            LocalizationSettings.StringDatabase.GetLocalizedString("StageName", theme)
            + LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Conquest")
            + LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Incomplete")
        );
    }

    public DeckSelectUI DeckSelectUI => deckSelectUI;
    public delegate void DeckSelectHandler(bool isSelect, DeckData deckdata);
    public DeckSelectHandler OnDeckSelected;
    
    // 초기화 단계별 분리
    private void Awake()
    {
        // 컴포넌트 검증
        ValidateComponents();
    }

    private void Start()
    {
        // UI 이벤트만 연결 (데이터 독립적)
        if (deckSelectUI != null)
        {
            deckSelectUI.onDeckSelected += SelectDeck;
        }
    }

    // 외부에서 호출하는 초기화
    public void Setup(StageDeckSelect newStageDeckSelect)
    {
        if (isInitialized) return;
        if (!ValidateInitializationData(newStageDeckSelect)) return;
        stageDeckSelect = newStageDeckSelect;
        
        // 이벤트 연결
        stageDeckSelect.onDeckPointChanged += CheckDeckPoint;

        // 초기 상태 설정
        SetInitialState();

        if (isDemoLocked)
        {
            isSelectable = false;
            deckSelectUI.DisabledUI(false);
            deckSelectUI.LockedButton(true);
            deckSelectUI.SetTooltip(() => LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Demo"));
        }
        else if (deckData.Type == DeckType.Dragon && !SaveManager.Game.stageCleared.Get(deckData.Theme.ToString()))
        {
            deckSelectUI.DisabledUI(false);
            deckSelectUI.LockedButton(true);
            RefreshLockedTooltip();
        }

        isInitialized = true;
    }

    private void ValidateComponents()
    {
        if (deckSelectUI == null)
        {
            Debug.LogError($"DeckSelectUI not assigned on {gameObject.name}");
        }
    }

    private bool ValidateInitializationData(StageDeckSelect stageDeckSelect)
    {
        if (stageDeckSelect == null)
        {
            Debug.LogError("StageDeckSelect is null");
            return false;
        }

        if (deckData == null)
        {
            Debug.LogError("DeckData is null");
            return false;
        }

        return true;
    }

    private void SetInitialState()
    {
        isSelected = false;
        isSelectable = true;

        // 초기 덱 포인트 체크
        CheckDeckPoint(stageDeckSelect.CurrentDeckPoint);
    }

    /// <summary>외부(스테이지 튜토리얼)에서 덱 선택을 강제로 잠근다/푼다. 선택된 덱은 해제도 막힌다.</summary>
    public void SetForceLocked(bool locked)
    {
        forceLocked = locked;
        if (!isInitialized || isSelected) return;
        CheckDeckPoint(stageDeckSelect.CurrentDeckPoint);   // UI 즉시 갱신
    }

    public void SelectDeck()
    {
        if (!isInitialized || !isSelectable || forceLocked) return;

        isSelected = !isSelected;
        OnDeckSelected?.Invoke(isSelected, deckData);
        deckSelectUI.SelectedUI(isSelected);
    }

    private void CheckDeckPoint(int deckPoint)
    {
        if (!isInitialized || isSelected || isDemoLocked) return;
        
        bool isUnlocked = deckData.Type != DeckType.Dragon || SaveManager.Game.stageCleared.Get(deckData.Theme.ToString());
        isSelectable = !forceLocked && isUnlocked && deckPoint >= deckData.GetDeckPoint();

        deckSelectUI.DisabledUI(isSelectable);
    }

    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    private void OnEnable()
    {
        deckSelectUI.UpdateTexts(deckData);
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        if (isDemoLocked)
        {
            isSelectable = false;
            deckSelectUI.DisabledUI(false);
            deckSelectUI.LockedButton(true);
        }
        else if (deckData.Type == DeckType.Dragon)
        {
            if (SaveManager.Game.stageCleared.Get(deckData.Theme.ToString()))
            {
                isSelectable = !forceLocked;
                deckSelectUI.DisabledUI(!forceLocked);
                deckSelectUI.LockedButton(false);
            }
            else if (isInitialized)
            {
                isSelectable = false;
                deckSelectUI.DisabledUI(false);
                deckSelectUI.LockedButton(true);
            }
        }
    }

    private void OnDisable()
    {
        // 비활성화될 때 상태 초기화
        if (isInitialized)
        {
            ResetState();
        }
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    /// <summary>패널이 CanvasGroup으로 닫힐 때 StageDeckSelect가 호출한다.
    /// 패널이 더 이상 SetActive(false)되지 않아 OnDisable 리셋이 실행되지 않으므로 이를 대체.</summary>
    public void ResetSelectionState()
    {
        if (!isInitialized) return;

        isSelected = false;
        deckSelectUI.SelectedUI(false);
        // 잠금(드래곤덱 미해금·데모락·튜토리얼 강제락) 상태를 반영해 선택 가능 여부와 UI를 다시 계산
        CheckDeckPoint(stageDeckSelect.CurrentDeckPoint);
    }

    private void ResetState()
    {
        isSelected = false;
        isSelectable = true;

        if (deckSelectUI != null)
        {
            deckSelectUI.SelectedUI(false);
            deckSelectUI.DisabledUI(true);
            //deckSelectUI.ClearTooltip();
        }
    }

    private void OnDestroy()
    {
        // UI 이벤트 해제
        if (deckSelectUI != null)
        {
            deckSelectUI.onDeckSelected -= SelectDeck;
        }

        // 스테이지 이벤트 해제
        if (stageDeckSelect != null)
        {
            stageDeckSelect.onDeckPointChanged -= CheckDeckPoint;
        }
    }
}

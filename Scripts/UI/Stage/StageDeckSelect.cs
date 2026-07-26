using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class StageDeckSelect : MonoBehaviour
{
    [Header("Dual Deck Select UI")]
    [SerializeField] private TooltipTrigger advancedToggleTip;
    [SerializeField] private TooltipTrigger advancedButtonTip;
    [SerializeField] private TooltipTrigger fusionButtonTip;
    [SerializeField] private GameObject deckSelectPanel;
    [SerializeField] private CanvasGroup deckSelectPanelGroup;
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private Toggle advancedToggle;
    [SerializeField] private Button backButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button explainButton;
    [SerializeField] private TabController menuTabController;
    
    // 덱 버튼 프리펩
    [SerializeField] private List<DeckController> deckButtons;
    [SerializeField] private Button advancedButton;
    [SerializeField] private Image advancedLockedImage;
    [SerializeField] private Button fusionButton;
    [SerializeField] private Image fusionLockedImage;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI stageName;
    [SerializeField] private TextMeshProUGUI stageDesc;
    
    [Header("Button Selected Color")]
    [SerializeField] private Color buttonSelectedColor = new Color(0.7f, 0.85f, 1f, 1f);
    [SerializeField] private Color buttonNormalColor = Color.white;

    [Header("DeckPoint")]
    [SerializeField] private int defaultDeckPoint;
    [SerializeField] private TextMeshProUGUI deckPointText;
    
    private DeckData advancedDeckData;
    private bool isAdvancedSelected;
    private bool isAdvancedSelectable;
    private DeckData fusionDeckData;
    private bool isFusionSelected;
    private bool isFusionSelectable;
    // 덱 데이터, 덱 컨트롤러로 구성된 딕셔너리
    private Dictionary<DeckData, DeckController> deckControllerMap = new();
    private int currentDeckPoint;
    private bool isStartable;
    private StageSelector currentStage;
    private void OnLocaleChanged(Locale _)
    {
        UpdateTexts();
        RefreshAdvancedToggleTip();
        UpdateAdvancedButton();
        UpdateFusionButton();
    }

    private void RefreshAdvancedToggleTip()
    {
        if (currentStage == null || advancedToggle.interactable) return;
        advancedToggleTip?.SetData(
            "",
            LocalizationSettings.StringDatabase.GetLocalizedString("StageName", currentStage.SceneType.ToString())
            + LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Conquest")
            + LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Incomplete")
        );
    }

    public delegate void DeckPointHandler(int deckPoint);
    public DeckPointHandler onDeckPointChanged;
    public int CurrentDeckPoint => currentDeckPoint;

    private void Awake()
    {
        SetUp();
    }

    void Start()
    {
        foreach (DeckController button in deckButtons)
        {
            button.Setup(this);
            button.OnDeckSelected += OnDeckSelected;
        }
        startButton.onClick.AddListener(StartGame);
        advancedButton.onClick.AddListener(OnAdvancedSelected);
        fusionButton.onClick.AddListener(OnFusionSelected);
        StageManager.Instance.onStageSelected += OnStageSelected;
        StageManager.Instance.onStageDeselected += OnStageDeselected;
        SetPanelVisible(false);
        backButton.onClick.AddListener(ClosePopup);
        advancedToggle.onValueChanged.AddListener(OnAdvancedToggleChanged);
    }

    private void SetUp()
    {
        currentDeckPoint = defaultDeckPoint;
        startButton.interactable = false;
        advancedButton.interactable = false;
        fusionButton.interactable = false;
        isStartable = false;
        isAdvancedSelected = false;
        isAdvancedSelectable = false;
        isFusionSelected = false;
        isFusionSelectable = false;
        SetButtonSelectedColor(advancedButton, false);
        SetButtonSelectedColor(fusionButton, false);
        advancedToggleTip?.ClearData();
        advancedButtonTip?.ClearData();
        fusionButtonTip?.ClearData();
    }

    /// <summary>SetActive 대신 CanvasGroup으로 패널 표시를 토글한다.
    /// OnEnable 연쇄와 Canvas 전체 리빌드를 피하기 위함 (스테이지 선택 시 프레임드랍 수정).</summary>
    private void SetPanelVisible(bool visible)
    {
        deckSelectPanelGroup.alpha = visible ? 1f : 0f;
        deckSelectPanelGroup.interactable = visible;
        deckSelectPanelGroup.blocksRaycasts = visible;
    }

    private void ClosePopup()
    {
        if (menuTabController != null && menuTabController.HasOpenPanel)
        {
            menuTabController.CloseCurrentPanel();
            return;
        }
        ToggleReset();
        StageManager.Instance.DeSelect();
    }

    private void OnDeckSelected(bool isSelected, DeckData deck)
    {
        DeckManager.Instance.SelectDeck(isSelected, deck);
        UpdateStartButtonUI(DeckManager.Instance.GetDeckDataCount());

        // 덱 포인트 초기화
        int deckPoint = 0;
        // 덱 선택 시 덱 종류에 따른 포인트 변경
        if (isSelected)
            deckPoint -= deck.GetDeckPoint();
        else
            deckPoint = deck.GetDeckPoint();
        // 덱 포인트 업데이트
        DeckPointChange(deckPoint);

        if (deck.Type == DeckType.Duel)
        {
            // 듀얼덱 선택 해제 시 강화덱/융합덱도 같이 해제
            if (!isSelected)
            {
                DeselectDeckIfSelected(ref isAdvancedSelected, advancedDeckData, advancedButton);
                DeselectDeckIfSelected(ref isFusionSelected, fusionDeckData, fusionButton);
            }

            UpdateAdvancedButton();
            UpdateFusionButton();
        }
    }
    
    private void DeselectDeckIfSelected(ref bool isSelected, DeckData deckData, Button button)
    {
        if (!isSelected) return;

        isSelected = false;
        // 선택중인 덱 리스트 업데이트
        DeckManager.Instance.SelectDeck(false, deckData);

        DeckPointChange(deckData.GetDeckPoint());
        SetButtonSelectedColor(button, false);

        // 게임 시작 가능 여부 업데이트
        UpdateStartButtonUI(DeckManager.Instance.GetDeckDataCount());
    }

    private void SetButtonSelectedColor(Button btn, bool selected)
    {
        if (btn == null) return;
        btn.GetComponent<Image>().color = selected ? buttonSelectedColor : buttonNormalColor;
    }

    private void DeckPointChange(int deckPoint)
    {
        currentDeckPoint += deckPoint;
        onDeckPointChanged?.Invoke(currentDeckPoint);
        UpdateDeckPointTextUI();
    }

    private void UpdateDeckPointTextUI()
    {
        deckPointText.text = currentDeckPoint + "/" + defaultDeckPoint;
    }

    private void OnStageSelected(StageSelector stage)
    {
        SetUp();
        UpdateDeckPointTextUI();
        DeckManager.Instance.ClearSelectedDeck();
        DeckManager.Instance.ClearSelectedAiDeck();
        currentStage = stage;
        UpdateTexts();
        
        SetPanelVisible(true);
        backButton.gameObject.SetActive(true);
        //explainButton.gameObject.SetActive(true);
        
        DeckManager.Instance.SelectAiDeck(currentStage.DeckDatas);
        
        if (stage.DeckDatas.Count <= 1
            && stage.DeckDatas[0].Type == DeckType.Duel
            && stage.DeckDatas[0].AdvancedDeckData != null)   // 튜토리얼 랜드 등 강화덱 없는 덱 방어
        {
            advancedToggle.interactable = true;
            advancedToggleTip?.ClearData();

            if (!SaveManager.Game.stageCleared.Get(stage.SceneType.ToString()))
            {
                advancedToggle.interactable = false;
                RefreshAdvancedToggleTip();
            }
        }
        else
        {
            advancedToggle.interactable = false;
        }
    }

    private void OnStageDeselected()
    {
        ToggleReset();
        backButton.gameObject.SetActive(false);
        explainButton.gameObject.SetActive(false);
        SetPanelVisible(false);
        advancedLockedImage.gameObject.SetActive(false);
        fusionLockedImage.gameObject.SetActive(false);
        stageName.text = "";
        stageDesc.text = "";
        currentStage = null;
        DeckManager.Instance.ClearSelectedDeck();
        DeckManager.Instance.ClearSelectedAiDeck();

        // 패널이 이제 비활성화되지 않으므로, 기존 OnDisable 연쇄가 하던 리셋을 직접 수행
        SetUp();
        foreach (DeckController button in deckButtons)
        {
            button.ResetSelectionState();
        }
        EventSystem.current?.SetSelectedGameObject(null);
    }
    
    /// <summary>시작 버튼으로 게임 시작이 확정된 시점 (씬 로드 직전) 알림.</summary>
    public event System.Action OnGameStarted;

    private void StartGame()
    {
        if (!isStartable)
        {
            return;
        }

        OnGameStarted?.Invoke();
        Managers.Scene.LoadScene(currentStage.SceneType, advancedToggle.isOn);
    }

    private void OnAdvancedSelected()
    {
        if (!isAdvancedSelectable) return;
        isAdvancedSelected = !isAdvancedSelected;
        OnDeckSelected(isAdvancedSelected, advancedDeckData);
        SetButtonSelectedColor(advancedButton, isAdvancedSelected);
    }

    private void UpdateAdvancedButton()
    {
        // 이미 선택된 상태면 잠금 재판정 불필요 (로케일 변경 등으로 재호출될 수 있음)
        if (isAdvancedSelected) return;

        if (DeckManager.Instance.SelectedDeckDatas.Count == 0 || currentDeckPoint < 2 || DeckManager.Instance.SelectedDeckDatas.Count > 1)
        {
            advancedLockedImage.gameObject.SetActive(false);
            isAdvancedSelectable = false;
            advancedButton.interactable = false;
            advancedButtonTip?.ClearData();
        }
        else
        {
            DeckData deckData = DeckManager.Instance.SelectedDeckDatas[0];
            if (!SaveManager.Game.stageCleared.Get(deckData.Theme.ToString()))
            {
                advancedLockedImage.gameObject.SetActive(true);
                advancedButtonTip?.SetData(
                    null,
                    LocalizationSettings.StringDatabase.GetLocalizedString("StageName", deckData.Theme.ToString())
                    + LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Conquest")
                    + LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Incomplete")
                );
                return;
            }
            advancedLockedImage.gameObject.SetActive(false);
            advancedDeckData = deckData.AdvancedDeckData;
            isAdvancedSelectable = true;
            advancedButton.interactable = true;
            advancedButtonTip?.ClearData();
        }
    }

    private void OnFusionSelected()
    {
        if (!isFusionSelectable) return;
        isFusionSelected = !isFusionSelected;
        OnDeckSelected(isFusionSelected, fusionDeckData);
        SetButtonSelectedColor(fusionButton, isFusionSelected);
    }

    private void UpdateFusionButton()
    {
        // 이미 선택된 상태면 잠금 재판정 불필요 (로케일 변경 등으로 재호출될 수 있음)
        if (isFusionSelected) return;

        if (currentDeckPoint < 1 || DeckManager.Instance.SelectedDeckDatas.Count != 2)
        {
            fusionLockedImage.gameObject.SetActive(false);
            isFusionSelectable = false;
            fusionButton.interactable = false;
            fusionButtonTip?.ClearData();
        }
        else
        {
            if (DeckManager.Instance.SelectedDeckDatas[0].FusionDeckData == DeckManager.Instance.SelectedDeckDatas[1].FusionDeckData)
            {
                string theme0 = DeckManager.Instance.SelectedDeckDatas[0].Theme.ToString();
                string theme1 = DeckManager.Instance.SelectedDeckDatas[1].Theme.ToString();
                bool cleared0 = SaveManager.Game.stageCleared.Get(theme0);
                bool cleared1 = SaveManager.Game.stageCleared.Get(theme1);
                if (!cleared0 || !cleared1)
                {
                    fusionLockedImage.gameObject.SetActive(true);
                    fusionButtonTip?.SetData(
                        null,
                        LocalizationSettings.StringDatabase.GetLocalizedString("StageName", theme0)
                        + LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Conquest")
                        + (cleared0
                            ? LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Complete")
                            : LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Incomplete"))
                        + "\n"
                        + LocalizationSettings.StringDatabase.GetLocalizedString("StageName", theme1)
                        + LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Conquest")
                        + (cleared1
                            ? LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Complete")
                            : LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Incomplete"))
                    );
                    return;
                }
                fusionLockedImage.gameObject.SetActive(false);
                fusionDeckData = DeckManager.Instance.SelectedDeckDatas[0].FusionDeckData;
                isFusionSelectable = true;
                fusionButton.interactable = true;
                fusionButtonTip?.ClearData();
            }
        }
    }

    private void UpdateStartButtonUI(int deckDataCount)
    {
       if (deckDataCount <= 0)
       {
            isStartable = false;
            startButton.interactable = false;
       }
       else
       {
            isStartable = true;
            startButton.interactable = true;
       }

    }
    
    private void OnAdvancedToggleChanged(bool isOn)
    {
        if (currentStage == null) return;
        currentStage.OnAdvancedVFX(isOn);
        if (currentStage.DeckDatas.Count == 1)
        {
            DeckData aiAdvancedDeck = currentStage.DeckDatas[0].AdvancedDeckData;
            if (isOn)
                DeckManager.Instance.AddAiDeck(aiAdvancedDeck);
            else
                DeckManager.Instance.RemoveAiDeck(aiAdvancedDeck);
        }
    }

    private void ToggleReset()
    {
        advancedToggle.SetIsOnWithoutNotify(false);
        currentStage?.OnAdvancedVFX(false);
    }

    private void UpdateTexts()
    {
        //if (!deckSelectPanel.gameObject.activeSelf) return;
        if (currentStage != null)
        {
            stageName.text = LocalizationSettings.StringDatabase.GetLocalizedString("StageName", currentStage.SceneType.ToString());
            stageDesc.text = LocalizationSettings.StringDatabase.GetLocalizedString("StageDescription", currentStage.SceneType.ToString());
        }
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        SetUp();
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        backButton.gameObject.SetActive(false);

        ToggleReset();

        advancedDeckData = null;
        fusionDeckData = null;
        currentStage = null;
        stageName.text = "";
        stageDesc.text = "";
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void OnDestroy()
    {
        foreach (DeckController button in deckButtons)
        {
            button.OnDeckSelected -= OnDeckSelected;
        }
        StageManager.Instance.onStageSelected -= OnStageSelected;
        StageManager.Instance.onStageDeselected -= OnStageDeselected;
    }
}

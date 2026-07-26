using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 선택 씬 진입 시 튜토리얼 진행 여부를 묻는 팝업.
/// "시작"은 튜토리얼 랜드를 강제 선택해 StageTutorialManager의 스텝을 개시한다.
/// (적 덱은 튜토리얼 랜드의 DeckDatas가, 플레이어 덱은 스텝의 파이어덱 선택이 결정)
/// 다음 두 경우에는 표시하지 않는다:
///   - 튜토리얼을 이미 클리어함 (stageCleared["Tutorial"], 스킵 포함)
///   - "다시 나타내지 않음"을 체크하고 닫음 (tutorialPromptDismissed)
/// </summary>
public class TutorialPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button startButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Toggle dontShowAgainToggle;
    [SerializeField] private StageTutorialManager stageTutorial;

    private void Start()
    {
        bool cleared = SaveManager.Game.stageCleared.Get(Define.Scene.Tutorial.ToString());
        panelRoot.SetActive(false);
        if (cleared || SaveManager.Game.tutorialPromptDismissed) return;

        startButton.onClick.AddListener(OnStartClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        panelRoot.SetActive(true);
    }

    private void OnStartClicked()
    {
        SaveDismissIfChecked();
        panelRoot.SetActive(false);
        stageTutorial.Begin();
    }

    private void OnCancelClicked()
    {
        SaveDismissIfChecked();
        panelRoot.SetActive(false);
    }

    private void SaveDismissIfChecked()
    {
        if (dontShowAgainToggle == null || !dontShowAgainToggle.isOn) return;
        SaveManager.Game.tutorialPromptDismissed = true;
        SaveManager.SaveGame();
    }
}
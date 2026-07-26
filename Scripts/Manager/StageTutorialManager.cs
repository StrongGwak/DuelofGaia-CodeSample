using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 스테이지 선택 씬 전용 튜토리얼 러너.
/// 흐름: TutorialPromptUI "시작" 또는 튜토리얼 랜드 직접 클릭
///   → 스텝 실행 (클리어 여부 무관 — 재도전 시에도 안내를 다시 보여준다):
///     덱 패널 설명 → 지정 덱 선택 → 게임 시작 안내
///     (Dialogue 스텝은 딤이 다른 입력을 덮고, 뒤로가기 이탈 시 스텝은 중단·재클릭 시 재실행)
///   → 게임 시작은 기존 StageDeckSelect.StartGame 흐름 그대로 (랜드 sceneType = Tutorial)
/// 튜토리얼 랜드가 선택된 동안에는 클리어 여부와 무관하게 지정 덱(파이어)만 선택 가능
/// (튜토리얼 씬의 강제 드로우 카드가 해당 덱에 묶여 있음).
/// </summary>
public class StageTutorialManager : MonoBehaviour
{
    [Header("Steps")]
    [SerializeField] private List<TutorialStepSO> steps;

    [Header("Scene References")]
    [SerializeField] private TutorialPanelUI panel;
    [SerializeField] private HighlightEntry[] highlightTargets;
    [SerializeField] private StageSelector tutorialLand;
    [SerializeField] private StageDeckSelect deckSelect;   // 시작 버튼 이벤트 구독용

    [Header("Deck")]
    [SerializeField] private DeckController fireDeck;        // 튜토리얼에서 선택하는 덱
    [SerializeField] private DeckController[] lockedDecks;   // 랜드 선택 중 잠글 나머지 덱 전부

    [Serializable]
    public struct HighlightEntry
    {
        public string id;
        public RectTransform target;
    }

    private bool deckSelected;
    private bool gameStarted;
    private bool stepsRunning;

    private void Start()
    {
        StageManager.Instance.onStageSelectStarted += OnStageSelectStarted;
        StageManager.Instance.onStageSelected += OnStageSelected;
        StageManager.Instance.onStageDeselected += OnStageDeselected;
        fireDeck.OnDeckSelected += OnFireDeckSelected;
        deckSelect.OnGameStarted += OnGameStarted;
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.onStageSelectStarted -= OnStageSelectStarted;
            StageManager.Instance.onStageSelected -= OnStageSelected;
            StageManager.Instance.onStageDeselected -= OnStageDeselected;
            StageManager.Instance.InputLocked = false;
        }
        if (fireDeck != null) fireDeck.OnDeckSelected -= OnFireDeckSelected;
        if (deckSelect != null) deckSelect.OnGameStarted -= OnGameStarted;
    }

    /// <summary>TutorialPromptUI의 "시작"이 호출한다 — 튜토리얼 랜드를 강제 선택.</summary>
    public void Begin()
    {
        StageManager.Instance.ForceSelect(tutorialLand);
    }

    // 줌 시작 시점 — 덱 강제 + 이탈 잠금
    private void OnStageSelectStarted(StageSelector stage)
    {
        if (stage != tutorialLand) return;

        foreach (DeckController deck in lockedDecks)
            deck.SetForceLocked(true);

        StageManager.Instance.InputLocked = true;   // 지형 오클릭으로 선택이 풀리는 사고 방지 (뒤로가기 이탈은 허용)
    }

    // 줌 완료(덱 패널 오픈) 시점 — 스텝 시작
    private void OnStageSelected(StageSelector stage)
    {
        if (stage != tutorialLand || stepsRunning) return;
        StartCoroutine(RunSteps());
    }

    private void OnStageDeselected()
    {
        foreach (DeckController deck in lockedDecks)
            deck.SetForceLocked(false);
        fireDeck.SetForceLocked(false);

        // Action 스텝 중 뒤로가기로 이탈한 경우 — 중단하고, 랜드를 다시 클릭하면 처음부터 재실행
        if (stepsRunning)
        {
            StopAllCoroutines();
            stepsRunning = false;
            panel.Hide();
        }
        StageManager.Instance.InputLocked = false;
    }

    private void OnFireDeckSelected(bool isSelected, DeckData _)
    {
        if (!isSelected || !stepsRunning) return;
        deckSelected = true;
        fireDeck.SetForceLocked(true);   // 선택 확정 — 해제로 시작 버튼이 다시 꺼지는 것 방지
    }

    private void OnGameStarted()
    {
        if (stepsRunning) gameStarted = true;
    }

    private IEnumerator RunSteps()
    {
        stepsRunning = true;
        deckSelected = false;
        gameStarted = false;
        panel.gameObject.SetActive(true);   // 씬에서 꺼둔 스텝 UI 켜기

        for (int i = 0; i < steps.Count; i++)
            yield return RunStep(steps[i], isLast: i == steps.Count - 1);

        stepsRunning = false;
        // 마지막 스텝(게임 시작 안내)의 텍스트/하이라이트는 유지한 채 종료 —
        // 시작 버튼 클릭 → 기존 StartGame이 튜토리얼 씬을 로드하며 마무리된다
    }

    private IEnumerator RunStep(TutorialStepSO step, bool isLast)
    {
        SetHighlight(step.HighlightId, step.Kind == TutorialStepKind.Action);

        // Dialogue는 딤으로 다른 입력을 막고 탭으로 진행 (정지시킬 게임플레이가 없어 pause는 안 쓴다)
        bool blocking = step.Kind == TutorialStepKind.Dialogue;
        foreach (string key in step.TextKeys)
        {
            panel.ShowText(key, blocking);
            if (blocking) yield return panel.WaitForTap();
        }

        if (step.Completion == TutorialCompletion.DeckSelected)
            yield return new WaitUntil(() => deckSelected);

        if (step.Completion == TutorialCompletion.GameStarted)
            yield return new WaitUntil(() => gameStarted);   // 클릭 직후 씬이 로드되므로 사실상 종료 대기

        if (!isLast) panel.Hide();
    }

    private void SetHighlight(string id, bool loop)
    {
        RectTransform target = string.IsNullOrEmpty(id)
            ? null
            : highlightTargets.FirstOrDefault(h => h.id == id).target;
        panel.SetHighlight(target, loop);
    }
}
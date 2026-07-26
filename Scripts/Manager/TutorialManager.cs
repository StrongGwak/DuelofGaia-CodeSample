using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 씬 전용 진행 매니저.
/// - 스텝 리스트를 순회하며 설명(정지)/행동(진행) 스텝을 실행
/// - 시작 시점부터 모든 완료 이벤트를 구독해 플래그로 기록 → 이미 충족된 스텝은 자동 통과
/// - pause 소유권을 직접 가짐 (BaseUI.pauseInGame을 쓰지 않는다)
/// - 행동 잠금은 스텝 단위 강제: 각 스텝의 Allowed에 있는 행동만 허용되고,
///   이전 스텝에서 배운 행동도 현재 스텝 Allowed에 없으면 다시 잠긴다
///   (드로우/스킬 습득은 Button.interactable로 잠금 — KeyBindButton이
///   interactable을 검사하므로 단축키도 함께 막힌다)
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("Steps")]
    [SerializeField] private List<TutorialStepSO> steps;

    [Header("Scarecrow Spawn")]
    [SerializeField] private CharacterData scarecrowData;      // 허수아비 데이터 (MoveSpeed 0)
    [SerializeField] private Transform scarecrowSpawnPoint;    // 소환 지점

    [Header("Scene References")]
    [SerializeField] private TutorialPanelUI panel;            // 설명/힌트 UI (탭 진행 포함)
    [SerializeField] private HighlightEntry[] highlightTargets;

    [Header("Lock Targets")]
    [SerializeField] private Button drawButton;                              // 드로우 버튼
    [SerializeField] private AcquireDuelistSkillButton[] skillAcquireButtons; // 스킬 습득 버튼들 (잠금 + 습득 감지 겸용)

    [Header("Target Found 판정")]
    [Tooltip("뷰포트 가장자리 여유 — 이만큼 안쪽까지 들어와야 '발견'으로 친다 (0~0.4)")]
    [SerializeField] private float targetFoundViewportMargin = 0.1f;

    [Serializable]
    public struct HighlightEntry
    {
        public string id;
        public RectTransform target;
    }

    // 자동 통과용 완료 카운트 — 스텝 도달 전에 발생한 이벤트도 기록되며,
    // 같은 완료 조건을 쓰는 스텝이 여러 개면 각 스텝이 발생 1회씩을 소비한다
    private readonly Dictionary<TutorialCompletion, int> completedCounts = new();
    private readonly Dictionary<TutorialCompletion, int> consumedCounts = new();
    private bool matchWon;

    private Duelist playerDuelist;
    private CardComponent playerCards;
    private Camera mainCamera;
    private BaseCharacter scarecrow;    // 허수아비 — 런타임 스폰 후 할당
    private TutorialStepSO currentStep; // 진행 중인 스텝 (드로우 연타 방지 판정용)
    private bool targetFound;   // TargetFound는 1회만 기록

    private void Start()
    {
        mainCamera = Camera.main;

        BattleManager.Instance.DoWhenBattleReady(() => StartCoroutine(WaitForPlayerAndRun()));
    }

    // MatchInitializer.SetupMatch도 OnBattleReady 구독자라 실행 순서가 보장되지 않는다.
    // → 듀얼리스트가 실제로 생성될 때까지 기다렸다가 튜토리얼을 시작한다.
    private IEnumerator WaitForPlayerAndRun()
    {
        yield return new WaitUntil(() =>
            BattleManager.Instance.ActiveDuelists.Any(d => d.IsPlayer));

        playerDuelist = BattleManager.Instance.ActiveDuelists.First(d => d.IsPlayer);
        SpawnScarecrow();
        SubscribeAll();
        LockAll();
        yield return RunTutorial();
    }

    /// <summary>허수아비를 적 팀으로 스폰 지점에 소환한다. 이동속도 0은 데이터에서 처리.</summary>
    private void SpawnScarecrow()
    {
        if (scarecrowData == null || scarecrowSpawnPoint == null)
        {
            Debug.LogError("[TutorialManager] 허수아비 데이터/스폰 지점이 할당되지 않았습니다.");
            return;
        }

        var bm = BattleManager.Instance;
        var enemyTeam = bm.GetCategoryForOwnerTeam(
            bm.OwnerTeam == OwnerTeam.Team1 ? OwnerTeam.Team2 : OwnerTeam.Team1);

        scarecrow = SpawnManager.Instance.SpawnCharacter(
            scarecrowData,
            enemyTeam,
            scarecrowSpawnPoint.position,
            scarecrowSpawnPoint.rotation);
        scarecrow.Died += OnScarecrowDied;
    }

    private void OnDestroy()
    {
        TutorialGate.Release();   // 튜토리얼 밖 씬에 잠금이 새지 않게
        UnsubscribeAll();
    }

    private void Update()
    {
        CheckTargetFound();
    }

    // 허수아비가 카메라 화면 안(여유 마진 안쪽)에 들어오면 TargetFound를 1회 기록한다
    private void CheckTargetFound()
    {
        if (targetFound || scarecrow == null || mainCamera == null) return;

        Vector3 vp = mainCamera.WorldToViewportPoint(scarecrow.transform.position);
        if (vp.z <= 0f) return;   // 카메라 뒤
        float m = targetFoundViewportMargin;
        if (vp.x < m || vp.x > 1f - m || vp.y < m || vp.y > 1f - m) return;

        targetFound = true;
        MarkDone(TutorialCompletion.TargetFound);
    }

    // ─────────────────────────────── 행동 잠금

    /// <summary>
    /// 스텝에 허용된 행동만 남기고 나머지는 전부 잠근다.
    /// 이전 스텝에서 배운 행동이라도 현재 스텝 Allowed에 없으면 다시 잠긴다.
    /// 스텝 SO의 Allowed에는 "이 스텝 동안 허용할 행동 전체"를 넣어야 한다.
    /// </summary>
    private void ApplyStepLocks(TutorialAllow allowed)
    {
        TutorialGate.SetAllowed(allowed);

        drawButton.interactable = (allowed & TutorialAllow.Draw) != 0;

        bool skillOn = (allowed & TutorialAllow.SkillPanel) != 0;
        foreach (var acquire in skillAcquireButtons)
            acquire.GetComponent<Button>().interactable = skillOn;
    }

    private void LockAll() => ApplyStepLocks(TutorialAllow.None);
    private void UnlockAll() => ApplyStepLocks(TutorialAllow.All);

    // ─────────────────────────────── 이벤트 구독

    private void SubscribeAll()
    {
        playerCards = playerDuelist.CardComponent;

        playerCards.onDrawed += OnDrawed;
        playerCards.onHandChanged += RewireHandEvents;
        RewireHandEvents();

        drawButton.onClick.AddListener(OnDrawClicked);

        // 스킬 습득 감지 — OnButtonClick은 isAvailable 검사를 통과한 실제 습득 시점에만 발화한다
        foreach (var acquire in skillAcquireButtons)
            acquire.OnButtonClick += OnSkillAcquireClicked;
        
        BattleManager.Instance.OnMatchOver += OnMatchOver;
    }

    // RewardAction.cs와 같은 패턴: 핸드가 바뀔 때마다 카드 이벤트 재구독
    private void RewireHandEvents()
    {
        foreach (CardController cc in playerCards.Hands)
        {
            cc.OnSpawn -= OnMonsterSpawned;   // 중복 방지
            cc.OnSpawn += OnMonsterSpawned;
        }
    }

    // 드로우 스텝 연타 방지 — 첫 클릭 즉시 버튼을 재잠금.
    // onDrawed는 드로우 애니메이션 종료 후 발화하므로, 그 사이 추가 클릭이
    // 포인트 소모/예약 카드 소진을 일으키는 것을 클릭 시점에 차단한다.
    // 다음 드로우 스텝의 ApplyStepLocks가 다시 풀어준다.
    private void OnDrawClicked()
    {
        if (currentStep != null && currentStep.Completion == TutorialCompletion.CardDrawn)
            drawButton.interactable = false;
    }

    private void OnDrawed() => MarkDone(TutorialCompletion.CardDrawn);
    private void OnSkillAcquireClicked() => MarkDone(TutorialCompletion.SkillAcquired);
    private void OnScarecrowDied(GameObject _) => MarkDone(TutorialCompletion.TargetDied);
    private void OnMonsterSpawned() => MarkDone(TutorialCompletion.MonsterSpawned);

    private void OnMatchOver(bool isWin)
    {
        if (!isWin) return;   // 튜토리얼 듀얼리스트는 위협이 없어 패배 경로는 없어야 한다
        matchWon = true;
        MarkDone(TutorialCompletion.MatchWon);
    }

    private void MarkDone(TutorialCompletion flag)
    {
        completedCounts.TryGetValue(flag, out int count);
        completedCounts[flag] = count + 1;
    }

    private void UnsubscribeAll()
    {
        if (playerCards != null)
        {
            playerCards.onDrawed -= OnDrawed;
            playerCards.onHandChanged -= RewireHandEvents;
            foreach (CardController cc in playerCards.Hands)
                cc.OnSpawn -= OnMonsterSpawned;
        }
        foreach (var acquire in skillAcquireButtons)
        {
            if (acquire != null) acquire.OnButtonClick -= OnSkillAcquireClicked;
        }
        if (drawButton != null) drawButton.onClick.RemoveListener(OnDrawClicked);
        if (scarecrow != null) scarecrow.Died -= OnScarecrowDied;
        if (BattleManager.Instance != null) BattleManager.Instance.OnMatchOver -= OnMatchOver;
    }

    // ─────────────────────────────── 진행 루프

    private IEnumerator RunTutorial()
    {
        BattleManager.Instance.MatchStart();

        foreach (TutorialStepSO step in steps)
        {
            // 적을 먼저 죽였으면 남은 스텝은 접고 종료
            if (matchWon && step.Completion != TutorialCompletion.MatchWon)
                continue;

            yield return RunStep(step);
        }

        FinishTutorial();
    }

    private IEnumerator RunStep(TutorialStepSO step)
    {
        currentStep = step;

        // 시나리오 드로우 예약 — 이 스텝 이후의 드로우가 지정된 카드 순서로 나온다
        if (step.ForcedDraws != null)
        {
            foreach (CardData card in step.ForcedDraws)
                playerCards.EnqueueForcedDraw(card);
        }

        ApplyStepLocks(step.Allowed);
        SetHighlight(step.HighlightId, step.Kind == TutorialStepKind.Action);

        bool pause = step.Kind == TutorialStepKind.Dialogue;
        if (pause) GameSpeed.PushPause();

        // 텍스트 페이지 표시 — Dialogue는 페이지마다 탭 대기 (패널 연출은 반드시 unscaled)
        foreach (string key in step.TextKeys)
        {
            panel.ShowText(key, blocking: pause);
            if (pause) yield return panel.WaitForTap();
        }

        if (pause) GameSpeed.PopPause();

        // 자동 통과: 이미 충족된 발생분이 남아 있으면 대기 없이 다음으로.
        // 같은 완료 조건의 스텝마다 발생 1회씩 소비하므로 반복 스텝(드로우 ×3 등)도 각각 새 행동을 요구한다
        if (step.Completion != TutorialCompletion.Tap)
        {
            consumedCounts.TryGetValue(step.Completion, out int used);
            int need = used + 1;
            consumedCounts[step.Completion] = need;
            yield return new WaitUntil(() =>
                matchWon ||   // 듀얼리스트를 먼저 잡아 매치가 끝나면 대기 중인 스텝도 즉시 탈출
                (completedCounts.TryGetValue(step.Completion, out int done) && done >= need));
        }

        panel.Hide();
        SetHighlight(null);
    }

    private void SetHighlight(string id, bool loop = false)
    {
        RectTransform target = string.IsNullOrEmpty(id)
            ? null
            : highlightTargets.FirstOrDefault(h => h.id == id).target;
        panel.SetHighlight(target, loop);
    }

    // ─────────────────────────────── 종료

    /// <summary>
    /// 모든 스텝 종료 시 호출. 씬 전환은 하지 않는다 —
    /// 매치 오버 승리 UI의 게임종료 버튼이 스테이지 선택으로 보내준다.
    /// </summary>
    private void FinishTutorial()
    {
        UnlockAll();
        SaveClear();
    }

    /// <summary>스킵 버튼 전용. 클리어로 취급하고 즉시 스테이지 선택으로 나간다.
    /// timeScale은 건드리지 않는다 — 씬 로드 경로(SceneFader/SceneManagerEx)가 1로 복구한다.</summary>
    public void CompleteTutorial()
    {
        StopAllCoroutines();
        UnlockAll();
        SaveClear();
        Managers.Scene.LoadScene(Define.Scene.Stage_Select);
    }

    // 클리어 저장 — StageSelector.IsCleared와 같은 키 규칙(SceneType 문자열)을 쓴다
    private void SaveClear()
    {
        string clearKey = Define.Scene.Tutorial.ToString();
        if (!SaveManager.Game.stageCleared.Get(clearKey))
        {
            SaveManager.Game.stageCleared.Set(clearKey, true);
            SaveManager.SaveGame();
        }
    }
}
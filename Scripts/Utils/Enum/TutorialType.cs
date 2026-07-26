using System;

/// <summary>튜토리얼 스텝의 진행 방식</summary>
public enum TutorialStepKind
{
    Dialogue,   // 게임 정지 + 탭으로 진행
    Action,     // 게임 진행 + 완료 이벤트 대기
    Hint,       // 비차단 배너만 표시, 대기 없음
}

/// <summary>스텝 완료 판정 조건</summary>
public enum TutorialCompletion
{
    Tap,             // 화면 탭/클릭
    CardDrawn,       // CardComponent.onDrawed
    SkillAcquired,   // AcquireDuelistSkillButton.OnButtonClick
    TargetDied,      // 지정 타겟(허수아비) BaseCharacter.Died
    MonsterSpawned,  // CardController.OnSpawn
    MatchWon,        // BattleManager.OnMatchOver(true)
    TargetFound,     // 허수아비가 카메라 뷰포트에 들어옴 (TutorialManager가 매 프레임 판정)
    DeckSelected,    // 스테이지 선택 튜토리얼 — 지정 덱 선택 (DeckController.OnDeckSelected)
    GameStarted,     // 스테이지 선택 튜토리얼 — 시작 버튼 클릭 (StageDeckSelect.OnGameStarted)
                     // 주의: 직렬화 값 보존을 위해 새 값은 항상 맨 뒤에 추가한다
}

/// <summary>스텝별 허용 행동 (0~3단계 잠금용)</summary>
[Flags]
public enum TutorialAllow
{
    None       = 0,
    Draw       = 1 << 0,   // 드로우 버튼
    SkillPanel = 1 << 1,   // 스킬 습득 패널
    CardUse    = 1 << 2,   // 카드 드래그/사용
    CameraMove = 1 << 3,   // 카메라 팬/줌/진영 포커스
    All        = ~0,
}

/// <summary>
/// 튜토리얼 진행 중 행동 잠금 상태를 노출하는 정적 게이트.
/// 튜토리얼 씬이 아니면 항상 All(전부 허용)이므로 기존 씬에 영향 없음.
/// </summary>
public static class TutorialGate
{
    public static TutorialAllow Allowed { get; private set; } = TutorialAllow.All;

    public static bool CardUseLocked => (Allowed & TutorialAllow.CardUse) == 0;
    public static bool CameraMoveLocked => (Allowed & TutorialAllow.CameraMove) == 0;

    public static void SetAllowed(TutorialAllow allowed) => Allowed = allowed;
    public static void Release() => Allowed = TutorialAllow.All;
}
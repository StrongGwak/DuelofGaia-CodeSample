using UnityEngine;

/// <summary>
/// 튜토리얼 스텝 하나의 정의. 순서는 TutorialManager의 리스트가 결정한다.
/// </summary>
[CreateAssetMenu(menuName = "SO/Tutorial/TutorialStep")]
public class TutorialStepSO : ScriptableObject
{
    [Header("Step")]
    [SerializeField] private TutorialStepKind kind;
    [SerializeField] private TutorialCompletion completion;

    [Header("Text (String Table: Tutorial)")]
    [Tooltip("Tutorial 테이블의 엔트리 키. Dialogue는 여러 페이지 가능")]
    [SerializeField] private string[] textKeys;

    [Header("Restriction")]
    [Tooltip("이 스텝 동안 허용되는 플레이어 행동")]
    [SerializeField] private TutorialAllow allowed = TutorialAllow.All;

    [Header("Presentation")]
    [Tooltip("하이라이트할 UI 대상 ID (TutorialManager의 매핑 테이블 참조, 비우면 없음)")]
    [SerializeField] private string highlightId;

    [Header("Scripted Draw")]
    [Tooltip("이 스텝 진입 시 예약할 드로우 카드 (순서대로 뽑힘, 비우면 랜덤). 튜토리얼 덱에 포함된 카드여야 한다")]
    [SerializeField] private CardData[] forcedDraws;

    public TutorialStepKind Kind => kind;
    public TutorialCompletion Completion => completion;
    public string[] TextKeys => textKeys;
    public TutorialAllow Allowed => allowed;
    public string HighlightId => highlightId;
    public CardData[] ForcedDraws => forcedDraws;
}
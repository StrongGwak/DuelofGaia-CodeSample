using UnityEngine;

/// <summary>
/// 카드 타입 및 시전 타입을 정의하는 Enum
/// </summary>
public enum CardType { Use, Spawn, Equipment, Passive, All }
/// <summary>
/// 카드의 정보를 가진 스크립터블 오브젝트 클래스
/// </summary>
[CreateAssetMenu(fileName = "NewCardData", menuName = "Scriptable Objects/CardData")]
public class CardData : GameData
{
    // 카드 타입
    [SerializeField] private CardType cardType;
    // 융합 카드인지 구분
    [SerializeField] private bool isDrawable;
    // drawPile에 들어가는 복사본 수 (많을수록 뽑힐 확률이 높아짐)
    [SerializeField] private int cardCount = 1;
    // 카드 버릴 시 드로우 포인트 획득 여부
    [SerializeField] private int drawPointFromDiscard; 
    // 카드에 연결된 스킬
    [SerializeField] private SkillData skillData;
    // 카드에 연결된 스킬
    [SerializeField] private SkillData passiveSkillData;

    public CardType CardType => cardType;
    public bool IsDrawable => isDrawable;
    public int CardCount => cardCount;
    public int DrawPointFromDiscard => drawPointFromDiscard;
    public SkillData SkillData => skillData;
    public SkillData PassiveSkillData => (passiveSkillData != null && passiveSkillData.Type == SkillType.Passive) ? passiveSkillData : null;
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 덱 테마를 정의하는 Enum
/// </summary>
[Flags]
public enum DeckTheme
{
    None = 0,
    Fire = 1 << 0, 
    Light = 1 << 1, 
    Water =  1 << 2, 
    Storm = 1 << 3, 
    Might =  1 << 4,
    Nature =  1 << 5, 
    Darkness =  1 << 6, 
    Death = 1 << 7,
    Red = 1 << 8, 
    Blue =  1 << 9, 
    Green =  1 << 10, 
    Black = 1 << 11,
    
    // 융합
    FireLight = Fire | Light,
    WaterStorm = Water | Storm,
    MightNature = Might | Nature,
    DeathDarkness = Death | Darkness,
}

/// <summary>
/// 덱 타입 정의하는 Enum
/// </summary>
public enum DeckType { All, Duel, Advanced, Fusion, Dragon }
/// <summary>
/// 덱의 정보를 가진 스크립터블 오브젝트 클래스
/// </summary>
[CreateAssetMenu(fileName = "NewDeckData", menuName = "Scriptable Objects/DeckData")]
public class DeckData : ScriptableObject
{
    [Header("DeckDesc")]
    //덱 테마
    [SerializeField] private DeckTheme theme;
    // 덱 타입
    [SerializeField] private DeckType type;
    // 덱 이름
    [SerializeField, TextArea] private string deckName;
    [Header("Cards")]
    // 카드 리스트
    [SerializeField] private List<CardData> cards;
    [Header("Sources")]
    [SerializeField] private DeckData advancedDeckData;
    [SerializeField] private DeckData fusionDeckData;
    // 융합 덱의 카드 조합 식
    [SerializeField] private List<FusionRecipe> fusionRecipes;
    
    [Header("UI")]
    [SerializeField] private Sprite deckIcon;
    [SerializeField] private Sprite indicatorSprite;
    [SerializeField] private Sprite spawnBackgroundSprite;
    
    public DeckTheme Theme => theme;
    public DeckType Type => type;
    public string DeckName => deckName;
    public List<CardData> Cards => cards;
    public List<FusionRecipe> FusionRecipes => fusionRecipes;
    public DeckData AdvancedDeckData => advancedDeckData;
    public DeckData FusionDeckData => fusionDeckData;
    public Sprite DeckIcon => deckIcon;
    public Sprite IndicatorSprite => indicatorSprite;
    public Sprite SpawnBackgroundSprite => spawnBackgroundSprite;

    // 덱 해금에 필요한 remainingUnlockConditions 반환 함수
    public int GetRequiredCount()
    {
        switch (Type)
        {
            // 융합 덱은 선행 덱이 2개 필요
            case DeckType.Fusion: return 2;
            // 강화 덱은 선행 덱이 1개 필요
            case DeckType.Advanced: return 1;
            default: return 0;
        }
    }

    // 덱 선택 시 필요한 포인트 반환 함수
    public int GetDeckPoint()
    {
        switch (Type)
        {
            // 강화 덱은 덱 포인트 2 필요
            case DeckType.Advanced: return 2;
            // 듀얼 및 융합 덱은 포인트 1 필요
            case DeckType.Fusion:
            default: return 1;
        }
    }

}

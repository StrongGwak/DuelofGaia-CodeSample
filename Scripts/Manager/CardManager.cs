using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 객체를 관리하는 카드 매니저 클래스
/// 과거 Player.cs와 함꼐 쓰인 매니저이지만 현재는 사용하지 않는다.
/// </summary>
public class CardManager : MonoBehaviour
{
    private static CardManager instance;

    private Dictionary<string, CardData> resultCardDataByCardNames = new();
    private Dictionary<CardData, List<CardData>> ingredientsByResult = new();
    public static CardManager Instance => instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        BuildFusionDictionary();
    }

    // 만약에 카드 조합 방식이 2장의 카드 조합이 아니라 여러장의 카드가 조합이 가능하다면
    // 핸드의 카드와 드로우한 카드를 조합해서 융합을 조합식으로 짜야함 nCk
    public CardData TryFusion(List<CardData> input)
    {
        string key = GenerateKey(input);
        return resultCardDataByCardNames.GetValueOrDefault(key);
    }

    private static string GenerateKey(List<CardData> cards)
    {
        // 1. 이름만 추출
        List<string> names = new();
        foreach (CardData card in cards)
        {
            names.Add(card.name);
        }

        // 2. 이름 정렬
        names.Sort();

        // 3. 하이픈으로 이어붙이기
        string key = string.Join("+", names);
        return key;
    }

    public List<CardData> GetIngredients(CardData resultCard)
        => ingredientsByResult.TryGetValue(resultCard, out var list) ? list : null;

    // 주의: 이 전역 딕셔너리는 레거시(Player.cs) 전용.
    // 실제 융합은 CardComponent가 자기 덱 기준으로 보유한 로컬 레시피로만 수행된다.
    public void BuildFusionDictionary()
    {
        foreach (DeckData deckData in DeckManager.Instance.SelectedDeckDatas)
        {
            if (deckData.Type != DeckType.Fusion && deckData.Type != DeckType.Dragon) continue;
            foreach (FusionRecipe fusionRecipe in deckData.FusionRecipes)
            {
                string key = GenerateKey(fusionRecipe.Ingredients);
                resultCardDataByCardNames[key] = fusionRecipe.Result;
                ingredientsByResult[fusionRecipe.Result] = fusionRecipe.Ingredients;
            }
        }
    }
    
    /*public CardData TryFusionWithTolerance(List<CardData> availableCards)
    {
        foreach (var kvp in resultCardDataByCardNames)
        {
            // "CardA+CardB+CardC" 형태의 키를 배열로 분할
            string[] requiredCards = kvp.Key.Split('+');

            // 매칭되는 카드 수 계산
            int matchCount = CountMatches(availableCards, requiredCards);

            // 부족한 카드 수와 매칭 비율 계산
            int missingCards = requiredCards.Length - matchCount;
            float matchRatio = (float)matchCount / requiredCards.Length;

            // 허용 조건 확인
            if (missingCards <= fusionTolerance.maxMissingCards &&
                matchRatio >= fusionTolerance.minMatchRatio)
            {
                return kvp.Value;
            }
        }

        return null;
    }*/
    
    // 용덱 6장 이상일 경우 가중치로 조합 하는 방법
    private int CountMatches(List<CardData> availableCards, string[] requiredCards)
    {
        // 사용 가능한 카드들의 고유한 이름들만 추출 (중복 제거)
        var uniqueAvailableNames = new HashSet<string>();
        foreach (CardData card in availableCards)
        {
            uniqueAvailableNames.Add(card.name);
        }

        int matches = 0;

        // 필요한 각 카드에 대해 확인
        foreach (string requiredCard in requiredCards)
        {
            // 고유한 카드 이름 중에 해당 카드가 있는지 확인
            if (uniqueAvailableNames.Contains(requiredCard))
            {
                matches++;
            }
        }

        return matches;
    }


}
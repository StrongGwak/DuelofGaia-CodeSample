using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class DiscardAction : EffectAction, IFloatingTextProvider
{
    [SerializeField] private int minDiscardCount;
    [SerializeField] private int maxDiscardCount;
    private int actualDiscardCount = 0;

    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        Debug.Log("다크핸드 실행");
        // 타겟 검증
        if (target == null || target.IsDead) return false;

        Duelist duelist = target as Duelist;
        if (duelist == null) return false;
        
        // CardComponent에서 핸드 카드 가져오기
        var cardComponent = duelist.GetComponent<CardComponent>();
        if (cardComponent == null) return false;

        var hands = cardComponent.Hands.Where(card => card.IsActive()).ToList();
        if (hands.Count == 0) return false;

        // 랜덤 카드 선택 및 버리기
        int discardCount = Random.Range(minDiscardCount, maxDiscardCount);
        actualDiscardCount = Mathf.Min(discardCount, hands.Count);
        for (int i = 0; i < actualDiscardCount; i++)
        {
            int randomIndex = Random.Range(0, hands.Count);
            var randomCard = hands[randomIndex];

            // 카드 버리기 실행
            cardComponent.Discard(randomCard);
            hands.RemoveAt(randomIndex);
        }

        return true;
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["minDiscardCount"] = minDiscardCount.ToString("0"),
            ["maxDiscardCount"] = maxDiscardCount.ToString("0"),
        };

        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new DiscardAction()
        {
            minDiscardCount = minDiscardCount,
            maxDiscardCount = maxDiscardCount,
        };
    }

    public bool TryBuildFloatingText(Effect effect, out FloatingTextData data)
    {
        data = default;
        var user = effect?.User;
        var target = effect?.Target;
        if (target == null || target.IsDead)
        {
            return false;
        }
        data.isShowValue = true; // Show the value
        data.value = actualDiscardCount;
        data.color = Color.blue; // Healing text color
        return true;
    }
}

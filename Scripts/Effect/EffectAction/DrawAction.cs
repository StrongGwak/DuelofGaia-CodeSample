using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DrawAction : EffectAction, IFloatingTextProvider
{
    [SerializeField] private int drawCount;
    [SerializeField] private bool isSpecificDeck;
    [SerializeField] private DeckData deckData;
    [SerializeField] private GameObject drawEffectPrefab;
    
    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        // 타겟이 null이거나 이미 죽은 상태인지 확인
        Duelist duelist = user as Duelist;
        if(duelist == null)
        {
            return false;
        }
        
        // 정해진 덱의 카드중에 뽑을 것인지
        if (isSpecificDeck)
        {
            if (deckData == null) return false;
            duelist.CardComponent.DrawCardsFromSpecificDeck(drawCount, deckData, drawEffectPrefab);
        }
        else
        {
            duelist.CardComponent.DrawCardWithoutPoint(drawCount, drawEffectPrefab);
        }
        
        return true;
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["drawCount"] = drawCount.ToString("0"),
        };
        
        if (isSpecificDeck && deckData != null)
        {
            descriptionValuesByKeyword["deckData"] = deckData.DeckName;
        }


        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new DrawAction()
        {
            drawCount = this.drawCount,
            isSpecificDeck = this.isSpecificDeck,
            deckData = this.deckData,
            drawEffectPrefab = this.drawEffectPrefab,
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
        data.value = drawCount;
        data.color = Color.white; // Healing text color
        return true;
    }
}

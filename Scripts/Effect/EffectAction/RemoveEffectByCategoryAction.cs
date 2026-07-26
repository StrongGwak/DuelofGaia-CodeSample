using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RemoveEffectByCategoryAction : EffectAction
{
    private enum EffectType {Heal, Damage}
    private enum TargetType {Target, Owner}
    // Target Effect가 가지고 있는 category
    [SerializeField]
    private CategoryData category;
    // Category를 가진 Effect들 중 가장 먼저 찾은 Effect만 지울 것인가? 모든 Effect를 지울 것인가?
    [SerializeField]
    private bool isRemoveAll;

    [SerializeField] 
    private bool isEffect;
    // 제거한 이펙트 개수만큼 Value를 얻을 건지
    [SerializeField]
    [ShowIf("isEffect", true)]
    private TargetType targetType;  
    [SerializeField]
    [ShowIf("isEffect", true)]
    private EffectType effectType;
    [SerializeField]
    [ShowIf("isEffect", true)]
    private float healthValue;
    [SerializeField]
    [ShowIf("isEffect", true)]
    private float manaValue;

    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if (target == null || target.IsDead) return false;
        int removeCount = target.SkillSystem.GetEffectCount(category);
        if (isRemoveAll)
            target.SkillSystem.RemoveEffectAll(category);
        else
            target.SkillSystem.RemoveEffect(category);


        if (isEffect)
        {
            BaseCharacter recipient = null;
            switch (targetType)
            {
                case TargetType.Owner:
                    recipient = user;
                    break;
                case TargetType.Target:
                    recipient = target;
                    break;
            }

            if (recipient == null) return false;
            switch (effectType)
            {
                case EffectType.Heal:
                    recipient.Stats.AddHealth(healthValue * removeCount);
                    recipient.Stats.AddSkillCost(manaValue * removeCount);
                    break;
                case EffectType.Damage:
                    recipient.TakeDamage(user, effect, healthValue * removeCount);
                    recipient.Stats.AddSkillCost(-manaValue * removeCount);
                    break;
            }
        }
        
        return true;
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["category"] = category.DisplayName,
            ["healthValue"] = healthValue.ToString(".##"),
            ["manaValue"] = manaValue.ToString(".##"),
        };

        return descriptionValuesByKeyword;
    }
    
    public override object Clone() => new RemoveEffectByCategoryAction()
    {
        category = category, 
        isRemoveAll = isRemoveAll,
        targetType = targetType,
        effectType = effectType,
        healthValue = healthValue,
        manaValue = manaValue,
    };
}

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReduceDamageAction : EffectAction
{
    private enum ResourceType { Mana }
    [SerializeField] private float reducePercentage;    // 전환할 데미지 비율
    [SerializeField] private float resourceRate;        // 데미지 1당 전환할 비용
    [SerializeField] private ResourceType resourceType; // 전환할 비용의 종류
    
    private Effect storedEffect;
    
    public override void Start(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if (target == null) return;
        storedEffect = effect;
        target.OnModifyDamage += OnModifyDamage;
    }

    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        return target != null && !target.IsDead;
    }

    public override void Release(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if (target == null) return;
        target.OnModifyDamage -= OnModifyDamage;
        storedEffect = null;
    }
    
    private float OnModifyDamage(BaseCharacter target, BaseCharacter instigator, object causer, float damage)
    {
        if (target == null || target.IsDead) return damage;
        if (instigator == null) return damage;

        // 즉사 카테고리는 무조건 감소 제외
        if (causer is Effect e && e.HasCategory("INSTANT_KILL"))
            return damage;

        float damageToAbsorb = damage * reducePercentage;
        float cost = damageToAbsorb * resourceRate;
        
        switch (resourceType)
        {
            case ResourceType.Mana:
                float currentMana = target.Stats.SkillCost;
                if (currentMana < cost)
                {
                    // 마나 부족: 가진 마나 모두 소진
                    target.Stats.AddSkillCost(-currentMana);

                    // 부족한 마나 비용을 데미지로 역전환
                    float unabsorbedManaCost = cost - currentMana;
                    float overflowDamage = unabsorbedManaCost / resourceRate;

                    // 효과 강제 종료
                    target.SkillSystem?.RemoveEffect(storedEffect);

                    return (damage - damageToAbsorb) + overflowDamage;
                }
                else
                {
                    // 마나 충분: 정상 차감
                    target.Stats.AddSkillCost(-cost);
                    return damage - damageToAbsorb;
                }

            default:
                return damage;
        }
    }
    
    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["reducePercentage"] = (reducePercentage * 100).ToString("0.#") + "%",
            ["resourceRate"] = resourceRate.ToString("0"),
        };

        return descriptionValuesByKeyword;
    }
    
    public override object Clone() => new ReduceDamageAction()
    {
        reducePercentage = reducePercentage,
        resourceRate = resourceRate,
        resourceType = resourceType,
    };
}

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamageByDiscardCountAction : EffectAction, IFloatingTextProvider
{
    [SerializeField] private float defaultDamage;

    private float totalDamage;

    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        // 타겟 검증
        if (target == null || target.IsDead) return false;
        
        totalDamage = defaultDamage * BattleManager.Instance.DiscardCount;
        target.TakeDamage(user, effect, totalDamage);

        return true;
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["defaultDamage"] = defaultDamage.ToString("0.##"),
        };

        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new DamageByDiscardCountAction()
        {
            defaultDamage = defaultDamage,
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
        data.value = totalDamage;
        data.color = Color.red; // Healing text color
        return true;
    }
}

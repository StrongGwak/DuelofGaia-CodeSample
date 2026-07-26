using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HealAction : EffectAction, IFloatingTextProvider, IExpectedHealProvider
{
    private enum HealType { Value, Rate }
    [SerializeField] private HealType healType;
    [SerializeField] 
    [ShowIf("healType", HealType.Value)]
    private float defaultValue;
    [SerializeField] 
    [ShowIf("healType", HealType.Rate)]
    private float rate;
    
    private float lastAppliedValue;

    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        // 타겟이 null이거나 이미 죽은 상태인지 확인
        if(target == null)
        {
            return false;
        }
        
        if(target.IsDead)
        {
            return false;
        }

        switch (healType)
        {
            case HealType.Value:
                lastAppliedValue = defaultValue;
                break;
            case HealType.Rate:
                lastAppliedValue = target.Stats.MaxHealth * rate;
                break;
        }
        target.Heal(user, effect, lastAppliedValue);
        return true;
    }

    /// <summary>적용 없이 예상 힐량만 계산한다. (타겟 필터 등 사전 판단용)</summary>
    public float GetExpectedHealAmount(BaseCharacter target)
    {
        if (target == null || target.Stats == null)
        {
            return 0f;
        }

        switch (healType)
        {
            case HealType.Value:
                return defaultValue;
            case HealType.Rate:
                return target.Stats.MaxHealth * rate;
            default:
                return 0f;
        }
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["defaultValue"] = defaultValue.ToString(".##"),
        };

        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new HealAction()
        {
            healType = healType,
            defaultValue = defaultValue,
            rate = rate,
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
        data.value = lastAppliedValue;
        data.color = Color.green; // Healing text color
        return true;
    }
}

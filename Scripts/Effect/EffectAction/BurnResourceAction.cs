using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BurnResourceAction : EffectAction
{
    [SerializeField] private float defaultValue;
    private float burnedValue;
    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        // 타겟이 null이거나 이미 죽은 상태인지 확인
        if (target == null || target.IsDead) return false;

        
        burnedValue = Mathf.Min(defaultValue, target.Stats.SkillCost);
        // 글씨가 두개 뜨는데 어색하면 주석의 코드 사용
        //target.ConsumeSkillCost(user, effect, burnedValue, false);
        target.ConsumeSkillCost(user, effect, burnedValue);
        target.TakeDamage(user, effect, burnedValue);
        return true;
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
        return new BurnResourceAction()
        {
            defaultValue = defaultValue,
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
        //data.value = lastAppliedValue;
        data.color = Color.blue; // Healing text color
        return true;
    }
}

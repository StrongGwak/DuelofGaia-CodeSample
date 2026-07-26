using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AddManaAction : EffectAction, IFloatingTextProvider
{
    private enum AddType { Value, Rate }
    [SerializeField] private AddType addType;
    [SerializeField] 
    [ShowIf("addType", AddType.Value)]
    private float defaultValue;
    [SerializeField] 
    [ShowIf("addType", AddType.Rate)]
    private float rate;
    private float lastAppliedValue;

    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        // 타겟이 null이거나 이미 죽은 상태인지 확인
        if(target == null)
        {
            Debug.LogError("RestoreAction.Apply: target is null");
            return false;
        }
        
        if(target.IsDead)
        {
            Debug.Log($"RestoreAction.Apply: target {target.name} is already dead. Restore not applied.");
            return false;
        }

        switch (addType)
        {
            case AddType.Value:
                lastAppliedValue = defaultValue;
                break;
            case AddType.Rate:
                lastAppliedValue = target.Stats.MaxHealth * rate;
                break;
        }
        target.RestoreSkillCost(user, effect, lastAppliedValue);
        //target.Stats.AddSkillCost(lastAppliedValue);
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
        return new AddManaAction()
        {
            addType = addType,
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
        data.color = Color.blue; // Healing text color
        return true;
    }
}

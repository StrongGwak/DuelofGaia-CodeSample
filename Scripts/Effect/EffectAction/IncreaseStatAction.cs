using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IncreaseStatAction : EffectAction
{
    [SerializeField]
    private Stat stat;
    // 새로 추가: 증가를 비율로 할 것인가?
    [SerializeField]
    private bool isPercentage;
    // 새로 추가: 비율 기준값을 MaxValue로 할지, 현재값(Value)로 할지
    [SerializeField]
    [ShowIf("isPercentage", true)]
    private bool useMaxValueAsBase = true;

    [SerializeField]
    [Tooltip("isPercentage가 true일 때는 소수점(%)으로 사용")]
    private float defaultValue;
    // MaxValue도 늘릴 것인가?
    [SerializeField]
    private bool isIncreaseMaxValue;
    // 적용한 값을 Release할 때 되돌릴 것인가?
    [SerializeField]
    private bool isUndoOnRelease = true;

    
    public override void Start(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        
    }
    
    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if(target == null ||  target.IsDead)
        {
            return false;
        }

        //Debug.Log(stat.Data.CodeName +"is increase to " +target.ToString());

        if (!target.Stats.TryGetStat(stat.Data, out var result)) return false;

        // 증가량 계산
        float amount;
        if (isPercentage)
        {
            var runtimeStat = target.Stats.GetStat(stat.Data);

            float baseValue;
            if (useMaxValueAsBase)
            {
                // HP/마나류 리소스에 적합
                baseValue = runtimeStat.MaxValue;
            }
            else
            {
                // 이동속도/공격력 등 능력치에 적합
                baseValue = runtimeStat.CurrentValue;
                // 필요하면 DefaultValue 기준으로 바꿔도 됨:
                // baseValue = runtimeStat.DefaultValue;
                // 상황에 따라서는 CurrentValue + BonusValue인 Value로 바꿔도 됨
            }

            // defaultValue: 20f => 20% 증가
            amount = baseValue * (defaultValue);
        }
        else
        {
            amount = defaultValue;
        }


        if (isIncreaseMaxValue)
        {
            
            target.Stats.SetMaxBonusValue(stat.Data, effect, amount);
            if (stat.Data == target.CharacterData.HpStat.Data)
            {
                target.Stats.AddHealth(amount);
            }
        }
        else
        {
            target.Stats.SetBonusValue(stat.Data, effect, amount);
        }
        return true;
    }

    public override void Release(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if (!isUndoOnRelease) return;
        if (target)
        {
            if (!target.Stats.TryGetStat(stat.Data, out var result)) return;
            
            if (isIncreaseMaxValue)
            {
                target.Stats.RemoveMaxBonusValue(stat.Data, effect);
            }
            else
            {
                target.Stats.RemoveBonusValue(stat.Data, effect);
            }
        }
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        string displayValue = isPercentage
            ? (defaultValue * 100f).ToString("0.##") + "%"
            : defaultValue.ToString("0.##");

        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            { "stat", stat.Data.DisplayName },
            { "defaultValue", displayValue },
        };

        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new IncreaseStatAction()
        {
            stat = stat,
            isPercentage = isPercentage,
            useMaxValueAsBase = useMaxValueAsBase,
            defaultValue = defaultValue,
            isIncreaseMaxValue = isIncreaseMaxValue,
            isUndoOnRelease = isUndoOnRelease
        };
    }
}

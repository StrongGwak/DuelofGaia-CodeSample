using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DrainHealthAction : EffectAction, IFloatingTextProvider
{
    private enum ApplyType { Value, Rate }
    private enum RateType {Total, Current}
    [SerializeField] private ApplyType applyType;
    [SerializeField] 
    [ShowIf("applyType", ApplyType.Rate)]
    private RateType rateType;

    // 수치 그대로 적용
    [SerializeField] 
    [ShowIf("applyType", ApplyType.Value)]
    private float value;
    
    // % 비율로 적용
    [SerializeField]
    [ShowIf("applyType", ApplyType.Rate)]
    private float rate;
    // 대상의 최대 체력만큼 회복할건지
    [SerializeField]
    [ShowIf("applyType", ApplyType.Rate)]
    private bool isGetMaxHealth;
    
    [SerializeField]
    private StatData bonusDamageStatData;
    
    float drainedHealth = 0f;
    
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

        switch (applyType)
        {
            case ApplyType.Value:
                if (bonusDamageStatData) value += user.Stats.GetValue(bonusDamageStatData);
                drainedHealth = Mathf.Min(value, target.Stats.Health);
                break;
            
            case ApplyType.Rate:
                switch (rateType)
                {
                    case RateType.Total:
                        // 전체 체력의 n%의 데미지
                        drainedHealth = Mathf.Min(target.Stats.Health, target.Stats.MaxHealth * rate);
                        break;
                    case RateType.Current:
                        // 현재 체력의 n%의 데미지
                        drainedHealth = target.Stats.Health * rate;
                        break;
                }
                break;
        }
        
        var appliedDamage = target.TakeDamage(user, effect, drainedHealth);

        if (isGetMaxHealth)
        {
            // 흡혈 회복도 캐릭터 단에서 처리(FT/이벤트 일괄)
            user.Heal(user, effect, target.Stats.MaxHealth);
        }
        else
        {
            // 기존 Stats.AddHealth 대신 Heal 경로로 통일(FT/이벤트 일괄)
            user.Heal(user, effect, appliedDamage);
        }
        return true;
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["healthValue"] = value.ToString(".##"),
            ["healthRate"] = (rate * 100).ToString("%"),
        };

        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new DrainHealthAction()
        {
            applyType = applyType,
            rateType = rateType,
            value = value,
            rate = rate,
            bonusDamageStatData = bonusDamageStatData,
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
        data.value = drainedHealth;
        data.color = Color.red; // Healing text color
        return true;
    }
}
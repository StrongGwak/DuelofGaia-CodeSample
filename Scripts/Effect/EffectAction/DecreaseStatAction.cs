using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DecreaseStatAction : EffectAction, IFloatingTextProvider
{
    [SerializeField]
    private Stat stat;
    // 새로추가: 감소를 비율로 할 것인가?
    [SerializeField]
    private bool isPercentage;
    [SerializeField]
    [ShowIf("isPercentage", true)]
    private bool useMaxValueAsBase;
    [SerializeField]
    [Tooltip("isPercentage로 사용할 경우에 소수점값으로 설정")]
    private float defaultValue;
    // MaxValue도 늘릴 것인가?
    [SerializeField]
    private bool isDecreaseMaxValue;
    // 적용한 값을 Release할 때 되돌릴 것인가?
    [SerializeField]
    private bool isUndoOnRelease = true;
    
    // 새로 추가: 점진적 회복 관련
    [SerializeField] private bool isGradualRestore = false;
    // (기존 방식) 고정 틱당 회복량 사용 여부
    [SerializeField]
    [ShowIf("isGradualRestore", true)]
    private bool isAutoRatioRestore = true;
    [SerializeField]
    [Tooltip("isAutoRatioRestore가 False일 경우에 고정 회복량")]
    [ShowIf("isGradualRestore", true)]
    private float restorePerTick = 1f; // 틱당 회복량

    private float decreasedAmount; // 실제 감소시킨 양
    private float restoredAmount;  // 현재까지 회복한 양

    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if(target == null ||  target.IsDead)
        {
            return false;
        }

        // 첫 Apply에서 감소
        if (restoredAmount == 0f && decreasedAmount == 0f)
        {
            float amount;

            var runtimeStat = target.Stats.GetStat(stat.Data);

            if(runtimeStat == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[DecreaseStatAction] Target does not have stat: {stat.Data.DisplayName}");
#endif
                return false;
            }

            if (isPercentage)
            {
                // 기준값 결정
                float baseValue;
                if (useMaxValueAsBase)
                {
                    // Max 기준 (HP, 마나 같은 리소스형에 적합)
                    baseValue = runtimeStat.MaxValue;
                }
                else
                {
                    
                    // 현재 or 기본값 기준 (이동속도, 공격력 같은 능력치에 적합)
                    // 둘 중 하나 선택해서 사용: Value(보너스 포함 실제 값) 또는 DefaultValue
                    baseValue = runtimeStat.CurrentValue;
                    // baseValue = runtimeStat.DefaultValue;
                }

                // defaultValue: 0.2f => 20% 감소
                amount = baseValue * defaultValue;
            }
            else
            {
                amount = defaultValue;
            }

            decreasedAmount = amount;
            restoredAmount = 0f;

            if (isDecreaseMaxValue)
                target.Stats.SetMaxBonusValue(stat.Data, effect, -decreasedAmount);
            else
                target.Stats.SetBonusValue(stat.Data, effect, -decreasedAmount);
            return true;
        }

        // 점진적 회복 처리
        if (isGradualRestore && restoredAmount < decreasedAmount)
        {
            float remain = decreasedAmount - restoredAmount; // 아직 복구 안 된 양
            if (remain <= 0f)
                return true;

            float restoreAmount;

            if (isAutoRatioRestore && !effect.IsInfinitelyApplicable)
            {
                // 남은 틱(적용 횟수) 계산
                int remainingTicks = effect.ApplyCount - effect.CurrentApplyCount;
                if (remainingTicks <= 0)
                {
                    // 안전장치: 남은 틱 정보가 이상하면 그냥 전부 복구
                    restoreAmount = remain;
                }
                else
                {
                    // 남은 틱 수로 나눠서, 매 Apply마다 딱 맞게 분배
                    restoreAmount = remain / remainingTicks;
                }
            }
            else
            {
                // 기존 방식: 고정 틱당 회복량 사용
                restoreAmount = Mathf.Min(restorePerTick * scale, remain);
            }

            if (restoreAmount > 0f)
            {
                restoredAmount += restoreAmount;

                // 전체 감소량에서 누적 회복량을 뺀 값을 현재 보너스로 세팅 (-decreasedAmount ~ 0 사이)
                float bonusValue = -decreasedAmount + restoredAmount;

                if (isDecreaseMaxValue)
                    target.Stats.SetMaxBonusValue(stat.Data, effect, bonusValue);
                else
                    target.Stats.SetBonusValue(stat.Data, effect, bonusValue);

                // 디버그용
                //Debug.Log($"[DecreaseStatAction] restoredAmount={restoredAmount}, remain={remain - restoreAmount}, bonusValue={bonusValue}");
            }

        }

        return true;
    }

    public override void Release(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if (!isUndoOnRelease) return;
        if (target)
        {
            var runtimeStat = target.Stats.GetStat(stat.Data);

            if(runtimeStat == null)
            {
                Debug.LogWarning($"[DecreaseStatAction] Target does not have stat: {stat.Data.DisplayName}");
                return;
            }
            
            if (isDecreaseMaxValue)
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
        return new DecreaseStatAction()
        {
            stat = stat,
            defaultValue = defaultValue,
            isPercentage = isPercentage,
            useMaxValueAsBase = useMaxValueAsBase,
            isDecreaseMaxValue = isDecreaseMaxValue,
            isUndoOnRelease = isUndoOnRelease,
            isGradualRestore = isGradualRestore,
            isAutoRatioRestore = isAutoRatioRestore,
            restorePerTick = restorePerTick,
        };
    }

    public bool TryBuildFloatingText(Effect effect, out FloatingTextData data)
    {
        data = default;

        var user = effect?.User;
        var target = effect?.Target;
        if (!user || !target || target.IsDead) return false;

        data.isShowValue = false;
        data.color = Color.black;
        return true;
    }
}

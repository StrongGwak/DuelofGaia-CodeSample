using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChangeAttackRangeAction : EffectAction, IFloatingTextProvider
{
    [SerializeField]
    private float appliedScale;
    // 적용한 값을 Release할 때 되돌릴 것인가?
    [SerializeField]
    private bool isUndoOnRelease = true;

    private float defaultScale = 1;
    private bool applied = false;

    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if(target == null ||  target.IsDead)
        {
            Debug.LogError("DecreaseStatAction.Apply: target is null");
            return false;
        }
        
        var monster = target.GetComponent<Monster>();
        var monsterData = target.CharacterData as MonsterData;
        if (monster == null || monsterData == null ) return false;
        var baseAttackSkill = monster.SkillSystem.FindById(monsterData.BaseAttack.ID);
        if (baseAttackSkill == null) return false;
        
        var selectAction = baseAttackSkill.TargetSearcher.SelectionAction;
        selectAction.SetIsUseScale(true);
        selectAction.SetScale(appliedScale);

        if(monster.RangeDetector is RangeDetectorComponent rd)
        {
            rd.SetAttackRangeScale(appliedScale, true);
        }

        applied = true;

        return true;
    }

    public override void Release(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if (!isUndoOnRelease || !applied) return;
        if (target == null) return;
        foreach (var runningEffect in target.SkillSystem.RunningEffects)
        {
            if(runningEffect == effect) continue;
            // 동일한 효과가 아직 남아있다면
            if (runningEffect.Action is ChangeAttackRangeAction)
            {
                // 그리고 그 효과가 끝나지 않았다면 Release하지 않음
                if (!runningEffect.IsFinished) return;
            }
        }
        var monster = target.GetComponent<Monster>();
        var monsterData = target.CharacterData as MonsterData;
        var baseAttackSkill = monster.SkillSystem.FindById(monsterData.BaseAttack.ID);

        if (monster == null || monsterData == null || baseAttackSkill == null) return;
        var selAct = baseAttackSkill.TargetSearcher.SelectionAction;
        selAct.SetIsUseScale(false);
        selAct.SetScale(defaultScale);
        if (monster.RangeDetector is RangeDetectorComponent rd)
        {
            rd.ResetAttackRangeScale(rescan: true);
        }

    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            { "scale", appliedScale.ToString("0") },
        };

        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new ChangeAttackRangeAction()
        {
            appliedScale = this.appliedScale,
            isUndoOnRelease = this.isUndoOnRelease
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

using UnityEngine;

[System.Serializable]
public class ApplyEffectsByTargetAction : SkillAction
{
    public override void Apply(Skill skill)
    {
        if (skill == null)
        {
            Debug.LogError($"InstantApplyAction: Target is null for skill {skill.CodeName}. Cannot apply action.");
        }
        
        if(skill.Targets == null ||  skill.Targets.Count <= 0)
        {             
            // 대상이 없다면 효과 없음
            return;
        }

        if (skill.Owner != null && !skill.Owner.IsDead)
        {
            skill.Owner.SkillSystem.ApplyEffectsByTargetType(skill, EffectTargetType.User);
        }
        
        foreach (var target in skill.Targets)
        {
            if (target == null)
            {
                Debug.LogError($"InstantApplyAction: Target is null for skill {skill.CodeName}. Cannot apply action123.");
            }

            target.SkillSystem.ApplyEffectsByTargetType(skill, EffectTargetType.Target);
        }

            
    }

    public override object Clone() => new ApplyEffectsByTargetAction();
}

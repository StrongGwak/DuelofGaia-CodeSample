using UnityEngine;

[System.Serializable]
public class SacrificeAction : SkillAction
{
    public override void Apply(Skill skill)
    {
        if (skill == null)
        {
            Debug.LogError($"InstantApplyAction: Target is null for skill {skill.CodeName}. Cannot apply action.");
        }
        if(skill.Targets == null)
        {             
            Debug.LogError($"InstantApplyAction: Targets are null for skill {skill.CodeName}. Cannot apply action12.");
        }
        // 1. SelectResult (선택된 타겟)를 희생
        var selectedTarget = skill.TargetSelectionResult.selectedTarget;
        if (selectedTarget != null)
        {
            BaseCharacter target = selectedTarget.GetComponent<BaseCharacter>();
            target.SkillSystem.Apply(skill);
        }

        // 2. skill.Targets에서 랜덤으로 선택해서 즉사
        if (skill.Targets != null && skill.Targets.Count > 0)
        {
            foreach (var target in skill.Targets)
            {
                Debug.Log($"{target.name} - {skill.CodeName} - {skill.TargetSelectionResult.selectedTarget.name}");
            }
            var randomTarget = skill.Targets[UnityEngine.Random.Range(0, skill.Targets.Count)];
            randomTarget.SkillSystem.Apply(skill);
            Debug.Log($"random target name : {randomTarget.CharacterData.CodeName}");
            
        }

    }

    public override object Clone() => new SacrificeAction();
}

using UnityEngine;

[System.Serializable]
public class PossessAction : SkillAction
{
    public override void Apply(Skill skill)
    {
        if (skill.Targets == null)
        {
            Debug.LogError($"PossessAction: Target is null for skill {skill.CodeName}.");
        }
        
        foreach (var target in skill.Targets)
        {
            target.ChangeTeam(skill.Owner.TeamCategory);
        }
    }
    
    public override object Clone() => new PossessAction();
}

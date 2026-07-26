using UnityEngine;

[System.Serializable]
public class ChangeCardAction : SkillAction
{
    public override void Apply(Skill skill)
    {
        if (skill.Owner is not Duelist duelist) return;
    }
    
    public override object Clone() => new ChangeCardAction();
}

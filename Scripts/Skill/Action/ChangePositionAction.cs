using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ChangePositionAction : SkillAction
{
    public enum ChangeType
    {
        ToAlliedDuelist,    // 몬스터와 같은 팀의 듀얼리스트 앞으로
        ToEnemyDuelist      // 상대 팀 듀얼리스트 앞으로 (몬스터 팀과 무관하게)
    }

    [SerializeField] private float distance = 3f;
    [SerializeField] private ChangeType changeType = ChangeType.ToEnemyDuelist;
    public override void Apply(Skill skill)
    {
        if (skill == null)
        {
            Debug.LogError($"InstantApplyAction: Target is null for skill {skill.CodeName}. Cannot apply action.");
        }
        
        foreach (var target in skill.Targets)
        {
            List<Duelist> targetDuelists = changeType switch
            {
                ChangeType.ToAlliedDuelist => GetSameTeamDuelists(target),  // 대상(몬스터)과 같은 팀
                ChangeType.ToEnemyDuelist => BattleManager.Instance.GetEnemyDuelists(skill.Owner), // 스킬 사용자의 적
                _ => new List<Duelist>()
            };

            if (targetDuelists.Count == 0) continue;

            var randomDuelist = targetDuelists[Random.Range(0, targetDuelists.Count)];
            target.transform.position = randomDuelist.transform.position + randomDuelist.transform.forward * distance;
        }
    }
    
    private List<Duelist> GetSameTeamDuelists(BaseCharacter monster)
    {
        var monsterTeam = monster.TeamCategory;
        return BattleManager.Instance.ActiveDuelists.Where(d =>
                !d.IsDead &&
                ReferenceEquals(d.TeamCategory, monsterTeam)  // 몬스터와 같은 팀
        ).ToList();
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword()
    {
        var dictionary = new Dictionary<string, string>()
        {
            { "distance", distance.ToString("#.00") },
        };
        return dictionary;
    }
    
    public override object Clone()
    {
        return new ChangePositionAction()
        {
            distance = distance,
            changeType = changeType,
        };
    }
}

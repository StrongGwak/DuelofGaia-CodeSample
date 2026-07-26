using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterSpawnAction : EffectAction
{
    // 릴리즈 시 몬스터를 소환하는 이펙트 액션
    [SerializeField] private MonsterData monsterData;
    [SerializeField] private int spawnCount = 1;
    
    public override void Start(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        
    }
    
    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if(target == null ||  target.IsDead)
        {
            return false;
        }

        return true;
    }

    public override void Release(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if(target== null || !target.IsDead)
        {
            return;
        }
        for (int i = 0; i < spawnCount; i++)
        {
            BaseCharacter monster = SpawnManager.Instance.SpawnCharacter(monsterData, user.TeamCategory, target.transform.position);
        }
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["monsterName"] = monsterData?.DisplayName ?? "???",
            ["spawnCount"] = spawnCount.ToString("0"),
        };

        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new MonsterSpawnAction()
        {
            monsterData = monsterData,
            spawnCount = spawnCount,
        };
    }
}

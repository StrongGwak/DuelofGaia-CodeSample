using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProcOnAttackAction : EffectAction
{
    [SerializeField] private float procChance;
    [SerializeField] private bool onlyTargetMonsters = true;

    public override void Start(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        target.OnBaseAttack += OnAttack;
    }
    
    public override bool Apply(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        if (target == null || target.IsDead) return false;

        return true;
    }

    public override void Release(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        // null 체크 추가
        if (target != null)
        {
            target.OnBaseAttack -= OnAttack;
        }

    }

    private void OnAttack(BaseCharacter user, BaseCharacter target, object causer)
    {
        
        if (onlyTargetMonsters && !(target is Monster)) return;
        // 확률 계산
        float randomValue = Random.Range(0f, 100f);
        
        if (randomValue <= procChance)
        {
            // 즉사 처리 - 현재 체력만큼 데미지
            float instantKillDamage = target.Stats.Health;
            target.TakeDamage(user, this, instantKillDamage);
        }
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["procChance"] = procChance.ToString("0.#"),
        };

        return descriptionValuesByKeyword;
    }
    
    public override object Clone() => new ProcOnAttackAction()
    {
        procChance = procChance,
        onlyTargetMonsters = onlyTargetMonsters,
    };
}

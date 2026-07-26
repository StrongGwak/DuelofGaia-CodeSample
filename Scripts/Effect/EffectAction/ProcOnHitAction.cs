using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProcOnHitAction : EffectAction 
{
    private enum HitType { ReduceDamage }
    [SerializeField] private float procChance;
    [SerializeField] private float defaultValue;
    [SerializeField] private bool isOnlyBaseAttack;
    [SerializeField] private HitType hitType;

    public override void Start(Effect effect, BaseCharacter user, BaseCharacter target, int level, float scale)
    {
        target.OnModifyDamage += OnModifyDamage;
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
            target.OnModifyDamage -= OnModifyDamage;
        }
    }
    
    private float OnModifyDamage(BaseCharacter entity, BaseCharacter instigator, object causer, float damage)
    {
        // 확률 계산
        float randomValue = Random.Range(0f, 100f);
        // procChance 확률 체크
        if (randomValue > procChance) return damage;

        // BaseAttack 전용 옵션이 켜져있다면 체크
        if (isOnlyBaseAttack)
        {
            Effect effect = causer as Effect;
            if (effect == null || !IsBaseAttackEffect(effect))
                return damage; // 원래 데미지 반환
        }

        switch (hitType)
        {
            case HitType.ReduceDamage:
                return Mathf.Max(0, damage - defaultValue);
            default:
                return damage;
        }
    }
    
    private bool IsBaseAttackEffect(Effect effect)
    {
        // BaseAttack 카테고리를 가진 Effect인지 확인
        return effect.HasCategory("BASE_ATTACK");
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect)
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
            ["procChance"] = procChance.ToString("0.#"),
            ["defaultValue"] = defaultValue.ToString("0.#"),
        };

        return descriptionValuesByKeyword;
    }
    
    public override object Clone() => new ProcOnHitAction()
    {
        procChance = procChance,
        defaultValue = defaultValue,
        isOnlyBaseAttack = isOnlyBaseAttack,
        hitType = hitType,
    };
}

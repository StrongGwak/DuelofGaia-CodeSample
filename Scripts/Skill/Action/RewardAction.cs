using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class RewardAction : SkillAction
{
    enum RewardTriggerType 
    { 
        OnSummon,              // 유닛 소환 시
        OnItemEquip,           // 장비 장착 시
        OnEnemyKill,           // 적 소환물 처치 시 (특정 스킬로)
        OnSkillCast,           // 주문 시전 시
        OnCardDraw,            // 카드 드로우 시
        OnSummonOrEquip,       // 소환 또는 장착 시 (융합용)
        OnPassive,             // 지속 효과 (체력 재생)
        OnEnchantCast,         // 인첸트 카드 시전 시
    }
    [SerializeField] private RewardTriggerType triggerType;
    [SerializeField] private List<SkillData> targetSkillDatas;

    private Duelist duelist;
    private Skill baseSkill;
    private Action unsubscribeAction;

    public override void Apply(Skill skill)
    {
        
    }
    
    public override void Activate(Skill skill)
    {
        baseSkill = skill;
        duelist = skill.Owner.GetComponent<Duelist>();
        if (duelist == null) return;

        CardController.ItemEquipHandler equipHandler;
        CardController.SpawnHandler spawnHandler;
        switch (triggerType)
        {
            case RewardTriggerType.OnEnemyKill:
                StatisticsComponent.KilledHandler killHandler = TriggerReward;
                duelist.StatisticsComponent.OnEnemyKilled += killHandler;
                unsubscribeAction = () => duelist.StatisticsComponent.OnEnemyKilled -= killHandler;
                break;
            case RewardTriggerType.OnSummon:
                spawnHandler = () => TriggerReward(skill.Owner, skill);
                foreach (var cc in duelist.CardComponent.Hands) cc.OnSpawn += spawnHandler;
                unsubscribeAction = () => { foreach (var cc in duelist.CardComponent.Hands) cc.OnSpawn -= spawnHandler; };
                break;
            case RewardTriggerType.OnItemEquip:
                equipHandler = () => TriggerReward(skill.Owner, skill);
                foreach (CardController cc in duelist.CardComponent.Hands) cc.OnItemEquip += equipHandler;
                unsubscribeAction = () => { foreach (var cc in duelist.CardComponent.Hands) cc.OnItemEquip -= equipHandler; };
                break;
            case RewardTriggerType.OnSkillCast:
                CardController.SkillCastHandler castHandler = (usedSkill) => TriggerReward(skill.Owner, usedSkill);
                foreach (CardController cc in duelist.CardComponent.Hands) cc.OnSkillCast += castHandler;
                unsubscribeAction = () => { foreach (var cc in duelist.CardComponent.Hands) cc.OnSkillCast -= castHandler; };
                break;
            case RewardTriggerType.OnCardDraw:
                CardComponent.DrawHandler drawHandler = () => TriggerReward(skill.Owner, skill);
                duelist.CardComponent.onDrawed += drawHandler;
                break;
            case RewardTriggerType.OnSummonOrEquip:
                spawnHandler = () => TriggerReward(skill.Owner, skill);                                                                                                                                      
                equipHandler = () => TriggerReward(skill.Owner, skill);                                                                                                                                  
                foreach (var cc in duelist.CardComponent.Hands)                                                                                                                                                                        
                {
                    cc.OnSpawn += spawnHandler;
                    cc.OnItemEquip += equipHandler;
                }
                unsubscribeAction = () =>
                {
                    foreach (var cc in duelist.CardComponent.Hands)
                    {
                        cc.OnSpawn -= spawnHandler;
                        cc.OnItemEquip -= equipHandler;
                    }
                };
                break;
                
        }
    }

    public override void Deactivate(Skill skill)
    {
        unsubscribeAction?.Invoke();
        unsubscribeAction = null;
        duelist = null;
    }
    
    private void TriggerReward(BaseCharacter target, Skill skill)
    {
        // 특정 스킬로만 발동하는 경우 필터링
        if (targetSkillDatas.Count > 0)
        {
            if (skill == null || !IsTargetSkill(skill))
            {
                return; // 조건 불만족 시 보상 없음
            }
                    
        }
        target.SkillSystem.Apply(baseSkill);
    }
    
    private bool IsTargetSkill(Skill killingSkill)
    {
        return targetSkillDatas.Any(skillData => killingSkill.ID == skillData.ID);
    }
    
    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword()
    {
        return new Dictionary<string, string>();
        /*var dictionary = new Dictionary<string, string>()
        {
            
        };
        return dictionary;*/
    }
    
    public override object Clone()
    {
        return new RewardAction()
        {
            triggerType = triggerType,
            targetSkillDatas = targetSkillDatas,
        };
    }
}

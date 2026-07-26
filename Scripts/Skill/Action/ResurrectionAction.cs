using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResurrectionAction : SkillAction
{
    [Header("Resurrection Settings")]
    [SerializeField] private int targetCount = 3;
    [SerializeField, Range(0f, 1f)] private float healthRate = 0.2f;

    [Header("Effect (Optional)")]
    [SerializeField] private GameObject skillObjectPrefab;

    private CategoryData usingCorpseCategory;

    public override void Apply(Skill skill)
    {
        if (!ValidateSkill(skill)) return;

        if (!usingCorpseCategory)
            usingCorpseCategory = Managers.Data.GetByCode<CategoryData>("USING_CORPSE");

        var spawnMgr = SpawnManager.Instance;
        if (spawnMgr == null)
        {
            Debug.LogWarning("ResurrectionAction: SpawnManager.Instance 없음");
            return;
        }

        //Debug.Log($"ResurrectionAction: resurrect up to {targetCount} targets (hpRate={healthRate}).");

        int resurrected = 0;
        for (int i = 0; i < skill.Targets.Count && resurrected < targetCount; i++)
        {
            var target = skill.Targets[i];
            if (!target) continue;

            // 죽은 대상만 부활 처리(살아있으면 스킵)
            if (!target.IsDead)
            {
                // 필요 시 정책 변경 가능: 살아있으면 힐 적용 등
                continue;
            }

            Vector3 pos = target.transform.position;
            Quaternion rot = target.transform.rotation;

            // 기존 시체 디스폰(풀로 되돌림)
            var corpseMarker = target.GetComponentInChildren<CorpseMarkerComponent>();
            if (corpseMarker)
            {
                if(corpseMarker.Used) continue;
                corpseMarker.Used = true;
            }

            // 이펙트 즉시 재생
            if (skillObjectPrefab != null)
                GameObject.Instantiate(skillObjectPrefab, pos, Quaternion.identity);

            // target이 AI에 의해 예약된 상태라면 해제 (플레이어/긴급 사용이 우선)
            if (usingCorpseCategory != null && target.HasCategory(usingCorpseCategory))
                target.RemoveCategory(usingCorpseCategory);

            spawnMgr.DespawnCharacter(target);

            // 동일한 데이터/팀으로 새 캐릭터 스폰
            var charData = target.CharacterData;
            var team = skill.Owner.TeamCategory;

            var newChar = spawnMgr.SpawnCharacter(charData, team, pos, rot);
            if (!newChar)
            {
                Debug.LogError("ResurrectionAction: 부활 스폰 실패");
                continue;
            }

            // 초기 체력 설정
            var maxHp = newChar.Stats != null ? newChar.Stats.MaxHealth : 0f;
            var reviveHp = Mathf.Max(1f, maxHp * Mathf.Clamp01(healthRate));
            newChar.Stats.ForceSetHealth(reviveHp, true);

            resurrected++;
        }
    }

    /// <summary>
    /// Start와 Apply에서 공통으로 사용하는 유효성 검사 메서드
    /// </summary>
    private bool ValidateSkill(Skill skill)
    {
        if (skill == null || skill.Owner == null)
        {
            Debug.LogError("ResurrectionAction: skill 또는 owner가 null입니다.");
            return false;
        }

        if (skill.Targets == null || skill.Targets.Count == 0)
        {
            Debug.LogWarning($"ResurrectionAction: 대상 없음. Skill={skill.CodeName}");
            return false;
        }

        return true;
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword()
    {
        var dictionary = new Dictionary<string, string>()
        {
            { "targetCount", targetCount.ToString() },
            { "healthRate", (healthRate * 100f).ToString("##%") },
        };
        return dictionary;
    }

    public override object Clone()
    {
        return new ResurrectionAction()
        {
            targetCount = targetCount,
            healthRate = healthRate,
            skillObjectPrefab = skillObjectPrefab,
            usingCorpseCategory = usingCorpseCategory
        };
    }
}
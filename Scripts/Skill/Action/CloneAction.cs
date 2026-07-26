using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI; // NavMesh 기능을 위해 추가

[System.Serializable]
public class CloneAction : SkillAction
{
    [SerializeField] private int spawnCount = 1;
    [SerializeField] private float spawnRadius = 1f;
    [SerializeField] private GameObject cloneVFX;

    [Header("Spawn Settings")]
    [SerializeField] private float randomOffset = 3f; // 랜덤 소환 범위
    [SerializeField] private float collisionRadius = 0.5f; // 충돌 체크 반경
    [SerializeField] private float navMeshSearchRadius = 5f; // NavMesh 검색 반경
    [SerializeField] private LayerMask obstacleLayer; // 장애물 레이어

    [Header("Clone Options")]
    [SerializeField] private bool copyRunningEffects = true; // 실행 중인 이펙트 복제 여부
    [SerializeField] private bool copyHealth = true; // 현재 체력 복제 여부

    public override void Apply(Skill skill)
    {
        SpawnManager spawnManager = SpawnManager.Instance;
        if (spawnManager == null) return;

        Monster targetMonster = skill.Targets[0] as Monster;
        if (targetMonster == null) return;
        Vector3 targetPosition = targetMonster.transform.position;
        GameObject.Instantiate(cloneVFX, targetMonster.transform);

        // 원본 상태 스냅샷 (소환 전에 캡처)
        float targetTakenDamage = targetMonster.Stats.MaxHealth - targetMonster.Stats.Health;

        // 소환 전에 원본 이펙트를 리스트로 캐싱 (RunningEffects는 매번 ToArray()하므로 1회만 호출)
        List<Effect> sourceEffects = copyRunningEffects
            ? new List<Effect>(targetMonster.SkillSystem.RunningEffects)
            : null;

        // 각 위치에 몬스터 소환
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomPos = GetRandomPositionWithCollisionCheck(targetPosition);
            Vector3 spawnPos = GetSafePositionWithNavMesh(randomPos);

            BaseCharacter monster = spawnManager.SpawnCharacter(targetMonster.MonsterData, skill.Owner.TeamCategory, spawnPos);
            GameObject.Instantiate(cloneVFX, monster.transform);

            // 실행 중인 이펙트를 상태 포함 복제
            if (sourceEffects != null)
            {
                // 복제 몬스터가 이미 보유한 이펙트 ID와 각 ID별 개수를 수집
                // (패시브/버프 등으로 자동 적용된 것 + IsAllowDuplicate로 스택된 것)
                Dictionary<int, int> existingEffectCounts = new Dictionary<int, int>();
                foreach (Effect e in monster.SkillSystem.RunningEffects)
                {
                    if (existingEffectCounts.ContainsKey(e.ID))
                    {
                        existingEffectCounts[e.ID]++;
                    }
                    else
                    {
                        existingEffectCounts[e.ID] = 1;
                    }
                }

                // 원본 이펙트별로 이미 적용된 수만큼 차감하고 나머지만 복제
                Dictionary<int, int> sourceEffectCounts = new Dictionary<int, int>();
                foreach (Effect effect in sourceEffects)
                {
                    if (effect.IsReleased) continue;

                    // 해당 ID의 소스 카운트 추적
                    if (!sourceEffectCounts.ContainsKey(effect.ID))
                    {
                        sourceEffectCounts[effect.ID] = 0;
                    }
                    sourceEffectCounts[effect.ID]++;

                    // 이미 적용된 개수 이하면 스킵 (패시브 등 자동 적용분)
                    int alreadyApplied = existingEffectCounts.ContainsKey(effect.ID)
                        ? existingEffectCounts[effect.ID]
                        : 0;

                    if (sourceEffectCounts[effect.ID] <= alreadyApplied)
                    {
                        continue;
                    }

                    monster.SkillSystem.ApplyEffectWithState(effect);
                }
            }

            monster.SkillSystem.Apply(skill);

            // StatsComponent.TakeDamage를 직접 호출하여 OnModifyDamage 경감을 우회
            monster.Stats.TakeDamage(targetTakenDamage);
        }
    }

    private Vector3 GetRandomPositionWithCollisionCheck(Vector3 basePosition)
    {
        Vector3 position = basePosition;
        int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * randomOffset;
            Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);
            position = basePosition + offset;

            if (!Physics.CheckSphere(position, collisionRadius, obstacleLayer))
            {
                return position;
            }
        }

        return position;
    }

    private Vector3 GetSafePositionWithNavMesh(Vector3 targetPosition)
    {
        NavMeshHit hit;

        if (NavMesh.SamplePosition(targetPosition, out hit, navMeshSearchRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return targetPosition;
    }


    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword()
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>
        {
        };

        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new CloneAction
        {
            spawnCount = spawnCount,
            spawnRadius = spawnRadius,
            cloneVFX = cloneVFX,
            randomOffset = randomOffset,
            collisionRadius = collisionRadius,
            navMeshSearchRadius = navMeshSearchRadius,
            obstacleLayer = obstacleLayer,
            copyRunningEffects = copyRunningEffects,
            copyHealth = copyHealth,
        };
    }
}
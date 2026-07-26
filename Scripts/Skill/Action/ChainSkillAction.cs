using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChainSkillAction : SkillAction
{
    [Header("Chain Settings")]
    [SerializeField]
    private int chainCount = 3;
    [SerializeField]
    private float chainDelay = 0.2f;
    
    [Header("Skill Object")]
    [SerializeField]
    private GameObject skillObjectPrefab;

    private WaitForSeconds cachedVolleyWait;
    public override void Apply(Skill skill)
    {
        cachedVolleyWait = new WaitForSeconds(chainDelay);
        skill.Owner.StartCoroutine(ExecuteChainSequence(skill));
    }
    
    private IEnumerator ExecuteChainSequence(Skill skill)
    {
        var hitTargets = new HashSet<BaseCharacter>();

        BaseCharacter currentTarget = skill.Targets[0];
        // 첫 번째 공격
        SpawnSkillObjectAt(skill, skill.Owner, currentTarget);
        skill.Targets[0].SkillSystem.Apply(skill);
        
        // 첫 번째 타겟이 있다면 hitTargets에 추가
        if (skill.Targets != null && skill.Targets.Count > 0)
        {
            hitTargets.Add(skill.Targets[0]);
        }
        
        // 연쇄 공격 실행
        for (int i = 1; i < chainCount; i++)
        {
            yield return cachedVolleyWait;
            
            var nextTarget = FindNearestValidTarget(skill, currentTarget.transform.position, hitTargets);
            //Debug.Log($"{i} Chain Next : {nextTarget}");
            if (nextTarget == null) break;
            
            SpawnSkillObjectAt(skill, currentTarget, nextTarget);
            currentTarget = nextTarget;
            hitTargets.Add(nextTarget);
            nextTarget.SkillSystem.Apply(skill);
        }
    }
    
    private BaseCharacter FindNearestValidTarget(Skill skill, Vector3 position, HashSet<BaseCharacter> excludeTargets)
  {
      var targetSearcher = skill.TargetSearcher;
      var selectionAction = targetSearcher.SelectionAction;

      // SelectionAction에서 범위와 필터 가져오기
      float searchRange = (float)targetSearcher.SelectionProperRange;

      if(Mathf.Approximately(searchRange, 0f))
      {
            var bm = BattleManager.Instance;
            if (bm != null)
                searchRange =  RangeUtility.GetRadiusToCoverBounds(position, bm.PlayableBounds);
            else
                searchRange = 9999f;
      }

      TargetFilterConfig selectionFilter = null;

      // SelectTarget 타입이면 selectionFilter에 접근
      if (selectionAction is SelectTarget selectTarget)
      {
          selectionFilter = selectTarget.SelectionFilter;
      }

      // SearchArea로 fallback (기존 로직)
      var searchAction = targetSearcher.SearchAction as SearchArea;
      TargetFilterConfig searchFilter = searchAction?.SearchFilter;

      // 우선순위: SelectionFilter > SearchFilter
      var filterToUse = selectionFilter ?? searchFilter;

      Collider[] nearbyColliders = new Collider[32];
      int hitCount = Physics.OverlapSphereNonAlloc(position, searchRange,
          nearbyColliders, LayerMask.GetMask("Character"));
    
      var candidates = new List<BaseCharacter>();

      for (int i = 0; i < hitCount; i++)
      {
          var character = nearbyColliders[i]?.GetComponent<BaseCharacter>();
          if (character == null || excludeTargets.Contains(character)) continue;

          // 필터 적용
          if (filterToUse != null)
          {
              if (!filterToUse.Validate(skill.Owner, skill.Owner.gameObject, character, skill.TargetSearcher.UserData))
                  continue;
          }
          else
          {
              // 기본 검증: 적군만, 죽지 않음, 자기 자신 제외
              if (character.IsDead || character == skill.Owner) continue;
              if (skill.Owner.IsAllyOf(character)) continue;
          }

          candidates.Add(character);
      }

      // 정렬 적용
      if (filterToUse?.sorter != null)
      {
          filterToUse.sorter.Sort(skill.Owner, skill.Owner.gameObject, candidates, null);
          return candidates.Count > 0 ? candidates[0] : null;
      }

      return GetNearest(position, candidates);
  }
    
    private BaseCharacter GetNearest(Vector3 position, List<BaseCharacter> candidates)
    {
        BaseCharacter nearest = null;
        float minDistSq = float.MaxValue;
        foreach (var c in candidates)
        {
            float distSq = (c.transform.position - position).sqrMagnitude;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                nearest = c;
            }
        }
        Debug.Log($"near : {nearest}");
        
        return nearest;
    }
    
    private void SpawnSkillObjectAt(Skill skill, BaseCharacter owner, BaseCharacter target)
    {
        if (skillObjectPrefab == null || owner == null || target == null) return;

        // 연쇄 중간부터는 owner가 직전 타겟(몬스터)이라 HitPoint 소켓이 없거나
        // 사망 처리로 파괴됐을 수 있음 — 없으면 캐릭터 루트 위치로 폴백
        var ownerSocket = owner.GetTransformSocket("HitPoint");
        var targetSocket = target.GetTransformSocket("HitPoint");
        if (ownerSocket == null) ownerSocket = owner.transform;
        if (targetSocket == null) targetSocket = target.transform;

        var spawnedObject = GameObject.Instantiate(skillObjectPrefab);
        spawnedObject.transform.position = ownerSocket.position;

        // IEffectController 우선 사용 (신규 인터페이스)
        var effectController = spawnedObject.GetComponent<IEffectController>();
        if (effectController != null)
        {
            var context = new EffectSpawnContext
            {
                Owner = owner,
                Target = target,
                OwnerSocket = ownerSocket,
                TargetSocket = targetSocket,
                StartPosition = owner.transform.position,
                EndPosition = target.transform.position
            };
            effectController.Initialize(context);
        }
    }

    public override object Clone()
    {
        return new ChainSkillAction()
        {
            chainCount = chainCount,
            chainDelay = chainDelay,
            skillObjectPrefab = skillObjectPrefab
        };
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword()
    {
        var dictionary = new Dictionary<string, string>()
        {
            { "chainCount", chainCount.ToString() },
            { "chainDelay", chainDelay.ToString("0.##") },
        };
        return dictionary;
    }
}

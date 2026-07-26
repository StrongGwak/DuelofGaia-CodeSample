using System;
using UnityEngine;

[System.Serializable]
public class CircleIndicatorViewAction : IndicatorViewAction
{
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private float indicatorRadiusOverride;
    [SerializeField] private Sprite indicatorSprite;
    [SerializeField] private bool onlyShowWhenValidTarget = false;

    private CircleIndicator spawnedRangeIndicator;
    private TargetSearcher targetSearcher;
    private GameObject requesterObject;
    private BaseCharacter requesterCharacter;

    public override void ShowIndicator(TargetSearcher targetSearcher, GameObject requesterObject, object range)
    {
        ShowIndicator(targetSearcher, requesterObject, range, null);
    }

    public override void ShowIndicator(TargetSearcher targetSearcher, GameObject requesterObject, object range,
        Predicate<BaseCharacter> validateTarget)
    {
        Debug.Assert(range is float, "CircleIndicatorViewAction::ShowIndicator - range는 float이어야 합니다.");

        HideIndicator();

        this.targetSearcher = targetSearcher;
        this.requesterObject = requesterObject;
        this.requesterCharacter = requesterObject != null ? requesterObject.GetComponent<BaseCharacter>() : null;

        float radius = Mathf.Approximately(indicatorRadiusOverride, 0f) ? (float)range : indicatorRadiusOverride;

        GameObject indicator = GameObject.Instantiate(indicatorPrefab);
        indicator.transform.position = requesterObject.transform.position;
        spawnedRangeIndicator = indicator.GetComponent<CircleIndicator>();
        spawnedRangeIndicator.Setup(radius, indicatorSprite, validateTarget);

        // 정책: 매 프레임 커서 위치로 Preview 후 결과를 훅으로 푸시
        spawnedRangeIndicator.OnCursorGroundHit += HandleCursorGroundHit;

        // 초기 상태: 유효 타겟 모드면 기본 숨김
        if (onlyShowWhenValidTarget)
            spawnedRangeIndicator.SetVisible(false);
    }

    public override void HideIndicator()
    {
        if (!spawnedRangeIndicator) return;
        spawnedRangeIndicator.OnCursorGroundHit -= HandleCursorGroundHit;
        GameObject.Destroy(spawnedRangeIndicator.gameObject);
        this.spawnedRangeIndicator = null;
        this.targetSearcher = null;
        this.requesterObject = null;
        this.requesterCharacter = null;
    }

    public override void UpdateIndicator(TargetSelectionResult result)
    {
        if (spawnedRangeIndicator == null) return;
        bool hasTarget = result.resultMessage == SearchResultMessage.FindTarget && result.selectedTarget != null;
        bool hasPosition = result.resultMessage == SearchResultMessage.FindPosition;
        bool displayable = hasTarget || hasPosition;

        if (onlyShowWhenValidTarget) spawnedRangeIndicator.SetVisible(displayable);


        // 타겟이 있으면 추종, 아니면 커서 추적(null)
        spawnedRangeIndicator.SetFollowTarget(hasTarget ? result.selectedTarget.transform : null);
    }

    private void HandleCursorGroundHit(Vector3 worldHit)
    {
        if (targetSearcher == null || requesterCharacter == null || requesterObject == null)
            return;

        var preview = targetSearcher.PreviewSelection(requesterCharacter, requesterObject, worldHit);
        targetSearcher.UpdateIndicator(preview);
    }

    public override object Clone()
    {
        return new CircleIndicatorViewAction()
        {
            indicatorPrefab = indicatorPrefab,
            indicatorRadiusOverride = indicatorRadiusOverride,
            indicatorSprite = indicatorSprite,
            onlyShowWhenValidTarget = onlyShowWhenValidTarget,
        };
    }
}
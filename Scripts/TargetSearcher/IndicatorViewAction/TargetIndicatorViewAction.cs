using System;
using UnityEngine;

[System.Serializable]
public class TargetIndicatorViewAction : IndicatorViewAction
{
    [SerializeField]
    private GameObject indicatorPrefab;

    private TargetIndicator spawnedTargetIndicator;

    public override void ShowIndicator(TargetSearcher targetSearcher, GameObject requesterObject,
        object range)
    {
        ShowIndicator(targetSearcher, requesterObject, range, null);
    }

    public override void ShowIndicator(TargetSearcher targetSearcher, GameObject requesterObject,
        object range, Predicate<BaseCharacter> validateTarget)
    {
        HideIndicator();
        GameObject indicator = GameObject.Instantiate(indicatorPrefab, UIManager.Instance.MainCanvas.transform);
        indicator.transform.position = requesterObject.transform.position;
        spawnedTargetIndicator = indicator.GetComponent<TargetIndicator>();
        spawnedTargetIndicator.Setup(validateTarget);
    }

    public override void HideIndicator()
    {
        if (!spawnedTargetIndicator)
            return;
        
        GameObject.Destroy(spawnedTargetIndicator.gameObject);
    }

    public override object Clone()
    {
        return new TargetIndicatorViewAction()
        {
            indicatorPrefab = indicatorPrefab,
        };
    }
}

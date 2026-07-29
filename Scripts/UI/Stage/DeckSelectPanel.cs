using System;
using UnityEngine;

public class DeckSelectPanel : MonoBehaviour
{
    [SerializeField] private UIPanelAnimator[] panelAnimators;

    private void MovePanel()
    {
        foreach (UIPanelAnimator animator in panelAnimators)
        {
            animator.Show();
        }
    }

    private void ReturnPanel()
    {
        foreach (UIPanelAnimator animator in panelAnimators)
        {
            animator.Hide();
        }
    }

    private void OnStageSelected(StageSelector _)
    {
        MovePanel();
    }

    private void Start()
    {
        // 패널이 CanvasGroup으로 관리되어 항상 활성 상태이므로 시작 시 숨김 위치로 정렬
        foreach (UIPanelAnimator animator in panelAnimators)
        {
            animator.ResetToHidden();
        }

        // Start는 모든 Awake 이후 실행이 보장되므로 항상 현재 씬의 StageManager를 구독한다
        // (OnEnable 구독은 씬 재진입 시 이전 씬의 파괴된 인스턴스를 잡을 수 있음)
        StageManager.Instance.onStageSelected += OnStageSelected;
        StageManager.Instance.onStageDeselected += ReturnPanel;
    }

    private void OnDestroy()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.onStageSelected -= OnStageSelected;
            StageManager.Instance.onStageDeselected -= ReturnPanel;
        }
    }
}

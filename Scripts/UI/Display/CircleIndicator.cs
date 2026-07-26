using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//<summary>
// 원형 인디케이터 UI를 처리하는 클래스
//</summary>
public class CircleIndicator : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private RectTransform canvas;
    [SerializeField] private Image outLineImage;
    [SerializeField] private Image fillImage;

    private float radius;
    private LayerMask characterLayerMask;
    private readonly Collider[] overlapBuffer = new Collider[32];
    private HashSet<BaseCharacter> currentOutlined = new();
    private HashSet<BaseCharacter> frameOutlined = new();
    private Predicate<BaseCharacter> validateTarget;

    // 외부(ViewAction)가 매 프레임 커서 위치를 받아 정책을 결정할 수 있는 훅
    // 주의: 가시성과 무관하게 항상 발행되어야 함(정책이 "숨김→표시"로 전환할 신호 역할)
    public event Action<Vector3> OnCursorGroundHit;

    // 정책이 지정한 추종 대상. null이면 커서 추적 모드.
    private Transform followTarget;

    // 렌더 가시성. false면 위치 이동/아웃라인 스킵(이벤트는 계속 발행)
    private bool isVisible = true;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max(value, 0f);
            canvas.localScale = RangeUtility.DATA_TO_WORLD * Vector3.one * (radius * 0.02f);
        }
    }

    public void Setup(float radius, Sprite indicatorSprite, Predicate<BaseCharacter> validateTarget = null)
    {
        this.validateTarget = validateTarget;
        characterLayerMask = InputManager.Instance.GetVisualLayerMask();
        if (indicatorSprite != null)
            SetDeckSprite(indicatorSprite);
        Radius = radius;
        TraceCursor();
    }

    public void SetFollowTarget(Transform target) => followTarget = target;

    public void SetVisible(bool visible)
    {
        if (isVisible == visible) return;

        isVisible = visible;

        if (canvas != null)
            canvas.gameObject.SetActive(visible);

        if (!visible)
            ClearAllOutlines();
    }

    private void Update()
    {
        TraceCursor();
    }

    private void TraceCursor()
    {
        Vector3 inputPos = (Vector3)(Pointer.current?.position.ReadValue() ?? Vector2.zero);

        var ray = Camera.main.ScreenPointToRay(inputPos);
        if (!Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, LayerMask.GetMask("Ground")))
            return;

        // 정책(ViewAction)이 상태를 재판정할 수 있도록 항상 발행
        OnCursorGroundHit?.Invoke(hitInfo.point);

        // 이하 렌더/아웃라인은 보이는 상태에서만
        if (!isVisible) return;

        Vector3 anchor = followTarget != null ? followTarget.position : hitInfo.point;
        transform.position = anchor + new Vector3(0f, 0.4f);

        UpdateOutlines(anchor);
    }

    private void UpdateOutlines(Vector3 center)
    {
        float worldRadius = RangeUtility.DataToWorld(radius);
        float airHeight = 20f;
        Vector3 point1 = center;
        Vector3 point2 = center + Vector3.up * airHeight;
        int count = Physics.OverlapCapsuleNonAlloc(point1, point2, worldRadius, overlapBuffer, characterLayerMask, QueryTriggerInteraction.Collide);

        frameOutlined.Clear();
        for (int i = 0; i < count; i++)
        {
            var character = overlapBuffer[i].GetComponentInParent<BaseCharacter>();
            if (character == null || character.IsDead) continue;

            if (validateTarget != null && !validateTarget(character)) continue;

            frameOutlined.Add(character);
        }

        foreach (var prev in currentOutlined)
        {
            if (!frameOutlined.Contains(prev))
                prev.SetOutline(false);
        }

        foreach (var curr in frameOutlined)
        {
            if (!currentOutlined.Contains(curr))
                curr.SetOutline(true);
        }

        (currentOutlined, frameOutlined) = (frameOutlined, currentOutlined);
    }

    private void OnDisable() => ClearAllOutlines();
    private void OnDestroy() => ClearAllOutlines();

    private void ClearAllOutlines()
    {
        foreach (var character in currentOutlined)
        {
            if (character != null)
                character.SetOutline(false);
        }
        currentOutlined.Clear();
    }

    public void SetDeckSprite(Sprite deckSprite)
    {
        if (fillImage != null)
            fillImage.sprite = deckSprite;
    }
}
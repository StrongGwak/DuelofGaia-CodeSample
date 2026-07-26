using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TargetIndicator : MonoBehaviour
{
    [SerializeField] private Image crosshairImage;
    private Canvas parentCanvas;
    private RectTransform canvasRect;

    private LayerMask visualLayerMask;
    private BaseCharacter currentTarget;
    private Predicate<BaseCharacter> validateTarget;

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        canvasRect = parentCanvas.transform as RectTransform;
        visualLayerMask = InputManager.Instance.GetVisualLayerMask();
    }

    public void Setup(Predicate<BaseCharacter> validateTarget = null)
    {
        this.validateTarget = validateTarget;
        SetColor(false);
    }

    private void Update()
    {
        TraceCursor();
    }

    private void TraceCursor()
    {
        Vector3 inputPos = (Vector3)(Pointer.current?.position.ReadValue() ?? Vector2.zero);

        Camera conversionCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, inputPos, conversionCamera, out Vector2 localPoint))
        {
            transform.localPosition = localPoint;
        }

        UpdateOutline(inputPos);
    }

    private void UpdateOutline(Vector3 screenPos)
    {
        var cam = Camera.main;
        if (cam == null) return;

        BaseCharacter hit = null;
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, visualLayerMask))
        {
            hit = hitInfo.collider.GetComponentInParent<BaseCharacter>();
            if (hit != null && hit.IsDead) hit = null;

            // 검증 함수가 있으면 유효성 체크
            if (hit != null && validateTarget != null && !validateTarget(hit))
                hit = null;
        }

        if (hit == currentTarget)
        {
            SetColor(hit != null);
            return;
        }

        if (currentTarget != null)
            currentTarget.SetOutline(false);

        currentTarget = hit;
        if (currentTarget != null)
            currentTarget.SetOutline(true);

        SetColor(currentTarget != null);
    }

    private void OnDisable() => ClearOutline();
    private void OnDestroy() => ClearOutline();

    private void ClearOutline()
    {
        if (currentTarget != null)
        {
            currentTarget.SetOutline(false);
            currentTarget = null;
        }
    }

    public void SetColor(bool isValid)
    {
        crosshairImage.color = isValid ? Color.green : Color.red;
    }
}
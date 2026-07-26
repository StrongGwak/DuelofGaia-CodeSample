using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class StageManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float zoomDuration;
    [SerializeField] private float fogStartDistance = 90f;
    [SerializeField] private float fogEndDistance = 180f;                                                                                                                                                                         
    [SerializeField] private Color fogColor = new Color(0.7f, 0.8f, 0.9f);

    private static StageManager instance;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float originalFOV;
    private bool isZoomed = false;
    private bool isMoving = false;

    public static StageManager Instance => instance;

    public delegate void StageSelectHandler(StageSelector stage);
    public delegate void StageDeselectHandler();

    public event StageSelectHandler onStageSelected;
    public event StageSelectHandler onStageSelectStarted;
    public event StageDeselectHandler onStageDeselected;
    
    public Action<StageSelector, Vector3> onStageHovered;
    public Action onStageUnhovered;

    private StageSelector selectedStage;
    
    public StageSelector SelectedStage => selectedStage;

    /// <summary>튜토리얼 진행 중 지형 클릭 차단 (선택 해제/다른 스테이지 선택 방지)</summary>
    public bool InputLocked { get; set; }
    public void HoverStage(StageSelector stage, Vector3 mousePosition) => onStageHovered?.Invoke(stage, mousePosition);
    public void UnhoverStage() => onStageUnhovered?.Invoke();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        originalPosition = mainCamera.transform.position;
        originalRotation = mainCamera.transform.rotation;
        originalFOV = mainCamera.fieldOfView;
        DeckManager.Instance.ClearSelectedDeck();
        DeckManager.Instance.ClearSelectedAiDeck();
    }

    private void Update()
    {
        bool clicked = false;
        Vector2 clickPos = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            clicked = true;
            clickPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            clicked = true;
            clickPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (clicked)
            HandleTerrainClick(clickPos);
    }

    public void DeSelect()
    {
        SelectStage(null);
    }

    /// <summary>외부(스테이지 튜토리얼)에서 스테이지를 강제 선택한다.</summary>
    public void ForceSelect(StageSelector stage) => SelectStage(stage);

    private void HandleTerrainClick(Vector2 screenPos)
    {
        if (isMoving || InputLocked)
        {
            return;
        }

        //AudioManager.Instance.PlayUISound(UISoundType.Click);

        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Stage")))
        {
            // 클릭된 Terrain이 어느 스테이지 그룹에 속하는지 찾기
            StageSelector clickedStage = hit.collider.gameObject.GetComponentInParent<StageSelector>();
            if (clickedStage == null)
            {
                SelectStage(null);
                return;
            }

            if (!clickedStage.IsUnlocked) return;
            
            SelectStage(clickedStage);
        }
    }

    private void SelectStage(StageSelector stage)
    {
        if (stage != null && selectedStage == stage)
        {
            return;
        }

        // 이전 선택 해제
        if (selectedStage != null || stage == null)
        {
            DeselectStage(selectedStage);
            return;
        }

        // 새 스테이지 선택
        stage.SelectStage();
        selectedStage = stage;
        onStageSelectStarted?.Invoke(stage);
        ZoomToTerrain(stage.CameraPosition);
    }

    private void DeselectStage(StageSelector stage)
    {
        if (stage !=null)
        {
            stage.DeSelect();
        }
        selectedStage = null;
        ZoomOut();
        onStageDeselected?.Invoke();
    }
    
    private void ZoomToTerrain(Transform cameraTransform)
    {
        if (isZoomed) return;
        if (cameraTransform == null)  return;

        isMoving = true;
        isZoomed = true;
        
        SetFog(true);
        
        Vector3 targetPosition = cameraTransform.position;
        Quaternion targetRotation = cameraTransform.rotation;

        // 카메라 이동 애니메이션
        LeanTween.move(mainCamera.gameObject, targetPosition, zoomDuration)
            .setEaseOutQuart();

        // 카메라 회전 애니메이션
        LeanTween.rotate(mainCamera.gameObject, targetRotation.eulerAngles, zoomDuration)
            .setEaseOutQuart();

        // FOV 변경으로 줌 효과 강화
        LeanTween.value(mainCamera.gameObject, originalFOV, 60f, zoomDuration)
            .setOnUpdate((float fov) => {
                mainCamera.fieldOfView = fov;
            })
            .setEaseOutQuart()
            .setOnComplete(() =>
            {
                isMoving = false;
                onStageSelected?.Invoke(selectedStage);
            });
    }
    
    private void ZoomOut()
    {
        if (!isZoomed) return;
        isZoomed = false;
        isMoving = true;
        
        SetFog(false);
        
        // 원래 위치로 복원
        LeanTween.move(mainCamera.gameObject, originalPosition, zoomDuration)
            .setEaseInOutQuart();

        LeanTween.rotate(mainCamera.gameObject, originalRotation.eulerAngles, zoomDuration)
            .setEaseInOutQuart();

        // 원래 FOV로 복원
        LeanTween.value(mainCamera.gameObject, mainCamera.fieldOfView, originalFOV, zoomDuration)
            .setOnUpdate((float fov) => {
                mainCamera.fieldOfView = fov;
            })
            .setEaseInOutQuart().setOnComplete(() => 
            {
                isMoving = false;
            });
    }

    private void SetFog(bool enable)
    {
        if (enable)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 300f;
            RenderSettings.fogEndDistance = 500f;
        
            LeanTween.value(mainCamera.gameObject, 300f, 125f, zoomDuration)
                .setOnUpdate((float dist) => { RenderSettings.fogStartDistance = dist; })
                .setEaseOutQuart();

            LeanTween.value(mainCamera.gameObject, 500f, 250f, zoomDuration)
                .setOnUpdate((float dist) => { RenderSettings.fogEndDistance = dist; });
        }
        else
        {
            LeanTween.value(mainCamera.gameObject, RenderSettings.fogStartDistance, 300f, zoomDuration)
                .setOnUpdate((float dist) => { RenderSettings.fogStartDistance = dist; })
                .setEaseInOutQuart();

            LeanTween.value(mainCamera.gameObject, RenderSettings.fogEndDistance, 500f, zoomDuration)
                .setOnUpdate((float dist) => { RenderSettings.fogEndDistance = dist; })
                .setEaseInOutQuart()
                .setOnComplete(() => { RenderSettings.fog = false; });
        }
    }

}

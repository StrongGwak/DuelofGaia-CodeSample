using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using DG.Tweening;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public enum DragEndType { Use, Cancel, Discard }

public sealed class InputManager : MonoBehaviour
{
    /* ---------- 싱글톤 ---------- */
    public static InputManager Instance { get; private set; }

    /* ---------- 인스펙터 ---------- */
    [SerializeField] LayerMask characterLayerMask = -1;
    [SerializeField] LayerMask visualLayerMask = -1;     // GroundUnit, FlyUnit 레이어 (공중 유닛용)

    [Header("Camera Pan : Z가 좌우, X가 상하")]
    [SerializeField] float panSpeed = 0.5f;

    [Header("Camera Edge Pan (RTS 스타일 화면 끝 스크롤)")]
    [SerializeField] bool edgePanEnabled = true;
    [SerializeField] float edgePanMarginPixels = 15f;

    [Header("Camera Continuous Pan Speed (에지팬 + WASD 공통, 실제 적용 속도)")]
    [SerializeField] float continuousPanSpeed = 1200f;

    public const float MinPanSpeed = 100f;
    public const float MaxPanSpeed = 2400f;
    public const int MinPanSpeedRatio = 1;
    public const int MaxPanSpeedRatio = 100;

    [Header("Y 최소일 때 X/Z 범위 (Y=20)")]
    [SerializeField] float panLimitXMinAtYBottom = -5f;
    [SerializeField] float panLimitXMaxAtYBottom = 30f;
    [SerializeField] float panLimitZMinAtYBottom = -80f;
    [SerializeField] float panLimitZMaxAtYBottom = 80f;
    
    [Header("Y 최대일 때 X/Z 범위 (Y=50)")]
    [SerializeField] float panLimitXMinAtYTop = 25f;
    [SerializeField] float panLimitXMaxAtYTop = 35f;
    [SerializeField] float panLimitZMinAtYTop = -45f;
    [SerializeField] float panLimitZMaxAtYTop = 45f;
    
    [Header("Camera Y 범위")]
    [SerializeField] float panLimitYBottom = 20f;
    [SerializeField] float panLimitYTop = 50f;

    [Header("Camera Zoom")]
    [SerializeField] float zoomMoveSpeed = 0.5f;   // 스크롤/핀치 스케일
    [SerializeField] float pinchZoomSpeed = 0.05f;

    [Header("Camera Smooth")]
    [SerializeField] float cameraMoveTweenDuration = 0.08f;
    [SerializeField] Ease cameraMoveEase = Ease.OutQuad;

    private Camera mainCamera;

    // 초기 카메라 위치(중심점) 캐시: (이제 Y 제한은 월드 고정으로 쓰므로, 필요 없으면 제거 가능)
    Vector3 cameraStartWorldPos;

    /* ---------- 입력 래퍼 ---------- */
    PlayerInput playerInput;
    InputActionMap gamePlayMap;
    InputAction pointAction, primaryPressAction, tapAction, holdAction, cameraPanAction, cameraZoomAction;

    [Header("Camera Pan Keyboard (UIHotkeys 액션, 리바인딩 가능)")]
    [SerializeField] private InputActionReference cameraPanUpAction;
    [SerializeField] private InputActionReference cameraPanDownAction;
    [SerializeField] private InputActionReference cameraPanLeftAction;
    [SerializeField] private InputActionReference cameraPanRightAction;

    [Header("Camera Focus Side (좌/우 진영 바로 보기, UIHotkeys 액션)")]
    [SerializeField] private InputActionReference cameraFocusLeftSideAction;
    [SerializeField] private InputActionReference cameraFocusRightSideAction;

    [Header("View Enemy Duelist (상대 듀얼리스트 순환 선택, UIHotkeys 액션)")]
    [SerializeField] private InputActionReference viewEnemyDuelistAction;

    /* ---------- 상태 ---------- */
    Vector2 pointerPos;
    Vector2 pointerStart;
    bool isDragging;
    private bool isSkillActive;

    /* ---------- 카메라 누적/목표 ---------- */
    Vector2 panAccum;
    float zoomAccum;

    Vector3 _cameraTargetPos;
    Tween _cameraMoveTween;

    /* ---------- 이벤트 ---------- */
    public event Action<BaseCharacter> OnCharacterSelected;
    public event Action<Vector2> OnCardDroppedOnTarget;
    public event Action<Vector2> OnCardDroppedOnCancelArea;
    public event Func<CardController, bool> OnUseSkill;
    public event Action<CardController> OnDiscard;
    public event Action OnCardDragStart;
    public event Action OnCardDragFinish;

    static readonly List<RaycastResult> _uiHits = new();
    private BaseCharacter _currentCharacter;
    private PerceptionStatusComponent _currentPerception;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        playerInput = GetComponent<PlayerInput>();

        continuousPanSpeed = RatioToPanSpeed(SaveManager.Settings.cameraPanSpeedRatio);

        mainCamera = playerInput.camera ?? Camera.main;

        if (mainCamera != null)
        {
            cameraStartWorldPos = mainCamera.transform.position;
            _cameraTargetPos = mainCamera.transform.position;
        }

        // 인스펙터에 할당된 프로젝트 에셋을 그대로 사용한다.
        // 복사본(new DefaultInput)을 만들면 KeyBindButton 등 InputActionReference가
        // 참조하는 원본과 인스턴스가 갈라져 enable 상태가 꼬인다.
        var actions = playerInput.actions;
        gamePlayMap = actions.FindActionMap("GamePlay", throwIfNotFound: true);

        pointAction        = gamePlayMap.FindAction("Point", true);
        primaryPressAction = gamePlayMap.FindAction("PrimaryPress", true);
        tapAction          = gamePlayMap.FindAction("Tap", true);
        holdAction         = gamePlayMap.FindAction("Hold", true);
        cameraPanAction    = gamePlayMap.FindAction("CameraPan", true);
        cameraZoomAction   = gamePlayMap.FindAction("CameraZoom", true);

        pointAction.performed += OnPoint;

        primaryPressAction.started += OnPrimaryDown;
        primaryPressAction.canceled += OnPrimaryUp;

        tapAction.performed += OnTap;

        holdAction.started += OnHoldStart;
        holdAction.performed += OnHoldDrag;
        holdAction.canceled += OnHoldEnd;

        cameraPanAction.performed += OnCameraPan;
        cameraZoomAction.performed += OnCameraZoom;

        gamePlayMap.Enable();
        actions.FindActionMap("UI", throwIfNotFound: true).Enable();

        cameraPanUpAction.action.Enable();
        cameraPanDownAction.action.Enable();
        cameraPanLeftAction.action.Enable();
        cameraPanRightAction.action.Enable();

        cameraFocusLeftSideAction.action.performed += OnCameraFocusLeftSide;
        cameraFocusRightSideAction.action.performed += OnCameraFocusRightSide;
        cameraFocusLeftSideAction.action.Enable();
        cameraFocusRightSideAction.action.Enable();

        viewEnemyDuelistAction.action.performed += OnViewEnemyDuelist;
        viewEnemyDuelistAction.action.Enable();

        EnhancedTouchSupport.Enable();
    }

    void OnDestroy()
    {
        UnsubscribeCurrentSelection();
        _cameraMoveTween?.Kill();

        if (gamePlayMap != null)
        {
            pointAction.performed -= OnPoint;
            primaryPressAction.started -= OnPrimaryDown;
            primaryPressAction.canceled -= OnPrimaryUp;
            tapAction.performed -= OnTap;
            holdAction.started -= OnHoldStart;
            holdAction.performed -= OnHoldDrag;
            holdAction.canceled -= OnHoldEnd;
            cameraPanAction.performed -= OnCameraPan;
            cameraZoomAction.performed -= OnCameraZoom;

            gamePlayMap.Disable();   // 원본 에셋은 공유 자원이므로 Dispose 하지 않는다
        }

        cameraPanUpAction.action.Disable();
        cameraPanDownAction.action.Disable();
        cameraPanLeftAction.action.Disable();
        cameraPanRightAction.action.Disable();

        cameraFocusLeftSideAction.action.performed -= OnCameraFocusLeftSide;
        cameraFocusRightSideAction.action.performed -= OnCameraFocusRightSide;
        cameraFocusLeftSideAction.action.Disable();
        cameraFocusRightSideAction.action.Disable();

        viewEnemyDuelistAction.action.performed -= OnViewEnemyDuelist;
        viewEnemyDuelistAction.action.Disable();

        if (EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Disable();
    }

    public LayerMask GetVisualLayerMask() => visualLayerMask;

    private void LateUpdate()
    {
        // 일시정지(옵션/튜토리얼 등) 중에는 카메라 입력을 전부 막는다.
        // 액션 콜백으로 쌓인 zoomAccum/panAccum도 여기서 매 프레임 비워
        // 옵션을 닫는 순간 밀린 이동이 한꺼번에 적용되는 것을 막는다.
        // 단, 일시정지(CameraAllowedWhilePaused)는 카메라 조작을 허용한다.
        if (Time.timeScale == 0f && !GameSpeed.CameraAllowedWhilePaused)
        {
            panAccum = Vector2.zero;
            zoomAccum = 0f;
            return;
        }

        CollectPinchZoom();
        CollectEdgePan();
        CollectKeyboardPan();
        ApplyCameraMoves();
    }

    void OnPoint(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        pointerPos = ctx.ReadValue<Vector2>();
    }

    void OnPrimaryDown(InputAction.CallbackContext ctx)
    {
        pointerStart = pointerPos;
        isDragging = false;
    }

    void OnPrimaryUp(InputAction.CallbackContext ctx)
    {
    }

    void OnTap(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        HandleCharacterClick(pointerPos);
    }

    void OnHoldStart(InputAction.CallbackContext ctx)
    {
        pointerStart = pointerPos;
        isDragging = false;
    }

    void OnHoldDrag(InputAction.CallbackContext ctx)
    {
        if (isDragging) return;
        isDragging = true;
    }

    void OnHoldEnd(InputAction.CallbackContext ctx)
    {
        if (!isDragging) return;
        isDragging = false;
    }

    void CollectPinchZoom()
    {
        if (Touch.activeFingers.Count != 2) return;

        var t0 = Touch.activeFingers[0].currentTouch;
        var t1 = Touch.activeFingers[1].currentTouch;

        float currentDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);
        float prevDist = Vector2.Distance(
            t0.screenPosition - t0.delta,
            t1.screenPosition - t1.delta);
        zoomAccum += (currentDist - prevDist) * pinchZoomSpeed;

        Vector2 currentMid = (t0.screenPosition + t1.screenPosition) * 0.5f;
        Vector2 prevMid = ((t0.screenPosition - t0.delta) + (t1.screenPosition - t1.delta)) * 0.5f;
        panAccum += currentMid - prevMid;
    }

    private void CollectEdgePan()
    {
        if (!edgePanEnabled || mainCamera == null) return;
        if (!Application.isFocused) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 pos = mouse.position.ReadValue();

        // 화면 밖(다른 모니터 등)으로 나간 좌표는 무시
        if (pos.x < -1f || pos.y < -1f || pos.x > Screen.width + 1f || pos.y > Screen.height + 1f) return;

        Vector2 dir = Vector2.zero;
        if (pos.x <= edgePanMarginPixels) dir.x -= 1f;
        else if (pos.x >= Screen.width - edgePanMarginPixels) dir.x += 1f;

        if (pos.y <= edgePanMarginPixels) dir.y -= 1f;
        else if (pos.y >= Screen.height - edgePanMarginPixels) dir.y += 1f;

        if (dir == Vector2.zero) return;

        // 하단 카드 핸드 등 UI 위에서는 에지 팬 무시 (드래그 시작 시 카메라 튐 방지)
        if (IsPointerOverUI(pos)) return;

        // TryAddPanDelta는 "드래그로 지도를 끌어당기는" 부호 체계라 에지 스크롤(커서 방향으로 화면 이동)에는 반대로 넣어야 함
        // unscaled: 카메라 속도는 배속/정지와 무관하게 일정해야 한다
        panAccum += -dir * continuousPanSpeed * Time.unscaledDeltaTime;
    }

    private void CollectKeyboardPan()
    {
        Vector2 dir = Vector2.zero;
        if (cameraPanUpAction.action.IsPressed()) dir.y += 1f;
        if (cameraPanDownAction.action.IsPressed()) dir.y -= 1f;
        if (cameraPanLeftAction.action.IsPressed()) dir.x -= 1f;
        if (cameraPanRightAction.action.IsPressed()) dir.x += 1f;

        if (dir == Vector2.zero) return;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        // 에지 팬과 동일한 부호 체계 (TryAddPanDelta의 드래그 부호를 상쇄)
        panAccum += -dir * continuousPanSpeed * Time.unscaledDeltaTime;
    }

    private void OnCameraFocusLeftSide(InputAction.CallbackContext ctx) => FocusSide(left: true);
    private void OnCameraFocusRightSide(InputAction.CallbackContext ctx) => FocusSide(left: false);

    /// <summary>
    /// 현재 높이(Y) 기준 Z 이동 한계까지 카메라를 한 번에 이동시켜 좌/우 진영을 보여준다.
    /// (Z가 화면 좌우 축: +Z가 우측 진영)
    /// </summary>
    private void FocusSide(bool left)
    {
        if (mainCamera == null) return;
        if (TutorialGate.CameraMoveLocked) return;
        // 메뉴/튜토리얼 정지 중에는 포커스 이동도 막는다 (트윈이 unscaled라 정지 중에도 움직이므로)
        if (Time.timeScale == 0f && !GameSpeed.CameraAllowedWhilePaused) return;

        var (_, _, minZ, maxZ) = GetPanBoundsAtHeight(_cameraTargetPos.y);

        _cameraTargetPos.z = left ? minZ : maxZ;
        ApplyCameraTargetWithTween();
    }

    /// <summary>
    /// 옵션 UI의 1~100 비율 값을 실제 팬 속도(100~2400)로 변환한다.
    /// </summary>
    public static float RatioToPanSpeed(int ratio)
    {
        float t = Mathf.InverseLerp(MinPanSpeedRatio, MaxPanSpeedRatio, ratio);
        return Mathf.Lerp(MinPanSpeed, MaxPanSpeed, t);
    }

    /// <summary>
    /// 옵션 UI에서 카메라 이동 속도(에지팬 + 키보드팬 공통)를 변경할 때 사용한다.
    /// </summary>
    public void SetPanSpeed(float speed)
    {
        continuousPanSpeed = Mathf.Clamp(speed, MinPanSpeed, MaxPanSpeed);
    }

    private void OnCameraPan(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (ctx.control.device is not Mouse) return;

        panAccum += ctx.ReadValue<Vector2>();
    }

    void OnCameraZoom(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        float scrollY = 0f;

        if (ctx.valueType == typeof(float))
            scrollY = ctx.ReadValue<float>();
        else if (ctx.valueType == typeof(Vector2))
            scrollY = ctx.ReadValue<Vector2>().y;
        else
        {
            try { scrollY = ctx.ReadValue<float>(); } catch { }
            if (Mathf.Approximately(scrollY, 0f))
            {
                try { scrollY = ctx.ReadValue<Vector2>().y; } catch { }
            }
        }

        zoomAccum += scrollY;
    }

    void ApplyCameraMoves()
    {
        if (mainCamera == null) return;

        // 튜토리얼 카메라 잠금 — 잠긴 동안 쌓인 입력도 버려서 해제 순간 튀지 않게 한다
        if (TutorialGate.CameraMoveLocked)
        {
            panAccum = Vector2.zero;
            zoomAccum = 0f;
            return;
        }

        bool changed = false;

        if (panAccum != Vector2.zero)
        {
            TryAddPanDelta(panAccum);
            panAccum = Vector2.zero;
            changed = true;
        }

        if (Mathf.Abs(zoomAccum) > 0.0001f)
        {
            TryAddZoomDelta(zoomAccum);
            zoomAccum = 0f;
            changed = true;
        }

        if (changed)
            ApplyCameraTargetWithTween();
    }

    void TryAddPanDelta(Vector2 screenDelta)
    {
        // 월드 이동량 계산 (기존 로직 유지)
        Vector3 move = new Vector3(screenDelta.y, 0f, -screenDelta.x) * panSpeed;
        _cameraTargetPos += move;

        _cameraTargetPos = ClampCameraXYZ(_cameraTargetPos);
    }

    void TryAddZoomDelta(float zoomInput)
    {
        float move = zoomInput * zoomMoveSpeed;

        Transform t = mainCamera.transform;

        // forward 방향으로 줌 (목표 위치만 갱신)
        Vector3 desiredPos = _cameraTargetPos + t.forward * move;

        // 팬처럼 Y도 월드 바운더리 내에서만 움직이게 강제
        desiredPos = ClampCameraXYZ(desiredPos);

        // "바운더리를 넘어가면 확대/축소가 안되게"의 의미를 강하게 적용:
        // - clamp로 인해 Y가 잘렸다면(=바운더리 밖으로 나가려 했다면) 줌을 무시
        //   (원하면 X/Z도 동일 정책 적용 가능)
        bool yWasClamped = desiredPos.y != (_cameraTargetPos + t.forward * move).y;
        if (yWasClamped)
            return;

        _cameraTargetPos = desiredPos;
    }

    void ApplyCameraTargetWithTween()
    {
        Transform t = mainCamera.transform;

        _cameraTargetPos = ClampCameraXYZ(_cameraTargetPos);

        _cameraMoveTween?.Kill();

        _cameraMoveTween = t
            .DOMove(_cameraTargetPos, cameraMoveTweenDuration)
            .SetEase(cameraMoveEase)
            .SetUpdate(true);   // unscaled — 정지 중 카메라 이동 허용 + 배속에 따른 스무딩 변화 방지
    }

    /// <summary>
    /// 현재 높이(Y)에 비례해 XZ 이동 한계를 보간한다 — 절두체 단면이 넓어지는 것과 동일한 원리.
    /// </summary>
    private (float minX, float maxX, float minZ, float maxZ) GetPanBoundsAtHeight(float height)
    {
        // 현재 높이의 정규화 비율 (0 = 최저 높이, 1 = 최고 높이)
        float minHeight = Mathf.Min(panLimitYBottom, panLimitYTop);
        float maxHeight = Mathf.Max(panLimitYBottom, panLimitYTop);
        float heightRatio = Mathf.InverseLerp(minHeight, maxHeight, height);

        return (Mathf.Lerp(panLimitXMinAtYBottom, panLimitXMinAtYTop, heightRatio),
                Mathf.Lerp(panLimitXMaxAtYBottom, panLimitXMaxAtYTop, heightRatio),
                Mathf.Lerp(panLimitZMinAtYBottom, panLimitZMinAtYTop, heightRatio),
                Mathf.Lerp(panLimitZMaxAtYBottom, panLimitZMaxAtYTop, heightRatio));
    }

    private Vector3 ClampCameraXYZ(Vector3 position)
    {
        // 카메라 높이를 허용 구간으로 제한
        float minHeight = Mathf.Min(panLimitYBottom, panLimitYTop);
        float maxHeight = Mathf.Max(panLimitYBottom, panLimitYTop);
        position.y = Mathf.Clamp(position.y, minHeight, maxHeight);

        var (minX, maxX, minZ, maxZ) = 
            GetPanBoundsAtHeight(position.y);
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);
        return position;
    }

    // ------ 이벤트 구독 관련 메서드 ------- //
    public void SubscribeToCardEvent(CardController cc)
    {
        cc.OnCardDragBegin += HandleCardDragBegin;
        cc.OnCardDrag += HandleCardDrag;
        cc.OnCardDragEnd += HandleCardDragEnd;
    }

    public void UnsubscribeToCardEvent(CardController cc)
    {
        cc.OnCardDragBegin -= HandleCardDragBegin;
        cc.OnCardDrag -= HandleCardDrag;
        cc.OnCardDragEnd -= HandleCardDragEnd;
    }

    void SubscribeCurrentSelection()
    {
        if (_currentCharacter == null) return;

        _currentCharacter.Died += OnSelectedCharacterDied;

        _currentPerception = _currentCharacter.GetComponent<PerceptionStatusComponent>();
        if (_currentPerception != null)
        {
            _currentPerception.PerceptionChanged += OnSelectedCharacterPerceptionChanged;
        }
    }

    void UnsubscribeCurrentSelection()
    {
        if (_currentCharacter != null)
        {
            _currentCharacter.Died -= OnSelectedCharacterDied;
        }

        if (_currentPerception != null)
        {
            _currentPerception.PerceptionChanged -= OnSelectedCharacterPerceptionChanged;
            _currentPerception = null;
        }
    }

    void HandleCardDragBegin(CardController cc) => OnCardDragStart?.Invoke();

    void HandleCardDrag(CardController cc, Vector2 screenPos)
    {
        bool isOverUI = IsPointerOverUI(screenPos);

        if (!isOverUI && !isSkillActive)
        {
            if (OnUseSkill?.Invoke(cc) ?? false)
                isSkillActive = true;
        }
        else if (isOverUI && isSkillActive)
        {
            cc.MainSkill?.Cancel();
            isSkillActive = false;
        }
    }

    void HandleCardDragEnd(CardController cc, Vector2 screenPos)
    {
        var type = GetDragEndType(screenPos);
        OnCardDragFinish?.Invoke();

        if (cc.CardData.CardType == CardType.Passive &&
            (type is DragEndType.Use or DragEndType.Cancel))
            return;

        switch (type)
        {
            case DragEndType.Use:
                if (isSkillActive)
                    OnCardDroppedOnTarget?.Invoke(screenPos);
                break;
            case DragEndType.Cancel:
                OnCardDroppedOnCancelArea?.Invoke(screenPos);
                break;
            case DragEndType.Discard:
                OnDiscard?.Invoke(cc);
                break;
        }

        isSkillActive = false;
    }

    DragEndType GetDragEndType(Vector2 screenPos)
    {
        var handContainer = UIManager.Instance.HandContainer;
        var discardArea = UIManager.Instance.DiscardArea;

        foreach (var r in RaycastUI(screenPos))
        {
            if (handContainer && r.gameObject.transform.IsChildOf(handContainer.transform))
                return DragEndType.Cancel;
            if (discardArea && r.gameObject.transform.IsChildOf(discardArea.transform))
                return DragEndType.Discard;
        }
        return DragEndType.Use;
    }

    void HandleCharacterClick(Vector2 screenPos)
    {
        if (IsPointerOverUI(screenPos))
        {
            if (_currentCharacter)
                _currentCharacter.Events.Publish(CharacterEventType.OnCharacterDeselected, _currentCharacter);
            return;
        }

        var ray = Camera.main.ScreenPointToRay(screenPos);
        BaseCharacter ch = null;

        if (Physics.Raycast(ray, out var visualHit, Mathf.Infinity, visualLayerMask))
        {
            ch = visualHit.collider.GetComponentInParent<BaseCharacter>();
        }

        // 은신 등 Perception 상태에 의해 플레이어 시점에서 타겟 불가능한 캐릭터 무시
        if (ch != null && !ch.CanbeSeenBy(BattleManager.Instance.CurrentDisplayedDuelist))
        {
            ch = null;
        }

        if (ch != null && !ch.IsDead)
        {
            SelectCharacter(ch);
        }
        else
        {
            if (_currentCharacter)
            {
                UnsubscribeCurrentSelection();
                _currentCharacter.Events.Publish(CharacterEventType.OnCharacterDeselected, _currentCharacter);
                _currentCharacter = null;
            }
            OnCharacterSelected?.Invoke(null);
            //Debug.Log("InputManager: No character selected.");
        }
    }

    /// <summary>
    /// 캐릭터를 클릭한 것과 동일하게 선택 처리한다 (선택 이벤트 발행, 사망/은신 구독 포함).
    /// 이미 선택된 캐릭터를 다시 선택하면 아무것도 하지 않는다 (이벤트 중복 구독 방지).
    /// </summary>
    public void SelectCharacter(BaseCharacter ch)
    {
        if (ch == null || ch.IsDead) return;
        if (_currentCharacter == ch) return;

        if (_currentCharacter)
        {
            UnsubscribeCurrentSelection();
            _currentCharacter.Events.Publish(CharacterEventType.OnCharacterDeselected, _currentCharacter);
        }

        _currentCharacter = ch;
        SubscribeCurrentSelection();
        ch.Events.Publish(CharacterEventType.OnCharacterSelected, ch);
        OnCharacterSelected?.Invoke(ch);
    }

    private void OnViewEnemyDuelist(InputAction.CallbackContext ctx) => SelectNextEnemyDuelist();

    /// <summary>
    /// 살아있고 플레이어 시점에서 보이는 상대 듀얼리스트를 순환 선택한다.
    /// </summary>
    private void SelectNextEnemyDuelist()
    {
        var battleManager = BattleManager.Instance;
        if (battleManager == null || battleManager.CurrentDisplayedDuelist == null) return;

        var owner = battleManager.CurrentDisplayedDuelist;
        var enemies = battleManager.GetEnemyDuelists(owner);
        if (enemies.Count == 0) return;

        // 현재 선택이 상대 듀얼리스트면 그 다음부터, 아니면 첫 번째부터 (은신 등 안 보이는 대상은 건너뜀)
        int start = enemies.IndexOf(_currentCharacter as Duelist);
        for (int offset = 1; offset <= enemies.Count; offset++)
        {
            var candidate = enemies[(start + offset) % enemies.Count];
            if (candidate.CanbeSeenBy(owner))
            {
                SelectCharacter(candidate);
                return;
            }
        }
    }

    /// <summary>
    /// 외부에서 현재 선택된 캐릭터를 강제로 해제한다.
    /// (캐릭터 사망, 은신 돌입 등)
    /// </summary>
    public void DeselectCurrentCharacter()
    {
        if (_currentCharacter != null)
        {
            UnsubscribeCurrentSelection();
            _currentCharacter.Events.Publish(CharacterEventType.OnCharacterDeselected, _currentCharacter);
            _currentCharacter = null;
            OnCharacterSelected?.Invoke(null);
        }
    }

    void OnSelectedCharacterDied(GameObject _)
    {
        DeselectCurrentCharacter();
    }

    void OnSelectedCharacterPerceptionChanged(BaseCharacter ch, PerceptionState state)
    {
        if (_currentCharacter == null || ch != _currentCharacter) return;

        // 플레이어 시점에서 안 보이면 선택 해제
        bool isAllyView = BattleManager.Instance.IsOwnerTeam(ch.TeamCategory);
        bool visible = isAllyView ? state.VisibleToAllies : state.VisibleToEnemies;

        if (!visible)
        {
            DeselectCurrentCharacter();
        }
    }

    static bool IsPointerOverUI(Vector2 pos) => RaycastUI(pos).Count > 0;

    static List<RaycastResult> RaycastUI(Vector2 pos)
    {
        _uiHits.Clear();
        var data = new PointerEventData(EventSystem.current) { position = pos };
        EventSystem.current.RaycastAll(data, _uiHits);
        return _uiHits;
    }
}
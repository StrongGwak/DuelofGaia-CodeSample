using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 옵션 Control 탭에서 액션 하나의 키 리바인딩을 담당한다.
/// 키 버튼 하나가 현재 키(오버라이드 없으면 디폴트)를 표시하고, 클릭 후 새 키를 누르면 교체된다.
/// 다른 액션이 쓰던 키를 할당하면 그 액션의 바인딩은 해제되어 빈 상태(—)로 표시된다.
/// captureArea(재설정 프롬프트 영역) 밖을 마우스로 클릭하면 리바인딩이 취소된다.
/// 기능 라벨은 액션 이름을 "CommonUI" 로컬라이제이션 테이블의 키로 그대로 사용해 조회한다.
/// </summary>
public class KeyRebindRow : MonoBehaviour
{
    private const string UnboundDisplay = "—";
    private const string ListeningDisplay = "...";

    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private RectTransform captureArea;
    [SerializeField] private Button changeButton;

    [Header("표시용")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text keyText;      // changeButton 안의 키 표시 텍스트
    [SerializeField] private Color unboundColor = new Color(0.6f, 0.6f, 0.6f);

    private InputActionRebindingExtensions.RebindingOperation rebindOperation;
    private Color boundColor;

    public bool IsRebinding => rebindOperation != null;

    private void Awake()
    {
        if (keyText != null)
            boundColor = keyText.color;

        if (changeButton != null)
            changeButton.onClick.AddListener(StartRebind);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        // 다른 행의 리바인딩(충돌 해제)으로 내 바인딩이 바뀌었을 수 있으므로 열릴 때마다 갱신한다.
        RefreshLabelText();
        RefreshKeyText();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        // Dispose만 하면 OnCancel이 불리지 않아 액션맵이 꺼진 채로 남는다.
        // Cancel()은 OnCancel → FinishRebind를 동기 호출해 액션맵 복원과 Dispose까지 처리한다.
        rebindOperation?.Cancel();
    }

    private void OnDestroy()
    {
        if (changeButton != null)
            changeButton.onClick.RemoveListener(StartRebind);
    }

    private void OnLocaleChanged(Locale _) => RefreshLabelText();

    private static readonly Regex trailingNumber = new(@"\d+$");

    // SkillButton1~4처럼 뒤에 숫자만 다른 액션들은 CommonUI에 하나의 키(SkillButton)만 두고 숫자를 붙여서 표시한다.
    private void RefreshLabelText()
    {
        if (labelText == null) return;

        string actionName = actionReference.action.name;
        Match match = trailingNumber.Match(actionName);
        string baseKey = match.Success ? actionName[..match.Index] : actionName;
        string baseText = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", baseKey);

        labelText.text = match.Success ? $"{baseText} {match.Value}" : baseText;
    }

    private void Update()
    {
        if (rebindOperation == null) return;

        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            bool inside = captureArea != null &&
                RectTransformUtility.RectangleContainsScreenPoint(captureArea, pos, null);

            if (!inside)
                rebindOperation.Cancel();
        }
    }

    public void StartRebind()
    {
        if (rebindOperation != null) return;

        // 대기 중 눌린 키가 다른 핫키(스킬 사용, 옵션 닫기 등)로 발동하지 않도록 맵 전체를 끈다.
        actionReference.action.actionMap.Disable();

        if (keyText != null)
        {
            keyText.text = ListeningDisplay;
            keyText.color = boundColor;
        }

        rebindOperation = actionReference.action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .WithControlsExcluding("Touchscreen")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(_ => FinishRebind(applied: true))
            .OnCancel(_ => FinishRebind(applied: false))
            .Start();
    }

    public void CancelRebind() => rebindOperation?.Cancel();

    private void FinishRebind(bool applied)
    {
        rebindOperation?.Dispose();
        rebindOperation = null;
        actionReference.action.actionMap.Enable();

        if (applied)
        {
            ResolveConflicts();
            NormalizeOwnOverride();
            SaveOverrides();
            RefreshOtherRows();
        }

        RefreshKeyText();

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == changeButton?.gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // ResolveConflicts()로 다른 행의 바인딩이 해제됐을 수 있으므로 표시를 갱신한다.
    private void RefreshOtherRows()
    {
        foreach (var row in FindObjectsByType<KeyRebindRow>(FindObjectsSortMode.None))
        {
            if (row != this)
                row.RefreshKeyText();
        }
    }

    public void RefreshKeyText()
    {
        if (keyText == null) return;

        string path = actionReference.action.bindings[0].effectivePath;
        bool unbound = string.IsNullOrEmpty(path);

        keyText.text = unbound ? UnboundDisplay : ToDisplayString(path);
        keyText.color = unbound ? unboundColor : boundColor;
    }

    private static string ToDisplayString(string path)
        => InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);

    // 같은 액션맵 내 다른 액션이 방금 할당한 키를 쓰고 있으면 그 바인딩을 해제해 빈 상태로 둔다.
    // 키는 항상 최대 한 액션만 소유한다.
    private void ResolveConflicts()
    {
        var thisAction = actionReference.action;
        string newPath = thisAction.bindings[0].effectivePath;

        foreach (var other in thisAction.actionMap.actions)
        {
            if (other == thisAction) continue;

            for (int i = 0; i < other.bindings.Count; i++)
            {
                if (other.bindings[i].effectivePath == newPath)
                    other.ApplyBindingOverride(i, path: "");
            }
        }
    }

    // 리바인딩 결과가 내 디폴트 키와 같으면 오버라이드를 제거해 '디폴트 사용 중' 상태로 되돌린다.
    private void NormalizeOwnOverride()
    {
        var action = actionReference.action;
        InputBinding binding = action.bindings[0];

        if (binding.overridePath != null && binding.overridePath == binding.path)
            action.RemoveBindingOverride(0);
    }

    private void SaveOverrides()
    {
        SaveManager.Settings.uiHotkeyOverridesJson = actionReference.asset.SaveBindingOverridesAsJson();
        SaveManager.SaveSettings();
    }
}

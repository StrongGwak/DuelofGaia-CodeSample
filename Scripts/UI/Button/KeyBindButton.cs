using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// InputActionReference로 참조한 액션이 수행되면 연결된 버튼의 onClick을 그대로 실행한다.
/// 버튼이 어떤 동작을 하든(패널 열기/닫기/토글) 그 로직을 그대로 재사용한다.
/// InputManager(대전 씬 전용 싱글톤)와 무관하게 동작하도록 InputActionReference로 직접 참조한다.
/// 키 리바인딩은 KeyRebindRow가 담당한다 (Managers 부트스트랩에서 저장된 오버라이드를 미리 적용해둔다).
/// </summary>
public class KeyBindButton : MonoBehaviour
{
    [SerializeField] private Button targetButton;
    [SerializeField] private InputActionReference actionReference;

    public InputAction Action => actionReference.action;

    private void OnEnable()
    {
        actionReference.action.Enable();
        actionReference.action.performed += OnPerformed;
    }

    private void OnDisable()
    {
        actionReference.action.performed -= OnPerformed;
        actionReference.action.Disable();
    }

    private void OnPerformed(InputAction.CallbackContext ctx)
    {
        if (!targetButton.IsInteractable()) return;   // 비활성 버튼은 단축키도 무시 (부모 CanvasGroup 잠금 포함)
        targetButton.onClick.Invoke();
    }
}
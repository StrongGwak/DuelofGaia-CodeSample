using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControlOptionUI : MonoBehaviour
{
    [Header("Camera Pan Speed (1~100 비율, 실제 속도 100~2400)")]
    [SerializeField] private Slider panSpeedSlider;
    [SerializeField] private TMP_InputField panSpeedInput;

    [Header("Reset")]
    [SerializeField] private Button resetButton;
    [SerializeField] private InputActionAsset uiHotkeysAsset; // 키 리바인딩 오버라이드 초기화 대상

    private void OnEnable()
    {
        int ratio = SaveManager.Settings.cameraPanSpeedRatio;

        panSpeedSlider.SetValueWithoutNotify(ratio);
        panSpeedInput.SetTextWithoutNotify(ratio.ToString());

        panSpeedSlider.onValueChanged.AddListener(OnPanSpeedSliderChanged);
        panSpeedInput.onEndEdit.AddListener(OnPanSpeedInputChanged);
        resetButton.onClick.AddListener(OnResetClicked);
    }

    private void OnDisable()
    {
        panSpeedSlider.onValueChanged.RemoveListener(OnPanSpeedSliderChanged);
        panSpeedInput.onEndEdit.RemoveListener(OnPanSpeedInputChanged);
        resetButton.onClick.RemoveListener(OnResetClicked);
    }

    private void OnResetClicked()
    {
        // 팬 속도
        var d = new SettingsData(); // 필드 초기값 = 기본값
        int ratio = d.cameraPanSpeedRatio;
        panSpeedSlider.SetValueWithoutNotify(ratio);
        panSpeedInput.SetTextWithoutNotify(ratio.ToString());
        ApplyAndSave(ratio);

        // 키 바인딩: 진행 중인 리바인딩 취소 후 오버라이드 전체 제거
        var rows = GetComponentsInChildren<KeyRebindRow>(true);
        foreach (var row in rows)
            row.CancelRebind();

        uiHotkeysAsset.RemoveAllBindingOverrides();
        SaveManager.Settings.uiHotkeyOverridesJson = "";
        SaveManager.SaveSettings();

        foreach (var row in rows)
            row.RefreshKeyText();
    }

    private void OnPanSpeedSliderChanged(float value)
    {
        int ratio = Mathf.RoundToInt(value);
        panSpeedInput.SetTextWithoutNotify(ratio.ToString());
        ApplyAndSave(ratio);
    }

    private void OnPanSpeedInputChanged(string text)
    {
        if (int.TryParse(text, out int parsed))
        {
            int ratio = Mathf.Clamp(parsed, InputManager.MinPanSpeedRatio, InputManager.MaxPanSpeedRatio);
            panSpeedSlider.SetValueWithoutNotify(ratio);
            panSpeedInput.SetTextWithoutNotify(ratio.ToString());
            ApplyAndSave(ratio);
        }
        else
        {
            panSpeedInput.SetTextWithoutNotify(Mathf.RoundToInt(panSpeedSlider.value).ToString());
        }
    }

    private void ApplyAndSave(int ratio)
    {
        if (InputManager.Instance != null)
            InputManager.Instance.SetPanSpeed(InputManager.RatioToPanSpeed(ratio));

        SaveManager.Settings.cameraPanSpeedRatio = ratio;
        SaveManager.SaveSettings();
    }
}
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class GraphicOptionUI : LocalizableUI
{
    [Header("Resolution")]
    [SerializeField] private TextMeshProUGUI resolutionText;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Fullscreen")]
    [SerializeField] private TextMeshProUGUI fullscreenText;
    [SerializeField] private TMP_Dropdown fullscreenDropdown;

    /*[Header("VSync")]
    [SerializeField] private TextMeshProUGUI vSyncText;
    [SerializeField] private Toggle vSyncToggle;*/

    [Header("FrameRate")]
    [SerializeField] private TextMeshProUGUI frameRateText;
    [SerializeField] private TMP_Dropdown frameRateDropdown;

    [Header("Quality")]
    [SerializeField] private TextMeshProUGUI qualityText;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Reset")]
    [SerializeField] private Button resetButton;

    private static readonly int[] frameRates = { 30, 60, 120, -1 };

    private static readonly FullScreenMode[] fullscreenModes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed,
    };

    private readonly List<Resolution> filteredResolutions = new();

    public static void Apply()
    {
        var s = SaveManager.Settings;

        QualitySettings.SetQualityLevel(s.graphicQualityLevel, true);
        QualitySettings.vSyncCount = s.vSync ? 1 : 0;
        Application.targetFrameRate = frameRates[Mathf.Clamp(s.targetFrameRateIndex, 0, frameRates.Length - 1)];
        Screen.fullScreenMode = fullscreenModes[Mathf.Clamp(s.fullScreenModeIndex, 0, fullscreenModes.Length - 1)];

        if (s.resolutionWidth > 0 && s.resolutionHeight > 0)
            Screen.SetResolution(s.resolutionWidth, s.resolutionHeight, Screen.fullScreenMode);
    }

    private void OnEnable()
    {
        UpdateTexts();
        InitResolutionDropdown();
        InitFullscreenDropdown();
        InitFrameRateDropdown();
        InitQualityDropdown();
        //InitVSync();

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        fullscreenDropdown.onValueChanged.AddListener(OnFullscreenChanged);
        frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        //vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        resetButton.onClick.AddListener(OnResetClicked);
    }

    private void OnDisable()
    {
        resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        fullscreenDropdown.onValueChanged.RemoveListener(OnFullscreenChanged);
        frameRateDropdown.onValueChanged.RemoveListener(OnFrameRateChanged);
        qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        //vSyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
        resetButton.onClick.RemoveListener(OnResetClicked);
    }

    private void OnResetClicked()
    {
        var d = new SettingsData(); // 필드 초기값 = 기본값
        var s = SaveManager.Settings;

        s.graphicQualityLevel = d.graphicQualityLevel;
        s.targetFrameRateIndex = d.targetFrameRateIndex;
        s.fullScreenModeIndex = d.fullScreenModeIndex;
        s.vSync = d.vSync;

        // 기본 해상도 = 사용 가능한 가장 큰 해상도(모니터 네이티브)
        if (filteredResolutions.Count > 0)
        {
            s.resolutionWidth = filteredResolutions[0].width;
            s.resolutionHeight = filteredResolutions[0].height;
        }

        SaveManager.SaveSettings();
        Apply();

        // 드롭다운 표시 갱신
        InitResolutionDropdown();
        InitFullscreenDropdown();
        InitFrameRateDropdown();
        InitQualityDropdown();
    }

    public override void UpdateTexts()
    {
        resolutionText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Resolution");
        fullscreenText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "FullscreenMode");
        //vSyncText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "VSync");
        frameRateText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "FrameRate");
        qualityText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Quality");
    }

    private void InitResolutionDropdown()
    {
        filteredResolutions.Clear();
        var seen = new HashSet<(int, int)>();
        foreach (var r in Screen.resolutions)
        {
            if (r.width < 1280 || r.height < 720) continue;
            if (seen.Add((r.width, r.height)))
                filteredResolutions.Add(r);
        }

        filteredResolutions.Reverse();

        var s = SaveManager.Settings;
        int savedW = s.resolutionWidth > 0 ? s.resolutionWidth : Screen.currentResolution.width;
        int savedH = s.resolutionHeight > 0 ? s.resolutionHeight : Screen.currentResolution.height;

        var options = new List<string>();
        int currentIndex = 0;
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            options.Add($"{filteredResolutions[i].width} x {filteredResolutions[i].height}");
            if (filteredResolutions[i].width == savedW && filteredResolutions[i].height == savedH)
                currentIndex = i;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(currentIndex);
    }

    private void InitFullscreenDropdown()
    {
        fullscreenDropdown.ClearOptions();
        fullscreenDropdown.AddOptions(new List<string>
        {
            LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "FullScreen_Exclusive"),
            LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "FullScreen_Borderless"),
            LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "FullScreen_Windowed"),
        });
        fullscreenDropdown.SetValueWithoutNotify(SaveManager.Settings.fullScreenModeIndex);
    }

    private void InitFrameRateDropdown()
    {
        frameRateDropdown.ClearOptions();
        frameRateDropdown.AddOptions(new List<string>
        {
            "30", "60", "120",
            LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Unlimited"),
        });
        frameRateDropdown.SetValueWithoutNotify(SaveManager.Settings.targetFrameRateIndex);
    }

    private void InitQualityDropdown()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.SetValueWithoutNotify(SaveManager.Settings.graphicQualityLevel);
    }

    /*private void InitVSync()
    {
        vSyncToggle.SetIsOnWithoutNotify(SaveManager.Settings.vSync);
    }*/

    private void OnResolutionChanged(int index)
    {
        var r = filteredResolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
        SaveManager.Settings.resolutionWidth = r.width;
        SaveManager.Settings.resolutionHeight = r.height;
        SaveManager.SaveSettings();
    }

    private void OnFullscreenChanged(int index)
    {
        Screen.fullScreenMode = fullscreenModes[index];
        SaveManager.Settings.fullScreenModeIndex = index;
        SaveManager.SaveSettings();
    }

    private void OnFrameRateChanged(int index)
    {
        Application.targetFrameRate = frameRates[index];
        SaveManager.Settings.targetFrameRateIndex = index;
        SaveManager.SaveSettings();
    }

    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
        SaveManager.Settings.graphicQualityLevel = index;
        SaveManager.SaveSettings();
    }

    private void OnVSyncChanged(bool value)
    {
        QualitySettings.vSyncCount = value ? 1 : 0;
        SaveManager.Settings.vSync = value;
        SaveManager.SaveSettings();
    }
}

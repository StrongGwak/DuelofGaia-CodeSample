using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SoundOptionUI : LocalizableUI
{
    [Header("Master")]
    [SerializeField] private TextMeshProUGUI masterText;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Toggle masterMute;
    [SerializeField] private TMP_InputField masterInput;
    [Header("BGM")]
    [SerializeField] private TextMeshProUGUI bgmText;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Toggle bgmMute;
    [SerializeField] private TMP_InputField bgmInput;
    [Header("SFX")]
    [SerializeField] private TextMeshProUGUI sfxText;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle sfxMute;
    [SerializeField] private TMP_InputField sfxInput;
    [Header("UI SFX")]
    [SerializeField] private TextMeshProUGUI uiText;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private Toggle uiMute;
    [SerializeField] private TMP_InputField uiInput;
    [Header("Reset")]
    [SerializeField] private Button resetButton;

    public override void UpdateTexts()
    {
        masterText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "MasterVolume");
        bgmText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "BGMVolume");
        sfxText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "SFXVolume");
        uiText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "UIVolume");
    }
    
    private void OnEnable()
    {
        UpdateTexts();

        // 슬라이더 이벤트
        masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        uiSlider.onValueChanged.AddListener(OnUISFXSliderChanged);

        // 토글 이벤트
        masterMute.onValueChanged.AddListener(OnMasterMute);
        bgmMute.onValueChanged.AddListener(OnBGMMute);
        sfxMute.onValueChanged.AddListener(OnSFXMute);
        uiMute.onValueChanged.AddListener(OnUISFXMute);

        // 인풋 이벤트
        masterInput.onEndEdit.AddListener(OnMasterInputChanged);
        bgmInput.onEndEdit.AddListener(OnBGMInputChanged);
        sfxInput.onEndEdit.AddListener(OnSFXInputChanged);
        uiInput.onEndEdit.AddListener(OnUISFXInputChanged);

        resetButton.onClick.AddListener(OnResetClicked);

        RefreshUI();
    }

    private void OnDisable()
    {
        masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        bgmSlider.onValueChanged.RemoveListener(OnBGMSliderChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
        uiSlider.onValueChanged.RemoveListener(OnUISFXSliderChanged);

        masterMute.onValueChanged.RemoveListener(OnMasterMute);
        bgmMute.onValueChanged.RemoveListener(OnBGMMute);
        sfxMute.onValueChanged.RemoveListener(OnSFXMute);
        uiMute.onValueChanged.RemoveListener(OnUISFXMute);

        masterInput.onEndEdit.RemoveListener(OnMasterInputChanged);
        bgmInput.onEndEdit.RemoveListener(OnBGMInputChanged);
        sfxInput.onEndEdit.RemoveListener(OnSFXInputChanged);
        uiInput.onEndEdit.RemoveListener(OnUISFXInputChanged);

        resetButton.onClick.RemoveListener(OnResetClicked);
    }

    // 저장된 설정값을 위젯에 반영한다 (이벤트 미발생)
    private void RefreshUI()
    {
        var s = SaveManager.Settings;
        masterMute.SetIsOnWithoutNotify(s.masterMute);
        bgmMute.SetIsOnWithoutNotify(s.bgmMute);
        sfxMute.SetIsOnWithoutNotify(s.gameSfxMute);
        uiMute.SetIsOnWithoutNotify(s.uiSfxMute);

        masterSlider.SetValueWithoutNotify(s.masterVolume);
        masterInput.SetTextWithoutNotify(Mathf.RoundToInt(s.masterVolume * 100f).ToString());

        bgmSlider.SetValueWithoutNotify(s.bgmVolume);
        bgmInput.SetTextWithoutNotify(Mathf.RoundToInt(s.bgmVolume * 100f).ToString());

        sfxSlider.SetValueWithoutNotify(s.gameSfxVolume);
        sfxInput.SetTextWithoutNotify(Mathf.RoundToInt(s.gameSfxVolume * 100f).ToString());

        uiSlider.SetValueWithoutNotify(s.uiSfxVolume);
        uiInput.SetTextWithoutNotify(Mathf.RoundToInt(s.uiSfxVolume * 100f).ToString());
    }

    private void OnResetClicked()
    {
        var d = new SettingsData(); // 필드 초기값 = 기본값
        var s = SaveManager.Settings;

        s.masterVolume = d.masterVolume;
        s.bgmVolume = d.bgmVolume;
        s.gameSfxVolume = d.gameSfxVolume;
        s.uiSfxVolume = d.uiSfxVolume;
        s.masterMute = d.masterMute;
        s.bgmMute = d.bgmMute;
        s.gameSfxMute = d.gameSfxMute;
        s.uiSfxMute = d.uiSfxMute;

        AudioManager.Instance.SetMasterVolume(s.masterVolume);
        AudioManager.Instance.SetBGMVolume(s.bgmVolume);
        AudioManager.Instance.SetSFXVolume(s.gameSfxVolume);
        AudioManager.Instance.SetUISFXVolume(s.uiSfxVolume);
        AudioManager.Instance.SetMasterMute(s.masterMute);
        AudioManager.Instance.SetBGMMute(s.bgmMute);
        AudioManager.Instance.SetSFXMute(s.gameSfxMute);
        AudioManager.Instance.SetUISFXMute(s.uiSfxMute);

        SaveManager.SaveSettings();
        RefreshUI();
    }
    
    // 슬라이더 → 인풋필드
    private void OnMasterSliderChanged(float value)
    {
        if (masterMute.isOn)
        {
            masterMute.SetIsOnWithoutNotify(false);
            SaveManager.Settings.masterMute = false;
            SaveManager.SaveSettings();
        }
        masterInput.SetTextWithoutNotify(Mathf.RoundToInt(value * 100f).ToString());
        AudioManager.Instance.SetMasterVolume(value);
    }

    private void OnBGMSliderChanged(float value)
    {
        if (bgmMute.isOn)
        {
            bgmMute.SetIsOnWithoutNotify(false);
            SaveManager.Settings.bgmMute = false;
            SaveManager.SaveSettings();
        }
        bgmInput.SetTextWithoutNotify(Mathf.RoundToInt(value * 100f).ToString());
        AudioManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXSliderChanged(float value)
    {
        if (sfxMute.isOn)
        {
            sfxMute.SetIsOnWithoutNotify(false);
            SaveManager.Settings.gameSfxMute = false;
            SaveManager.SaveSettings();
        }
        sfxInput.SetTextWithoutNotify(Mathf.RoundToInt(value * 100f).ToString());
        AudioManager.Instance.SetSFXVolume(value);
    }

    private void OnUISFXSliderChanged(float value)
    {
        if (uiMute.isOn)
        {
            uiMute.SetIsOnWithoutNotify(false);
            SaveManager.Settings.uiSfxMute = false;
            SaveManager.SaveSettings();
        }
        uiInput.SetTextWithoutNotify(Mathf.RoundToInt(value * 100f).ToString());
        AudioManager.Instance.SetUISFXVolume(value);
    }

    // 인풋필드 → 슬라이더
    private void OnMasterInputChanged(string text)
    {
        if (int.TryParse(text, out int parsed))
        {
            float value = Mathf.Clamp(parsed, 1, 100) / 100f;
            masterSlider.SetValueWithoutNotify(value);
            masterInput.SetTextWithoutNotify(Mathf.RoundToInt(value * 100f).ToString());
            if (masterMute.isOn)
            {
                masterMute.SetIsOnWithoutNotify(false);
                SaveManager.Settings.masterMute = false;
                SaveManager.SaveSettings();
            }
            AudioManager.Instance.SetMasterVolume(value);
        }
        else
        {
            masterInput.SetTextWithoutNotify(Mathf.RoundToInt(masterSlider.value * 100f).ToString());
        }
    }

    private void OnBGMInputChanged(string text)
    {
        if (int.TryParse(text, out int parsed))
        {
            float value = Mathf.Clamp(parsed, 1, 100) / 100f;
            bgmSlider.SetValueWithoutNotify(value);
            bgmInput.SetTextWithoutNotify(Mathf.RoundToInt(value * 100f).ToString());
            if (bgmMute.isOn)
            {
                bgmMute.SetIsOnWithoutNotify(false);
                SaveManager.Settings.bgmMute = false;
                SaveManager.SaveSettings();
            }
            AudioManager.Instance.SetBGMVolume(value);
        }
        else
        {
            bgmInput.SetTextWithoutNotify(Mathf.RoundToInt(bgmSlider.value * 100f).ToString());
        }
    }

    private void OnSFXInputChanged(string text)
    {
        if (int.TryParse(text, out int parsed))
        {
            float value = Mathf.Clamp(parsed, 1, 100) / 100f;
            sfxSlider.SetValueWithoutNotify(value);
            sfxInput.SetTextWithoutNotify(Mathf.RoundToInt(value * 100f).ToString());
            if (sfxMute.isOn)
            {
                sfxMute.SetIsOnWithoutNotify(false);
                SaveManager.Settings.gameSfxMute = false;
                SaveManager.SaveSettings();
            }
            AudioManager.Instance.SetSFXVolume(value);
        }
        else
        {
            sfxInput.SetTextWithoutNotify(Mathf.RoundToInt(sfxSlider.value * 100f).ToString());
        }
    }

    private void OnUISFXInputChanged(string text)
    {
        if (int.TryParse(text, out int parsed))
        {
            float value = Mathf.Clamp(parsed, 1, 100) / 100f;
            uiSlider.SetValueWithoutNotify(value);
            uiInput.SetTextWithoutNotify(Mathf.RoundToInt(value * 100f).ToString());
            if (uiMute.isOn)
            {
                uiMute.SetIsOnWithoutNotify(false);
                SaveManager.Settings.uiSfxMute = false;
                SaveManager.SaveSettings();
            }
            AudioManager.Instance.SetUISFXVolume(value);
        }
        else
        {
            // 잘못된 입력이면 슬라이더 현재값으로 복구
            uiInput.SetTextWithoutNotify(Mathf.RoundToInt(uiSlider.value * 100f).ToString());
        }
    }

    private void OnMasterMute(bool muted)
    {
        AudioManager.Instance.SetMasterMute(muted);
        SaveManager.Settings.masterMute = muted;
        SaveManager.SaveSettings();
    }

    private void OnBGMMute(bool muted)
    {
        AudioManager.Instance.SetBGMMute(muted);
        SaveManager.Settings.bgmMute = muted;
        SaveManager.SaveSettings();
    }

    private void OnSFXMute(bool muted)
    {
        AudioManager.Instance.SetSFXMute(muted);
        SaveManager.Settings.gameSfxMute = muted;
        SaveManager.SaveSettings();
    }

    private void OnUISFXMute(bool muted)
    {
        AudioManager.Instance.SetUISFXMute(muted);
        SaveManager.Settings.uiSfxMute = muted;
        SaveManager.SaveSettings();
    }
}

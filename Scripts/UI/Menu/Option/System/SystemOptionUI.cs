using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SystemOptionUI : LocalizableUI
{
    [SerializeField] private TextMeshProUGUI languageText;
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Reset")]
    [SerializeField] private Button resetButton;
    
    private static readonly Dictionary<string, string> LocaleDisplayNames = new()                                                                                                                                                  {                                                                                                                                                                                                                            
        { "ko", "한국어" },                                                                                                                                                                                                      
        { "en", "English" },          
        { "ja", "日本語" },
    };

    private void Start()
    {
        StartCoroutine(InitDropdown());
    }

    private IEnumerator InitDropdown()
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        languageDropdown.ClearOptions();

        var options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < locales.Count; i++)
        {
            string code = locales[i].Identifier.Code;
            options.Add(LocaleDisplayNames.TryGetValue(code, out string name) ? name : locales[i].LocaleName);
            if (locales[i] == LocalizationSettings.SelectedLocale)
                currentIndex = i;
        }

        languageDropdown.AddOptions(options);
        languageDropdown.SetValueWithoutNotify(currentIndex);
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        resetButton.onClick.AddListener(OnResetClicked);
    }

    private void OnResetClicked()
    {
        var defaultLocale = LocalizationSettings.ProjectLocale; // 프로젝트 기본 로케일
        if (defaultLocale == null) return;

        LocalizationSettings.SelectedLocale = defaultLocale;
        SaveManager.Settings.locale = defaultLocale.Identifier.Code;
        SaveManager.SaveSettings();

        int index = LocalizationSettings.AvailableLocales.Locales.IndexOf(defaultLocale);
        if (index >= 0)
            languageDropdown.SetValueWithoutNotify(index);
    }

    public override void UpdateTexts()
    {
        languageText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "Language");
    }

    private void OnLanguageChanged(int index)
    {
        var locale = LocalizationSettings.AvailableLocales.Locales[index];
        LocalizationSettings.SelectedLocale = locale;
        SaveManager.Settings.locale = locale.Identifier.Code;
        SaveManager.SaveSettings();
    }

    private void OnDestroy()
    {
        languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        resetButton.onClick.RemoveListener(OnResetClicked);
    }
}

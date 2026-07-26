using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public abstract class LocalizableUI : MonoBehaviour, ILocalizable
{
    private void OnEnable()
    {
        UpdateTexts();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _) => UpdateTexts();

    public abstract void UpdateTexts();
}
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private LocalizedString localizedString;
    private TextMeshProUGUI text;

    private void Awake() => text = GetComponent<TextMeshProUGUI>();
    
    private void Start() => text.text = localizedString.GetLocalizedString();

    private void OnEnable()
    {
        localizedString.StringChanged += UpdateText;
    }

    private void OnDisable()
    {
        localizedString.StringChanged -= UpdateText;
    }

    private void UpdateText(string value) => text.text = value;
}

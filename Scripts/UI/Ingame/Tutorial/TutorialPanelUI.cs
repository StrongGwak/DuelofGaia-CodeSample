using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 설명/힌트 표시 패널.
/// - blocking(설명 스텝): 전체 화면 딤이 게임 입력을 막고, 아무 곳이나 탭하면 진행
/// - non-blocking(힌트 스텝): 텍스트 박스만 표시, 게임 입력을 막지 않음
/// 설명 스텝은 timeScale=0에서 표시되므로 모든 연출은 unscaled 기준(SetUpdate(true))이어야 한다.
///
/// 권장 하이어라키 (아래에서 위 순서로):
///   DimBlocker(Image+Button, 반투명 전체 화면) → HighlightFrame → TextBox(CanvasGroup)
/// </summary>
public class TutorialPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private CanvasGroup panelGroup;      // 텍스트 박스 루트
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button dimBlocker;           // 전체 화면 딤 (레이캐스트 차단 + 탭 감지 겸용)

    [Header("Highlight")]
    [SerializeField] private RectTransform highlightFrame;
    [Tooltip("대상 크기에 더할 여백 (총합 기준 — 양쪽에 절반씩 적용)")]
    [SerializeField] private Vector2 highlightPadding = new(20f, 20f);
    [Tooltip("집중 애니메이션 시작 시 최종 크기에 더해지는 여분 크기")]
    [SerializeField] private float focusStartExtra = 100f;
    [Tooltip("집중 애니메이션 재생 시간")]
    [SerializeField] private float focusDuration = 0.3f;
    [Tooltip("조여든 뒤 다음 반복까지 대기 시간")]
    [SerializeField] private float focusLoopInterval = 0.6f;

    [Header("Localization")]
    [SerializeField] private string stringTable = "Tutorial";

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.15f;
    [Tooltip("페이지 표시 후 탭이 먹기까지의 유예 시간 — 연타로 설명을 건너뛰는 것 방지 (unscaled)")]
    [SerializeField] private float tapDelay = 0.5f;

    private bool tapReceived;
    private DG.Tweening.Sequence focusSeq;   // 하이라이트 집중 반복 시퀀스 (스텝 전환 시 Kill)
    private string currentKey;   // 표시 중인 텍스트 키 — 언어 변경 시 재적용용

    private void Awake()
    {
        dimBlocker.onClick.AddListener(() => tapReceived = true);
        panelGroup.alpha = 0f;
        panelGroup.gameObject.SetActive(false);
        dimBlocker.gameObject.SetActive(false);
        highlightFrame.gameObject.SetActive(false);
    }

    private void OnEnable() => LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

    private void OnLocaleChanged(Locale _)
    {
        if (!string.IsNullOrEmpty(currentKey) && panelGroup.gameObject.activeSelf)
            messageText.text = LocalizationSettings.StringDatabase.GetLocalizedString(stringTable, currentKey);
    }

    /// <summary>
    /// 로컬라이즈된 텍스트를 표시한다.
    /// blocking이면 전체 화면 딤으로 게임 입력을 막는다 (탭 대기용).
    /// </summary>
    public void ShowText(string key, bool blocking)
    {
        currentKey = key;
        messageText.text = LocalizationSettings.StringDatabase.GetLocalizedString(stringTable, key);
        dimBlocker.gameObject.SetActive(blocking);

        panelGroup.DOKill();   // 진행 중인 Hide 페이드아웃(+SetActive(false) 예약) 취소
        if (!panelGroup.gameObject.activeSelf)
        {
            panelGroup.gameObject.SetActive(true);
            panelGroup.alpha = 0f;
        }
        panelGroup.DOFade(1f, fadeDuration).SetUpdate(true);
    }

    /// <summary>딤 영역이 탭될 때까지 대기한다. timeScale=0에서도 동작한다.
    /// 표시 직후 tapDelay 동안의 탭은 무시한다 (연타 스킵 방지).</summary>
    public IEnumerator WaitForTap()
    {
        yield return new WaitForSecondsRealtime(tapDelay);
        tapReceived = false;   // 유예 중 들어온 탭은 버린다
        yield return new WaitUntil(() => tapReceived);
    }

    /// <summary>
    /// 하이라이트 프레임을 대상 UI 위로 이동시킨다. null이면 숨긴다.
    /// loop이면 집중 연출을 무한 반복한다 (행동 대기 스텝용).
    /// </summary>
    public void SetHighlight(RectTransform target, bool loop = false)
    {
        if (target == null)
        {
            focusSeq?.Kill();   // 진행 중인 집중 반복 애니메이션 정리
            highlightFrame.gameObject.SetActive(false);
            return;
        }

        highlightFrame.gameObject.SetActive(true);
        // rect.center는 피벗 기준 로컬 좌표의 사각형 중심 → 대상 피벗이 어디든 시각적 중심에 맞는다
        highlightFrame.position = target.TransformPoint(target.rect.center);

        // 같은 Canvas 안에서만 정확하다 — 다른 Canvas 대상이 생기면 WorldCorners 변환으로 교체할 것
        Vector2 finalSize = target.rect.size + highlightPadding;

        // 집중 연출: 크게 시작해서 대상 크기로 조여든다 (Dialogue는 timeScale=0이므로 unscaled 필수)
        focusSeq?.Kill();
        highlightFrame.sizeDelta = finalSize + Vector2.one * focusStartExtra;
        focusSeq = DOTween.Sequence()
            .Append(highlightFrame.DOSizeDelta(finalSize, focusDuration).SetEase(Ease.OutCubic))
            .AppendInterval(focusLoopInterval)
            .SetUpdate(true)
            .SetLoops(loop ? -1 : 1, LoopType.Restart);   // Dialogue는 1회, Action은 무한 반복
    }

    /// <summary>패널 전체(텍스트/딤/하이라이트)를 숨긴다.</summary>
    public void Hide()
    {
        panelGroup.DOKill();
        panelGroup.DOFade(0f, fadeDuration).SetUpdate(true)
            .OnComplete(() => panelGroup.gameObject.SetActive(false));
        dimBlocker.gameObject.SetActive(false);
        SetHighlight(null);
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Localization.Settings;
using Object = UnityEngine.Object;

public class TitleScene : BaseScene
{
    private const string PreloadFolder = "PreLoad";
    private const float FontWarmupProgressShare = 0.3f;
    private const float ShaderWarmupProgressShare = 0.2f;
    private const float WarmupProgressShare = FontWarmupProgressShare + ShaderWarmupProgressShare;

    [Header("Font Warmup")]
    [SerializeField] private List<TMP_FontAsset> warmupFonts = new();
    [SerializeField] private bool warmupIncludeFontFeatures = true;

    [Header("Shader Warmup")]
    [SerializeField] private ShaderVariantCollection warmupShaders;
    [SerializeField] private int shaderVariantsPerBatch = 8;

    [Header("Title")]
    [SerializeField] private Image titleImage;
    [SerializeField, Range(1f, 1.5f)] private float titleOvershootScale = 1.15f;
    [SerializeField] private float titleFadeInDuration = 0.9f;
    [SerializeField] private float titleScaleInDuration = 0.8f;
    [SerializeField] private Ease titleScaleEase = Ease.OutBack;

    private DG.Tweening.Sequence titleIntroSeq;

    [Header("Main Menu Group")]
    [SerializeField] private Define.Scene nextScene = Define.Scene.Stage_Select;
    [SerializeField] private CanvasGroup buttonGroupCg;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button dictionaryButton;
    [SerializeField] private GameObject dictionaryCanvas;
    [SerializeField] private Button optionButton;
    [SerializeField] private GameObject optionCanvas;

    [Header("Click Feedback")]
    [SerializeField] private float punchScale = 0.08f;
    [SerializeField] private float punchDuration = 0.18f;
    [SerializeField] private int punchVibrato = 8;
    [SerializeField] private float punchElasticity = 0.8f;

    private bool mainMenuStarted;

    protected override void Init()
    {
        SceneType = Define.Scene.Title;
        base.Init();

        InitializeUIState();
        RegisterButtonEvents();
        StartMainMenu();
    }

    private void Update()
    {
        /*if (Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame)
            ResetStageProgress();

        if (Keyboard.current != null && Keyboard.current.endKey.wasPressedThisFrame)
            SetPrefs();

        if (Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame)
            ResetSettings();*/
    }

    private void ResetStageProgress()
    {
        SaveManager.ResetGame();
        Debug.Log("[Debug] 스테이지 정복 정보 초기화 완료");
    }

    private void ResetSettings()
    {
        SaveManager.ResetSettings();
    }

    private void SetPrefs()
    {
        Define.Scene[] stages =
        {
            Define.Scene.Light, Define.Scene.Fire, Define.Scene.Fire_Light,
            Define.Scene.Might, Define.Scene.Nature, Define.Scene.Might_Nature,
            Define.Scene.Death, Define.Scene.Darkness, Define.Scene.Death_Darkness,
            Define.Scene.Storm, Define.Scene.Water, Define.Scene.Storm_Water,
            Define.Scene.Red, Define.Scene.Blue, Define.Scene.Black, Define.Scene.Green,
        };

        Define.Scene[] advancedStages =
        {
            Define.Scene.Light, Define.Scene.Fire, Define.Scene.Might, Define.Scene.Nature,
            Define.Scene.Death, Define.Scene.Darkness, Define.Scene.Storm, Define.Scene.Water,
        };

        foreach (var stage in stages)
        {
            SaveManager.Game.stageCleared.Set(stage.ToString(), true);
        }
        foreach (var stage in advancedStages)
        {
            SaveManager.Game.stageCleared.Set("Advanced" + stage, true);
        }
        SaveManager.SaveGame();
    }

    private void InitializeUIState()
    {
        if (buttonGroupCg != null) UIHider.Hide(buttonGroupCg);
    }

    [ContextMenu("PlayTitleIntroAnimation")]
    private void PlayTitleIntroAnimation()
    {
        if (titleImage == null) return;

        titleIntroSeq?.Kill();

        SetActiveSafe(titleImage, true);
        var tr = titleImage.transform;

        tr.localScale = Vector3.one * titleOvershootScale;
        var c = titleImage.color;
        c.a = 0f;
        titleImage.color = c;

        titleIntroSeq = DOTween.Sequence();
        titleIntroSeq.Append(titleImage.DOFade(1f, titleFadeInDuration));
        titleIntroSeq.Join(tr.DOScale(1f, titleScaleInDuration).SetEase(titleScaleEase));
    }

    private void RegisterButtonEvents()
    {
        AddListenerSafe(startButton, NextScene);
        AddListenerSafe(exitButton, Application.Quit);
        AddListenerSafe(dictionaryButton, () => dictionaryCanvas.SetActive(true));
        AddListenerSafe(optionButton, () => optionCanvas.SetActive(true));
    }

    private void AddListenerSafeWithoutCoroutine(Button button, Action action)
    {
        if (button == null || action == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action?.Invoke());
    }

    private void AddListenerSafe(Button button, Action action)
    {
        if (button == null || action == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => StartCoroutine(ClickFeedbackThen(action, button)));
    }

    private IEnumerator ClickFeedbackThen(Action action, Button btn)
    {
        if (btn == null || action == null) yield break;

        // 중복 연타 방지
        if (!btn.interactable) yield break;
        btn.interactable = false;

        var tr = btn.transform;
        yield return tr
            .DOPunchScale(new Vector3(punchScale, punchScale, 0f), punchDuration, punchVibrato, punchElasticity)
            .WaitForCompletion();

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        action?.Invoke();

        // 씬 전환이 즉시 일어나지 않는 버튼 대비
        btn.interactable = true;
    }

    private void StartMainMenu()
    {
        if (mainMenuStarted) return;
        mainMenuStarted = true;
        
        if (buttonGroupCg != null)
        {
            UIHider.Show(buttonGroupCg);
            var t = buttonGroupCg.transform;
            t.localScale = Vector3.zero;
            t.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

    }

    private void NextScene()
    {
        if (SceneFader.Instance == null)
        {
            Managers.Scene.LoadScene(nextScene);
            return;
        }

        SceneFader.Instance.LoadSceneWithPreload(nextScene, onProgress =>
        {
            StartCoroutine(PreloadRoutine(onProgress));
        });
    }

    private IEnumerator PreloadRoutine(Action<float> onProgress)
    {
        var tableNames = new[]
        {
            "CardName", "StageName", "CommonUI", "StageDescription", "SkillDescription", "DeckName",
            "SkillName", "EffectDescription", "MonsterName", "StatName", "StageExplain", "Tutorial"
        };
        foreach (var tableName in tableNames)
        {
            var op = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
            while (!op.IsDone) yield return null;
        }

        // 폰트 아틀라스 워밍업 (0 ~ 30%)
        yield return FontWarmup.WarmupRoutine(
            warmupFonts,
            tableNames,
            p => onProgress(p * FontWarmupProgressShare),
            warmupIncludeFontFeatures);

        // 셰이더 변형 워밍업 (30% ~ 50%)
        yield return ShaderWarmup.WarmupRoutine(
            warmupShaders,
            p => onProgress(FontWarmupProgressShare + p * ShaderWarmupProgressShare),
            shaderVariantsPerBatch);

        // 리소스 로드 (50% ~ 100%)
        Managers.Resource.LoadAllAsync<Object>(PreloadFolder, (key, count, total) =>
        {
            float p = total > 0 ? (float)count / total : 1f;
            onProgress(WarmupProgressShare + p * (1f - WarmupProgressShare));
        });
    }

    private void SetActiveSafe(Behaviour comp, bool active)
    {
        if (comp != null) comp.gameObject.SetActive(active);
    }

    public override void Clear() { }

    private void OnDestroy()
    {
        titleIntroSeq?.Kill();
    }

    private void RemoveAllListeners(params Button[] buttons)
    {
        if (buttons == null) return;
        foreach (var b in buttons)
            if (b != null)
                b.onClick.RemoveAllListeners();
    }
}

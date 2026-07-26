using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 사용자의 입력과 상호작용을 시각적으로 처리하는 UI 컴포넌트
/// 카드 사용, 텍스트 표시 등 View 역할을 담당
/// 내부 로직은 CardController에 위임
/// </summary>
public class CardUI : BaseUI
{
    [Header("Card Info")]
    [SerializeField] private Image cardImage;   // 카드 일러스트
    [SerializeField] private Image cardBackground;  // 카드 배경
    [SerializeField] private Image useFrame;   // 주문 카드 프레임
    [SerializeField] private Image spawnFrame;   // 몬스터 소환 카드 프레임
    [SerializeField] private Image deckIcon;    // 덱 아이콘
    [SerializeField] private Image cooldown;    // 쿨다운 이미지
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI spawnDesc;
    [SerializeField] private TextMeshProUGUI useDesc;
    [SerializeField] private TextMeshProUGUI manaCostText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private GameObject deco;
    
    [Header("Draw Animation")]
    [SerializeField] private Transform cardVisualRoot;

    [Header("Fusion Effect")]
    [SerializeField] private ParticleSystem fusionEffect;

    [Header("Selected Event")]
    [SerializeField] private GameObject glow;
    [SerializeField] private GameObject usableGlow;
    [SerializeField] private GameObject unusableGlow;
    [SerializeField] private float selectScale = 0.4f;
    [SerializeField] private float defaultScale = 0.3f;
    [SerializeField] private float defaultWidth = 680f;
    
    private RectTransform rectTransform;
    private HorizontalLayoutGroup horizontalLayoutGroup;
    private Skill currentSkill;
    private Coroutine cooldownCoroutine;
    
    //private Canvas cardCanvas;

    private void Awake()
    {
        cardName.text = "";
        spawnDesc.text = "";
        useDesc.text = "";
        manaCostText.text = "";
        damageText.text = "";
        healthText.text = "";
        rectTransform = GetComponent<RectTransform>();
        horizontalLayoutGroup = GetComponentInParent<HorizontalLayoutGroup>();
    }

    // 카드 정보를 통해서 UI 세팅
    public void Setup(CardController cardController)
    {
        RefreshUI();   // Setup 자체가 항상 깨끗한 상태에서 시작하도록

        Skill skill = cardController.MainSkill;
        if (skill == null) return;
        useFrame.gameObject.SetActive(true);
        spawnFrame.gameObject.SetActive(false);
        
        CardData cardData = cardController.CardData;
        if (cardData == null) return;
        if (cardImage != null) cardImage.sprite = cardData.SkillData.Icon;
        
        DeckData deckData = DeckManager.Instance.GetDeckDataByCardData(cardData);
        if (deckData == null) return;
        if (deckData.Type != DeckType.Duel && deckData.Type != DeckType.Dragon) deco.SetActive(true);
        if (deckIcon != null) deckIcon.sprite = deckData.DeckIcon;
        
        if (cardData.CardType == CardType.Spawn)
        {
            if (spawnFrame != null)
            {
                spawnFrame.gameObject.SetActive(true);
                useFrame.gameObject.SetActive(false);
            }

            if (cardBackground != null) cardBackground.sprite = deckData.SpawnBackgroundSprite;
            
            var (health, damage) = skill.GetSummonMonsterStats();
            healthText.text = health.ToString();
            damageText.text = damage.ToString();
        }

        UpdateTexts(cardData, skill);

        if (cardData.CardType == CardType.Equipment && skill.HasCooldown)
        {
            currentSkill = skill;
            cooldown.gameObject.SetActive(true);
            cooldown.fillAmount = 0f;
            skill.onStateChanged += OnSkillStateChanged;
            if (skill.IsInState<CooldownState>())
                cooldownCoroutine = StartCoroutine(ShowCooldown());
        }

        if (cardData.CardType == CardType.Passive) return;
        
        foreach (var cost in skill.Costs)
        {
            if (cost.CodeName == "MANA")
            {
                manaCostText.text = skill.GetCostValue(cost.ID).ToString();
                break;
            }
        }
    }

    private void RefreshUI()
    {
        cardName.text = "";
        spawnDesc.text = "";
        useDesc.text = "";
        manaCostText.text = "";
        damageText.text = "";
        healthText.text = "";
        deco.SetActive(false);
        spawnFrame.gameObject.SetActive(false);
        useFrame.gameObject.SetActive(false);
        cooldown.fillAmount = 0f;
        cooldown.gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        if (currentSkill != null)
        {
            currentSkill.onStateChanged -= OnSkillStateChanged;
            currentSkill = null;
        }
        cooldownCoroutine = null;

        RefreshUI();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(defaultWidth, rectTransform.sizeDelta.y);
        }
        base.OnDisable();
    }

    public void OnSelected(bool isSelected)
    {
        // Spacing 애니메이션: 현재 값 → 목표 값
        float currentSpacing = horizontalLayoutGroup.spacing;
        float targetSpacing = isSelected ? -75 : -30;

        LeanTween.value(gameObject, currentSpacing, targetSpacing, 0.2f)
            .setOnUpdate((float value) => {
                if (horizontalLayoutGroup != null)
                {
                    horizontalLayoutGroup.spacing = value;
                }
            })
            .setIgnoreTimeScale(true);
        glow.SetActive(isSelected);
        LeanTween.scale(gameObject, isSelected ? Vector3.one * selectScale : Vector3.one * defaultScale, 0.2f);
        AnimateWidth(isSelected);
    }

    public void PlayDrawAnimation(Action onComplete = null)
    {
        if (cardVisualRoot == null)
        {
            onComplete?.Invoke();
            return;
        }
        cardVisualRoot.localScale = Vector3.zero;
        cardVisualRoot.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).OnComplete(() => onComplete?.Invoke());
    }

    public void PlayFusionEffect()
    {
        if (fusionEffect == null) return;
        fusionEffect.gameObject.SetActive(true);
        fusionEffect.Play();
    }

    public void OnUsable(bool isUsable)
    {
        if (isUsable)
        {
            unusableGlow.SetActive(false);
            usableGlow.SetActive(true);
        }
        else
        {
            usableGlow.SetActive(false);
            unusableGlow.SetActive(true);
        }
    }
    
    private void AnimateWidth(bool selected)
    {
        if (rectTransform == null || !gameObject.activeInHierarchy) return;
        
        float currentWidth = rectTransform.sizeDelta.x;
        float toWidth = selected ? 1000f : defaultWidth;
        
        // 이미 목표 크기에 도달했다면 애니메이션 실행하지 않음
        if (Mathf.Approximately(currentWidth, toWidth)) return;

        LeanTween.value(gameObject, currentWidth, toWidth, 0.2f)
            .setOnUpdate((float value) => {
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(value, rectTransform.sizeDelta.y);
                }
            })
            .setIgnoreTimeScale(true);
    }

    private void OnSkillStateChanged(Skill skill, State<Skill> currentState, State<Skill> prevState, int layer)
    {
        if (!gameObject.activeInHierarchy) return;
        if (currentState.GetType() == typeof(CooldownState))
            cooldownCoroutine = StartCoroutine(ShowCooldown());
    }

    private IEnumerator ShowCooldown()
    {
        while (currentSkill.IsInState<CooldownState>())
        {
            cooldown.fillAmount = currentSkill.CurrentCooldown / currentSkill.Cooldown;
            yield return null;
        }
        cooldown.fillAmount = 0f;
        cooldownCoroutine = null;
    }

    public void UpdateTexts(CardData data, Skill skill)
    {
        cardName.text = skill.DisplayName;
        //cardName.text = LocalizationSettings.StringDatabase.GetLocalizedString("CardName", data.name);
        if (data.CardType == CardType.Spawn)
        {
            spawnDesc.text = skill.Description;
        }
        else
        {
            useDesc.text = skill.Description;
        }
        
        if (data.CardType == CardType.Passive) 
            manaCostText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", data.CardType.ToString()+"Card");
    }
}

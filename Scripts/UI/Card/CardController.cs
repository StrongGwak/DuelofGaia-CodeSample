using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 카드 UI에서 발생한 이벤트를 처리하고 게임 로직을 실행하는 Controller
/// UI와 게임 시스템 사이의 중재자 역할
/// </summary>
public class CardController : MonoBehaviour, IPointerClickHandler, IDragHandler, IEndDragHandler, IBeginDragHandler
{
    [SerializeField] private CardData cardData;
    [SerializeField] private CardUI cardUI;
    
    private bool isSelected = false;
    private bool isUseable = false;
    private Skill mainSkill;
    private Skill subSkill;
    private GameObject drawEffectInstance;

    public CardData CardData => cardData;
    public CardUI CardUI => cardUI;
    public Skill MainSkill => mainSkill;
    public Skill SubSkill => subSkill;
    public bool IsUseable => isUseable;
    

    public delegate void DragBeginHandler(CardController cardController);
    public delegate void DragHandler(CardController cardController, Vector2 mousePosition);
    public delegate void DragEndHandler(CardController cardController, Vector2 mousePosition);
    public delegate void CardUsedHandler(CardController cardController);
    
    // 카드 사용 이벤트
    public delegate void SpawnHandler();
    public delegate void ItemEquipHandler();
    public delegate void SkillCastHandler(Skill usedSkill);
    
    public event DragBeginHandler OnCardDragBegin;
    public event DragHandler OnCardDrag;
    public event DragEndHandler OnCardDragEnd;
    public event CardUsedHandler OnCardUsed;
    public event SpawnHandler OnSpawn;
    public event ItemEquipHandler OnItemEquip;
    public event SkillCastHandler OnSkillCast;

    // 드래그 중 사용 불가 이유를 화면에 표시하기 위한 정적 이벤트
    public static event Action<Define.UseBlockReason> OnCardDragBlocked;
    public static event Action OnCardDragBlockCleared;
    

    private void OnLocaleChanged(Locale _) => cardUI.UpdateTexts(cardData, mainSkill);

    private void Awake()
    {
        cardUI = GetComponent<CardUI>();
    }

    // 카드 객체 연결, UI 세팅 및 활성화, 이벤트 구독 함수
    public void Bind(CardData newCardData, SkillSystem skillSystem)
    {
        if (newCardData == null || skillSystem == null) return;
        // GetOrRegister: 이미 있으면 찾고, 없으면 새로 등록
        Skill newSkill = skillSystem.GetOrRegisterSkillWithActivation(newCardData.SkillData);
        if (newSkill != null) mainSkill = newSkill;
        // 패시브 기능을 가진 카드라면 부가적으로 활성화
        if (newCardData.PassiveSkillData != null)
        {
            newSkill = skillSystem.GetOrRegisterSkillWithActivation(newCardData.PassiveSkillData);
            if (newSkill != null) subSkill = newSkill;
        }
        cardData = newCardData;
        if (!IsActive()) gameObject.SetActive(true);
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        mainSkill.onStateChanged += OnMainSkillStateChanged;
        cardUI.Setup(this);

        // 바인딩 직후 비용 충족 여부를 즉시 체크
        RefreshUseable();
    }

    /// <summary>
    /// 현재 스킬의 모든 비용을 검사하여 isUseable 상태를 갱신합니다.
    /// Bind 시점 등 onStatValueChanged 이벤트 없이도 정확한 상태를 보장합니다.
    /// </summary>
    public void RefreshUseable()
    {
        if (cardData == null || mainSkill == null)
        {
            return;
        }

        if (cardData.CardType == CardType.Passive)
        {
            isUseable = false;
            cardUI.OnUsable(false);
            if (isSelected) OnCardDragBlocked?.Invoke(GetBlockReason());
            return;
        }

        if (mainSkill.HasCooldown && !mainSkill.IsCooldownCompleted)
        {
            isUseable = false;
            cardUI.OnUsable(false);
            if (isSelected) OnCardDragBlocked?.Invoke(Define.UseBlockReason.OnCooldown);
            return;
        }

        foreach (var cost in mainSkill.Costs)
        {
            if (!cost.HasEnoughCost(mainSkill.Owner))
            {
                isUseable = false;
                cardUI.OnUsable(false);
                if (isSelected) OnCardDragBlocked?.Invoke(GetBlockReason());
                return;
            }
        }

        bool wasUseable = isUseable;
        isUseable = true;
        cardUI.OnUsable(true);
        if (isSelected)
        {
            OnCardDragBlockCleared?.Invoke();
            if (!wasUseable)
                OnCardDrag?.Invoke(this, Mouse.current?.position.ReadValue() ?? Vector2.zero);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 클릭 시 원하는 동작을 여기에 작성
        //Select();
        //OnSelected?.Invoke(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Select();
        OnCardDragBegin?.Invoke(this);
        if (!isUseable)
            OnCardDragBlocked?.Invoke(GetBlockReason());
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (cardData.CardType == CardType.Passive) return;
        OnCardDrag?.Invoke(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Deselect();
        OnCardDragEnd?.Invoke(this, eventData.position);
        OnCardDragBlockCleared?.Invoke();
    }

    public void Select()
    {
        if (isSelected) return;
        isSelected = true;
        CardUI.OnSelected(true);
    }
    
    public void Deselect()
    {
        if (!isSelected) return;
        isSelected = false;
        CardUI.OnSelected(false);
    }
    
    public void ToggleSelect()
    {
        if (isSelected)
            Deselect();
        else
            Select();
    }
    
    public void PlayDrawAnimation(Action onComplete = null)
    {
        cardUI.PlayDrawAnimation(onComplete);
    }

    public void PlayDrawEffect(GameObject effectPrefab)
    {
        if (effectPrefab == null) return;
        drawEffectInstance = Instantiate(effectPrefab, transform);
    }

    // 오브젝트가 활성화 된 상태인지 검사하는 함수
    public void PlayFusionEffect() => cardUI.PlayFusionEffect();

    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    // 카드 사용 후 비활성화 하는 함수
    public void UseCard()
    {
        if (cardData == null || mainSkill == null) return;
        if (CardData.CardType == CardType.Spawn)
        {
            OnSpawn?.Invoke();
        }
        else if (CardData.CardType == CardType.Use && MainSkill.HasCategory("ITEM"))
        {
            OnItemEquip?.Invoke();
        }
        else if (CardData.CardType == CardType.Use)
        {
            //Debug.Log("USE");
            
            OnSkillCast?.Invoke(mainSkill);
        }
        
        ResetCardState();
        OnCardUsed?.Invoke(this);
    }
    
    // 카드 버리기 함수
    public void DiscardCard(CardData newCardData, SkillSystem skillSystem)
    {
        if (newCardData == null) return;
        // 스킬 시스템에서 해제 (패시브는 비활성화, 액티브는 그대로)
        if (newCardData?.SkillData != null && skillSystem != null)
        {
            if (newCardData.CardType == CardType.Passive)
            {
                skillSystem.GetOrRegisterSkillWithActivation(newCardData.SkillData, false);
            }
            else if (newCardData.PassiveSkillData != null)
            {
                skillSystem.GetOrRegisterSkillWithActivation(newCardData.PassiveSkillData, false);
            }
        }

        ResetCardState();
    }
    
    // 카드 상태 초기화 (내부 사용)
    private void ResetCardState()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        if (mainSkill != null)
            mainSkill.onStateChanged -= OnMainSkillStateChanged;
        Deselect();
        cardData = null;
        mainSkill = null;
        subSkill = null;
        isUseable = false;
        if (drawEffectInstance != null)
        {
            Destroy(drawEffectInstance);
            drawEffectInstance = null;
        }
        cardUI?.HideUI();
    }
    
    // 이전 버전과의 호환성을 위한 메서드 (Deprecated)
    [System.Obsolete("Use UseCard() or DiscardCard() instead")]
    public void UnEnableCard()
    {
        ResetCardState();
    }

    public void DestroyCard()
    {
        //Destroy(gameObject);
    }

    public void CheckSkillCostAvailability(StatsComponent statsComponent, Stat skillCostStat, float currentValue, float prevValue)
    {
        if (cardData == null || !IsActive() || mainSkill == null || CardData.CardType == CardType.Passive) return;
        // cost 체력, 마나
        bool isRelevantStat = false;
        foreach (var cost in mainSkill.Costs)
        {
            if (cost.ID == skillCostStat.ID)
            {
                isRelevantStat = true;
                break;
            }
        }
        if (!isRelevantStat) return;

        if (mainSkill.HasCooldown && !mainSkill.IsCooldownCompleted)
        {
            isUseable = false;
            cardUI.OnUsable(false);
            if (isSelected) OnCardDragBlocked?.Invoke(Define.UseBlockReason.OnCooldown);
            return;
        }

        foreach (var cost in mainSkill.Costs)
        {
            if (!cost.HasEnoughCost(statsComponent))
            {
                isUseable = false;
                cardUI.OnUsable(false);
                if (isSelected) OnCardDragBlocked?.Invoke(GetBlockReason());
                return;
            }
        }

        bool wasUseable = isUseable;
        isUseable = true;
        cardUI.OnUsable(true);
        if (isSelected)
        {
            OnCardDragBlockCleared?.Invoke();
            if (!wasUseable)
                OnCardDrag?.Invoke(this, Mouse.current?.position.ReadValue() ?? Vector2.zero);
        }
    }

    private void OnMainSkillStateChanged(Skill skill, State<Skill> currentState, State<Skill> prevState, int layer)
    {
        RefreshUseable();
    }

    private Define.UseBlockReason GetBlockReason()
    {
        if (mainSkill == null) return Define.UseBlockReason.NotAvailable;
        if (mainSkill.Type == SkillType.Passive) return Define.UseBlockReason.IsPassive;
        if (mainSkill.HasCooldown && !mainSkill.IsCooldownCompleted) return Define.UseBlockReason.OnCooldown;
        foreach (var cost in mainSkill.Costs)
        {
            if (!cost.HasEnoughCost(mainSkill.Owner))
                return Define.UseBlockReason.NotEnoughCost;
        }
        return Define.UseBlockReason.NotAvailable;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[RequireComponent(typeof(StatsComponent))]
public class CardComponent : BaseCharacterComponent
{
    [Header("Stat References")]
    [SerializeField] private StatData drawPointStatData;
    [SerializeField] private StatData handSizeStatData;

    [Header("UI Setup")]
    [SerializeField] private bool isFusion = false;
    [SerializeField] private GameObject cardPrefab;

    [Header("Debug")]
    [SerializeField] private CardData testDrawCard; // 테스트 드로우로 뽑을 카드

    // 런타임 데이터
    private List<DeckData> deckDatas = new();     // 사용 가능한 덱 목록
    private List<CardData> drawPile = new();     // 사용 가능한 덱 목록
    private List<CardController> hands = new();         // 핸드 카드 컨트롤러
    private GameObject handContainer; // 핸드 카드 컨테이너
    private GameObject inactiveCards; // 빈 카드 컨테이너
    private bool isPlayer; // 플레이어 여부
    private bool hasDragon = false;
    private CardData recentUseCardData;

    private readonly Queue<CardData> forcedDrawQueue = new(); // 예약된 드로우 (튜토리얼/연출용)

    private StatsComponent statsComponent;
    private Stat drawPointStat;
    private Stat handSizeStat;
    private SkillSystem skillSystem;
    private Dictionary<Skill, CardController> skillToCardMap = new();
    private Dictionary<CardData, int> fusionSuppressedCards = new();
    // 자기 덱의 융합 레시피만 보유 — 상대 덱 레시피로는 융합되지 않는다
    private Dictionary<string, CardData> fusionResultByKey = new();
    private Dictionary<CardData, List<CardData>> ownIngredientsByResult = new();
    private IEventMediator<CharacterEventType> eventMediator;
    private bool isDrawing;   // 드로우 코루틴(애니메이션+융합 판정) 진행 중 재진입 방지

    // Properties
    public int DrawPoint => Mathf.FloorToInt(drawPointStat?.CurrentValue ?? 0);
    public int MaxHandSize => Mathf.FloorToInt(handSizeStat?.CurrentValue ?? 6);
    public int CurrentHandSize => hands?.Count(h => h.IsActive()) ?? 0;
    public bool CanDraw => DrawPoint > 0 && CurrentHandSize < MaxHandSize && !isDrawing;
    public bool IsPlayer => isPlayer;
    public IReadOnlyList<DeckData> DeckDatas => deckDatas;
    public IReadOnlyList<CardData> DrawPile => drawPile;
    public IReadOnlyList<CardController> Hands => hands;
    public bool HasRecentCard => recentUseCardData != null;
    
    public delegate void CardDiscardedHandler();    
    public delegate void DrawHandler();    
    public delegate void DrawPointChangedHandler(int currentDrawPoint);
    public delegate void HandChangedHandler();
    public event CardDiscardedHandler onCardDiscarded;
    public event DrawHandler onDrawed;
    public event DrawPointChangedHandler onDrawPointChanged;
    public event HandChangedHandler onHandChanged;

    // 드래그 후 사용 실패 (타겟 없음 등) — 시간형 표시용
    public static event Action<Define.UseBlockReason> OnCardUseFailed;

    public override void Initialize(CharacterData data, IEventMediator<CharacterEventType> eventMediator)
    {
        this.eventMediator = eventMediator;
    }

    public void SetUp(Duelist duelist)
    {
        if (duelist == null || duelist.SkillSystem == null || duelist.Stats == null)
        {
            Debug.LogError($"CardComponent.SetUp: duelist {duelist} or SkillSystem {duelist.SkillSystem} or Stat {duelist.Stats} cant be null");
            return;
        }
        
        skillSystem = duelist.SkillSystem;
        statsComponent = duelist.Stats;

        // 스탯 데이터 초기화   
        statsComponent.TryGetStat(drawPointStatData, out drawPointStat);
        statsComponent.TryGetStat(handSizeStatData, out handSizeStat);

        isPlayer = duelist.IsPlayer;
        deckDatas = isPlayer ? DeckManager.Instance.SelectedDeckDatas : DeckManager.Instance.AiDeckDatas;

        // 플레이어/AI 공통: 덱 타입에 따라 융합 가능 여부 설정 + 자기 덱 레시피 등록
        fusionResultByKey.Clear();
        ownIngredientsByResult.Clear();
        foreach (var d in deckDatas)
        {
            if (d.Type != DeckType.Fusion && d.Type != DeckType.Dragon) continue;

            if (d.Type == DeckType.Dragon) hasDragon = true;
            isFusion = true;

            if (d.FusionRecipes == null) continue;
            foreach (var recipe in d.FusionRecipes)
            {
                if (recipe?.Ingredients == null || recipe.Result == null) continue;
                string key = GenerateFusionKey(recipe.Ingredients);
                fusionResultByKey[key] = recipe.Result;
                ownIngredientsByResult[recipe.Result] = recipe.Ingredients;
            }
        }

        if (isPlayer)
        {
            AddCardToDrawPile();
            handContainer = UIManager.Instance.HandContainer;
            inactiveCards = UIManager.Instance.InactiveCards;
            CreateHand(MaxHandSize);
            InputManager.Instance.OnUseSkill += UseCardSkill;
            InputManager.Instance.OnDiscard += Discard;
        }
        else
        {
            AddCardToDrawPile();
            handContainer = gameObject;
            inactiveCards = gameObject;
            CreateHand(MaxHandSize);
        }
    }

    private void AddCardToDrawPile()
    {
        if (deckDatas == null || deckDatas.Count == 0)
        {
            Debug.LogWarning("CardComponent: 선택된 덱 데이터가 없습니다.");
            return;
        }

        drawPile.Clear(); // 중복 방지
        
        foreach (DeckData deck in deckDatas)
        {
            if (deck?.Cards == null)
            {
                Debug.LogWarning($"CardComponent: 덱 {deck?.name}의 카드 리스트가 null입니다.");
                continue;
            }
            
            foreach (CardData card in deck.Cards)
            {
                if (card == null)
                {
                    Debug.LogWarning($"CardComponent: 덱 {deck.name}에서 null 카드를 발견했습니다.");
                    continue;
                }

                Skill skill = skillSystem.GetOrRegisterSkill(card.SkillData);
                if (skill.IsPassive) skillSystem.SetPassiveSkillActive(skill, false);
                if (!card.IsDrawable) continue;
                //Debug.Log($"{card} 추가");
                for (int i = 0; i < card.CardCount; i++)
                    drawPile.Add(card);
            }
        }

        if (drawPile.Count == 0)
        {
            Debug.LogError("CardComponent: 뽑을 수 있는 카드가 없습니다!");
        }
    }

    private void CreateHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject card = Instantiate(cardPrefab, inactiveCards.transform);
            CardController cardController = card.GetComponent<CardController>();
            
            if (isPlayer)
            {
                InputManager.Instance.SubscribeToCardEvent(cardController);
            }
            // 카드 이벤트 구독
            cardController.OnCardUsed += OnCardUsed;
            statsComponent.onStatValueChanged += cardController.CheckSkillCostAvailability;
            cardController.gameObject.SetActive(false);
            hands.Add(cardController);
        }
    }

    public bool DrawCardWithPoint()
    {
        if (!CanDraw)
        {
            if (isPlayer) PlaySound(UISoundType.Click);
            return false;
        }
        if (isPlayer) PlaySound(UISoundType.Draw);
        drawPointStat.CurrentValue--;
        CardData card = DrawFromPile();
        if (card == null) return false;

        isDrawing = true;
        StartCoroutine(DrawCardCoroutine(card, () =>
        {
            isDrawing = false;
            onDrawed?.Invoke();
        }, null));
        return true;
    }

    public void DrawCardWithoutPoint(int drawCount, GameObject effectPrefab = null)
    {
        StartCoroutine(DrawCardsCoroutine(drawCount, null, effectPrefab));
    }

    /// <summary>
    /// 다음 드로우 결과를 예약한다 (튜토리얼/연출용). 큐가 비어 있으면 평소처럼 랜덤 드로우.
    /// 예약 카드는 스킬 등록을 위해 선택된 덱에 포함되어 있어야 한다.
    /// </summary>
    public void EnqueueForcedDraw(CardData card)
    {
        if (card == null) return;
        forcedDrawQueue.Enqueue(card);
    }

    private CardData DrawFromPile()
    {
        if (forcedDrawQueue.Count > 0) return forcedDrawQueue.Dequeue();

        if (drawPile == null || drawPile.Count == 0)
        {
            Debug.LogWarning("CardComponent: DrawPile이 비어있거나 null입니다. 카드를 뽑을 수 없습니다.");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, drawPile.Count);
        CardData cardData = drawPile[randomIndex];

        if (cardData == null)
        {
            Debug.LogError($"CardComponent: DrawPile의 인덱스 {randomIndex}에서 null CardData를 발견했습니다.");
            return null;
        }
        return cardData;
    }
    
    public void DrawCardsFromSpecificDeck(int drawCount, DeckData targetDeck, GameObject effectPrefab = null)
    {
        StartCoroutine(DrawCardsCoroutine(drawCount, targetDeck, effectPrefab));
    }

    private IEnumerator DrawCardsCoroutine(int drawCount, DeckData targetDeck, GameObject effectPrefab = null)
    {
        for (int i = 0; i < drawCount; i++)
        {
            CardData card = targetDeck != null ? DrawFromSpecificDeck(targetDeck) : DrawFromPile();
            if (card == null) yield break;
            yield return StartCoroutine(DrawCardCoroutine(card,() => onDrawed?.Invoke(), effectPrefab));
        }
        PlaySound(UISoundType.Draw);
    }

    private IEnumerator DrawCardCoroutine(CardData drawnCard, System.Action onComplete, GameObject effectPrefab = null)
    {
        var slot = AddCardToHand(drawnCard);
        if (slot == null) yield break;

        bool animDone = false;
        slot.PlayDrawAnimation(() => animDone = true);
        yield return new WaitUntil(() => animDone);

        if (effectPrefab != null)
            slot.PlayDrawEffect(effectPrefab);

        if (isFusion) TryFusion(drawnCard, slot);
        onHandChanged?.Invoke();
        onComplete?.Invoke();
    }

    private CardData DrawFromSpecificDeck(DeckData targetDeck)
    {
        var deckCards = drawPile.Where(card => deckDatas.Any(deck => deck == targetDeck && deck.Cards.Contains(card))).ToList();

        if (deckCards.Count == 0) return null;

        int randomIndex = Random.Range(0, deckCards.Count);
        
        return deckCards[randomIndex];
    }

    /// <summary>카드 이름을 정렬해 "+"로 이어붙인 융합 레시피 조회 키를 만든다.</summary>
    private static string GenerateFusionKey(List<CardData> cards)
    {
        List<string> names = new();
        foreach (CardData card in cards)
        {
            names.Add(card.name);
        }

        names.Sort();
        return string.Join("+", names);
    }

    /// <summary>자기 덱에 등록된 레시피로만 융합 결과를 조회한다.</summary>
    private CardData TryGetFusionResult(List<CardData> cards)
    {
        string key = GenerateFusionKey(cards);
        return fusionResultByKey.TryGetValue(key, out var result) ? result : null;
    }

    /// <summary>자기 덱 레시피 기준으로 융합 결과 카드의 재료 목록을 조회한다.</summary>
    private List<CardData> GetOwnIngredients(CardData fusionResult)
        => ownIngredientsByResult.TryGetValue(fusionResult, out var list) ? list : null;

    private bool TryFusion(CardData newCardData, CardController newSlot)
    {
        CardData fusionData = null;
        foreach (CardController cardController in hands.Where(h => h.IsActive() && h != newSlot))
        {
            List<CardData> cardDatas = new() { cardController.CardData, newCardData };
            fusionData = TryGetFusionResult(cardDatas);
            if (fusionData != null)
            {
                DeactivateCardPassive(cardController.CardData);
                cardController.Bind(fusionData, skillSystem);
                cardController.PlayFusionEffect();
                if (newSlot != null) DiscardSilently(newSlot);
                RegisterFusionSuppression(fusionData);
                drawPointStat.CurrentValue++;
                return true;
            }
        }

        if (fusionData == null && hasDragon && hands.Count(h => h.IsActive() && h != newSlot) >= 5)
        {
            List<CardData> cardDatas = new();
            foreach (CardController cardController in hands.Where(h => h.IsActive() && h != newSlot))
            {
                cardDatas.Add(cardController.CardData);
            }
            cardDatas.Add(newCardData);
            fusionData = TryGetFusionResult(cardDatas);
            if (fusionData != null)
            {
                bool isBind = false;
                int count = hands.Count;
                for (int i = 0; i < count; i++)
                {
                    if (hands[i].IsActive() && hands[i] != newSlot)
                    {
                        DeactivateCardPassive(hands[i].CardData);
                        if (!isBind)
                        {
                            hands[i].Bind(fusionData, skillSystem);
                            hands[i].PlayFusionEffect();
                            isBind = true;
                        }
                        else
                        {
                            DiscardSilently(hands[i]);
                        }
                    }
                }
                if (newSlot != null) DiscardSilently(newSlot);
                RegisterFusionSuppression(fusionData);
                drawPointStat.CurrentValue += count;
                return true;
            }
        }
        return false;
    }

    private CardController AddCardToHand(CardData card)
    {
        // 비활성화 된 핸드 슬롯을 찾아서 카드데이터 바인딩
        var emptySlot = hands.FirstOrDefault(h => !h.IsActive());
        if (emptySlot != null)
        {
            emptySlot.transform.SetParent(handContainer.transform, false);
            emptySlot.Bind(card, skillSystem);

            RegisterFusionSuppression(card);
            if (fusionSuppressedCards.ContainsKey(card))
                DeactivateCardPassive(card);

            if (isPlayer)
            {
                var rt = handContainer.GetComponent<RectTransform>();
                if (rt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            return emptySlot;
        }
        return null;
    }

    public override void SubscribeEvent()
    {
        if (drawPointStat != null)
        {
            drawPointStat.onValueChanged += OnDrawPointChanged;
        }

        if (handSizeStat != null)
        {
            handSizeStat.onValueChanged += OnHandSizeChanged;
        }

        var owner = GetComponent<BaseCharacter>();

        // 플레이어만 UI 이벤트 구독
        /*if (owner.IsPlayer)
        {
            UIManager.Instance.OnDrawed += DrawCardWithPoint;
        }*/
    }

    public override void UnsubscribeEvent()
    {
        if (drawPointStat != null)
        {
            drawPointStat.onValueChanged -= OnDrawPointChanged;
        }

        if (handSizeStat != null)
        {
            handSizeStat.onValueChanged -= OnHandSizeChanged;
        }

        var owner = GetComponent<BaseCharacter>();

        // 플레이어만 UI 이벤트 구독 해제
        /*if (owner.IsPlayer)
        {
            UIManager.Instance.OnDrawed -= DrawCardWithPoint;
        }*/
    }

    private void OnDrawPointChanged(Stat stat, float current, float prev)
    {
        // UI 업데이트 등
        onDrawPointChanged?.Invoke(DrawPoint);
    }

    private void OnHandSizeChanged(Stat stat, float current, float prev)
    {
        // 핸드 최대 사이즈 변경에 따른 핸드 슬롯 조정
        int newSize = Mathf.FloorToInt(current);
        int oldSize = Mathf.FloorToInt(prev);

        if (newSize > oldSize)
        {
            // 핸드 슬롯 추가
            int slotsToAdd = newSize - hands.Count;
            if (slotsToAdd > 0)
            {
                CreateHand(slotsToAdd);
            }
        }
    }

    public bool UseCardSkill(CardController cardController)
    {
        if (cardController == null || cardController.CardData == null || cardController.MainSkill == null)
        {
            Debug.LogWarning("CardComponent.UseCardSkill: Invalid card parameters");
            return false;
        }
        if (skillSystem == null)
        {
            Debug.LogError("CardComponent.UseCardSkill: SkillSystem is null");
            return false;
        }
        
        // 스킬 사용 시도 — 실제 발동(Activate)은 조건 전이라 항상 다음 Update 틱 이후이므로,
        // 성공했을 때만 구독해도 발동을 놓치지 않음
        bool success = skillSystem.UseCardSkill(cardController.MainSkill);
        if (!success)
            return false;

        var skill = cardController.MainSkill;
        // 장비 카드는 소모되지 않으므로 발동-소모 파이프라인에서 제외
        if (cardController.CardData.CardType != CardType.Equipment)
        {
            skillToCardMap[skill] = cardController;
            skill.onActivated += OnCardSkillActivated;
            skill.onCanceled += OnCardSkillCanceled;
        }

        skill.onTargetSelectionCompleted += OnTargetSelectionCompleted;

        return true;
    }

    private void OnDestroy()
    {
        InputManager.Instance.OnUseSkill -= UseCardSkill;
        InputManager.Instance.OnDiscard -= Discard;
        foreach (var pendingSkill in skillToCardMap.Keys)
        {
            pendingSkill.onActivated -= OnCardSkillActivated;
            pendingSkill.onCanceled -= OnCardSkillCanceled;
            pendingSkill.onTargetSelectionCompleted -= OnTargetSelectionCompleted;
        }
        skillToCardMap.Clear();
        foreach (var cardController in hands)
        {
            if (isPlayer)
            {
                InputManager.Instance.UnsubscribeToCardEvent(cardController);
                cardController.OnCardUsed -= OnCardUsed;
            }
            if (statsComponent != null)
            {
                statsComponent.onStatValueChanged -= cardController.CheckSkillCostAvailability;
            }
        }
    }

    private void OnTargetSelectionCompleted(Skill skill, TargetSearcher searcher, TargetSelectionResult result)
    {
        skill.onTargetSelectionCompleted -= OnTargetSelectionCompleted;
        if (result.resultMessage == SearchResultMessage.Fail
         || result.resultMessage == SearchResultMessage.OutOfRange)
        {
            // 대상 없는 곳에 드롭 — 취소 이벤트 없이 Ready로 돌아가므로 여기서 정리
            CleanupPending(skill, out _);
            OnCardUseFailed?.Invoke(Define.UseBlockReason.NoValidTarget);
        }
    }

    private void OnCardSkillActivated(Skill skill)
    {
        CleanupPending(skill, out var cardController);
        if (cardController == null) return;

        // 최근 사용 카드 저장
        bool isChangeCard = skill.HasAction<ChangeCardAction>();
        var rebindTarget = recentUseCardData;
        if (!isChangeCard && cardController.CardData.SkillData.CodeName != "DUEL_LIGHT_USE_SEALOFHEAVEN")
            recentUseCardData = cardController.CardData;

        cardController.UseCard();

        if (isChangeCard && rebindTarget != null)
        {
            cardController.transform.SetParent(handContainer.transform, false);
            cardController.Bind(rebindTarget, skillSystem);
            recentUseCardData = rebindTarget;
            onHandChanged?.Invoke();
        }
    }

    private void OnCardSkillCanceled(Skill skill) => CleanupPending(skill, out _);

    // 발동/취소/대상 실패 어느 쪽으로 끝나든 스킬-카드 매핑과 구독을 대칭적으로 정리
    private void CleanupPending(Skill skill, out CardController cardController)
    {
        skillToCardMap.TryGetValue(skill, out cardController);
        skillToCardMap.Remove(skill);
        skill.onActivated -= OnCardSkillActivated;
        skill.onCanceled -= OnCardSkillCanceled;
        skill.onTargetSelectionCompleted -= OnTargetSelectionCompleted;
    }

    public void Discard(CardController cardController)
    {
        if (cardController == null || cardController.CardData == null)
        {
            Debug.LogWarning("CardComponent.Discard: Invalid card parameters");
            return;
        }

        // 버리기 시 드로우 포인트 획득
        if (cardController.CardData.DrawPointFromDiscard != 0 && drawPointStat != null)
        {
            drawPointStat.CurrentValue += cardController.CardData.DrawPointFromDiscard;
        }
        
        // 카드 위치 및 상태 초기화
        if (inactiveCards != null)
        {
            cardController.transform.SetParent(inactiveCards.transform, false);
        }
        
        PlaySound(UISoundType.Discard);
        MoveCardToInactive(cardController);
        ReleaseFusionSuppression(cardController.CardData);
        cardController.DiscardCard(cardController.CardData, skillSystem);
        
        // 통계 업데이트
        onCardDiscarded?.Invoke();
        onHandChanged?.Invoke();
        BattleManager.Instance?.AddDiscardCardCount();
    }
    
    public void DiscardSilently(CardController cardController)
    {
        if (cardController == null || cardController.CardData == null)
        {
            Debug.LogWarning("CardComponent.Discard: Invalid card parameters");
            return;
        }
        
        // 카드 위치 및 상태 초기화
        if (inactiveCards != null)
        {
            cardController.transform.SetParent(inactiveCards.transform, false);
        }
        
        ReleaseFusionSuppression(cardController.CardData);
        cardController.DiscardCard(cardController.CardData, skillSystem);
        onHandChanged?.Invoke();
    }

    public void ChangeCard()
    {
        if (recentUseCardData == null) return;
        AddCardToHand(recentUseCardData);
    }
    
    // 카드 이벤트 핸들러
    private void OnCardUsed(CardController cardController)
    {
        // 카드 사용 처리 (이미 SkillSystem에서 처리됨)
        if(isPlayer)MoveCardToInactive(cardController);
        onHandChanged?.Invoke();
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void RegisterFusionSuppression(CardData fusionResult)
    {
        // 융합 재료 확인
        var ingredients = GetOwnIngredients(fusionResult);
        if (ingredients == null) return;

        foreach (var ingredient in ingredients)
        {
            // 융합 재료 참조 카운트 증가
            fusionSuppressedCards.TryGetValue(ingredient, out int count);
            fusionSuppressedCards[ingredient] = count + 1;

            // 기존 핸드에 여분의 재료 패시브 카드가 있다면 비활성화
            if (hands.Any(h => h.IsActive() && h.CardData == ingredient))
                DeactivateCardPassive(ingredient);
        }
    }

    private void ReleaseFusionSuppression(CardData fusionResult)
    {
        // 융합 재료 확인
        List<CardData> ingredients = GetOwnIngredients(fusionResult);
        if (ingredients == null) return;

        foreach (CardData ingredient in ingredients)
        {
            if (!fusionSuppressedCards.TryGetValue(ingredient, out int count)) continue;

            count--;
            if (count <= 0)
            {
                fusionSuppressedCards.Remove(ingredient);
                if (hands.Any(h => h.IsActive() && h.CardData == ingredient))
                    ActivateCardPassive(ingredient);
            }
            else
            {
                fusionSuppressedCards[ingredient] = count;
            }
        }
    }

    private void DeactivateCardPassive(CardData cardData)
    {
        if (cardData.CardType == CardType.Passive)
            skillSystem.GetOrRegisterSkillWithActivation(cardData.SkillData, false);
        else if (cardData.PassiveSkillData != null)
            skillSystem.GetOrRegisterSkillWithActivation(cardData.PassiveSkillData, false);
    }

    private void ActivateCardPassive(CardData cardData)
    {
        if (cardData.CardType == CardType.Passive)
            skillSystem.GetOrRegisterSkillWithActivation(cardData.SkillData, true);
        else if (cardData.PassiveSkillData != null)
            skillSystem.GetOrRegisterSkillWithActivation(cardData.PassiveSkillData, true);
    }

    private void MoveCardToInactive(CardController cardController)
    {
        if (cardController != null && inactiveCards != null)
        {
            cardController.transform.SetParent(inactiveCards.transform, false);
        }
    }

    private void PlaySound(UISoundType soundType)
    {
        if (!IsPlayer) return;
        AudioManager.Instance.PlayUISound(soundType);
    }
    
    // 디버그/유틸리티 메서드
    [ContextMenu("Draw Test Card")]
    private void DrawTestCardForTest()
    {
        if (testDrawCard == null)
        {
            Debug.LogWarning("CardComponent: testDrawCard가 비어 있습니다. 인스펙터에서 카드를 지정하세요.");
            return;
        }
        EnqueueForcedDraw(testDrawCard);
        DrawCardWithoutPoint(1);
    }

    public int GetHandCount() => hands.Count;
    public int GetActiveHandCount() => hands.Where((h) => h.IsActive()).Count();
    public IReadOnlyList<CardData> GetHandCards() => hands.Where(h => h.IsActive()).Select(h => h.CardData).ToList();
    public IReadOnlyList<CardController> GetCardsByCardType(CardType cardType) => Hands.Where(h => h.IsActive() && h.CardData?.CardType == cardType).ToList();
}

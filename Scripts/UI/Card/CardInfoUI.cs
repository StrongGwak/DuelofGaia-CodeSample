using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 사용자의 입력과 상호작용을 시각적으로 처리하는 UI 컴포넌트
/// 카드 사용, 텍스트 표시 등 View 역할을 담당
/// 내부 로직은 CardController에 위임
/// </summary>
public class CardInfoUI : BaseUI
{
    [Header("Card Info")]
    [SerializeField] private Image cardImage;   // 카드 일러스트
    [SerializeField] private Image cardBackground;  // 카드 배경
    [SerializeField] private Image useFrame;   // 주문 카드 프레임 (기존 cardFrame에서 변경)
    [SerializeField] private Image spawnFrame;   // 몬스터 소환 카드 프레임 (추가)
    [SerializeField] private Image deckIcon;    // 덱 아이콘
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI spawnDesc;
    [SerializeField] private TextMeshProUGUI useDesc;
    [SerializeField] private TextMeshProUGUI manaCostText;  // GameObject 없이 Text만
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private GameObject deco;  // 추가
    
    [Header("Selected Event")]
    [SerializeField] private GameObject glow;
    [SerializeField] private Image glowImage;
    [SerializeField] private float defaultWidth = 200;

    private CardType cardType;
    public CardType CardType => cardType;

    private void Awake()
    {
        glowImage = glow.GetComponent<Image>();
    }

    // 카드 정보를 통해서 UI 세팅
    public void Setup(CardData cardData, DeckData deckData = null)
    {
        if (cardData == null || cardData.SkillData == null) return;
        SkillData skillData = cardData.SkillData;
        cardName.text = LocalizationSettings.StringDatabase.GetLocalizedString("CardName", cardData.name);
        cardType = cardData.CardType;

        RefreshUI();

        if (cardImage != null) cardImage.sprite = cardData.SkillData.Icon;

        // deckData 없으면 기존대로 조회
        if (deckData == null) deckData = DeckManager.Instance.GetDeckDataByCardData(cardData);
        if (deckData == null) return;

        if (deckData.Type != DeckType.Duel && deckData.Type != DeckType.Dragon) deco.SetActive(true);
        if (deckIcon != null) deckIcon.sprite = deckData.DeckIcon;

        if (cardData.CardType == CardType.Passive)
        {
            manaCostText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", cardData.CardType.ToString() + "Card");
            useDesc.text = skillData.Description;
            return;
        }

        foreach (var cost in skillData.SkillLevelDatas[0].costs)
        {
            if (cost.CodeName == "MANA")
            {
                manaCostText.text = cost.Value.ToString();
                break;
            }
        }

        // 프레임 토글 로직 추가
        if (cardData.CardType == CardType.Spawn)
        {
            if (spawnFrame != null)
            {
                spawnFrame.gameObject.SetActive(true);
                useFrame.gameObject.SetActive(false);
            }
            if (cardBackground != null) cardBackground.sprite = deckData.SpawnBackgroundSprite;
            
            spawnDesc.text = skillData.Description;
            
            var (health, damage) = skillData.GetSummonMonsterStats();
            healthText.text = health.ToString();
            damageText.text = damage.ToString();
        }
        else
        {
            useDesc.text = skillData.Description;
        }
    }

    private void RefreshUI()
    {
        spawnDesc.text = "";
        useDesc.text = "";
        healthText.text = "";
        damageText.text = "";
        manaCostText.text = "";
        deco.SetActive(false);
        spawnFrame.gameObject.SetActive(false);
        useFrame.gameObject.SetActive(true);
    }

    public void OnSelected(bool isSelected)
    {
        glow.SetActive(isSelected);
    }
}

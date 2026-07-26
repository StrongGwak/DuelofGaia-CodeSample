using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    private static DeckManager instance;
    [Header("Deck Infomation")]
    [SerializeField] private List<DeckData> deckDatas;
    [SerializeField] private List<DeckData> selectedDeckDatas;
    [SerializeField] private List<DeckData> aiDeckDatas;
    
    private Dictionary<CardData, DeckData> cardToDeckMap = new Dictionary<CardData, DeckData>();

    public static DeckManager Instance => instance;
    public List<DeckData> DeckDatas => deckDatas;
    public List<DeckData> AiDeckDatas => aiDeckDatas;
    public List<DeckData> SelectedDeckDatas => selectedDeckDatas;
    
    public delegate void SelectDeckHandler(List<DeckData> deckDatas);
    public event SelectDeckHandler OnSelectDeck;
    public event SelectDeckHandler OnChangedAiDeck;

    // 카드와 덱 매핑

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
        
        // DeckData들을 순회하면서 매핑 생성
        foreach(var deckData in deckDatas)
        {
            foreach(var card in deckData.Cards)
            {
                cardToDeckMap[card] = deckData;
            }
        }
    }

    public void SelectDeck(bool isSelected, DeckData deckData)
    {
        if (isSelected)
        {
            selectedDeckDatas.Add(deckData);
            OnSelectDeck?.Invoke(selectedDeckDatas);
        }
        else
        {
            selectedDeckDatas.Remove(deckData);
            OnSelectDeck?.Invoke(selectedDeckDatas);
        }
    }

    public void SelectAiDeck(List<DeckData> stageDeckDatas)
    {
        foreach (var stageDeckData in stageDeckDatas)
        {
            aiDeckDatas.Add(stageDeckData);
        }
        OnChangedAiDeck?.Invoke(aiDeckDatas);
    }

    public void AddAiDeck(DeckData deckData)
    {
        if (deckData == null) return;
        aiDeckDatas.Add(deckData);
        OnChangedAiDeck?.Invoke(aiDeckDatas);
    }

    public void RemoveAiDeck(DeckData deckData)
    {
        if (deckData == null) return;
        aiDeckDatas.Remove(deckData);
        OnChangedAiDeck?.Invoke(aiDeckDatas);
    }

    public void ClearSelectedAiDeck()
    {
        aiDeckDatas.Clear();
        OnChangedAiDeck?.Invoke(aiDeckDatas);
    }

    public int GetDeckDataCount()
    {
        return selectedDeckDatas.Count;
    }

    public void ClearSelectedDeck()
    {
        selectedDeckDatas.Clear();
        OnSelectDeck?.Invoke(selectedDeckDatas);
    }

    public DeckData GetDeckDataByCardData(CardData cardData)
    {
        if (cardToDeckMap.TryGetValue(cardData, out DeckData deckData))
        {
            return deckData;
        }
        //Debug.LogWarning($"[DeckManager] cardToDeckMap에서 {cardData?.name}를 찾을 수 없습니다.");
        return null;
    }

}

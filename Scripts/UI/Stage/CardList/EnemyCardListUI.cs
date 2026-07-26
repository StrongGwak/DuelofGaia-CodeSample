using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyCardListUI : MonoBehaviour
{
    [SerializeField] private GameObject cardButtonPrefab;
    [SerializeField] private Transform listParent;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private List<CardInfoButton> cardButtons = new();

    private void Awake()
    {
        for (int i = cardButtons.Count; i < initialPoolSize; i++)
        {
            var go = Instantiate(cardButtonPrefab, listParent);
            go.SetActive(false);
            cardButtons.Add(go.GetComponent<CardInfoButton>());
        }
        
        foreach (var cardInfoButton in cardButtons)
        {
            cardInfoButton.gameObject.SetActive(true);
        }

        ClearAllCards();
    }

    private void Start()
    {
        // 패널이 CanvasGroup으로 관리되어 OnEnable이 열림 시점에 불리지 않으므로
        // 씬 수명 기준(Start/OnDestroy)으로 구독한다
        DeckManager.Instance.OnChangedAiDeck += ChangedAiDeck;
    }

    private void OnDestroy()
    {
        if (DeckManager.Instance != null)
            DeckManager.Instance.OnChangedAiDeck -= ChangedAiDeck;
    }

    private void ChangedAiDeck(List<DeckData> deckDatas)
    {
        RefreshCardList(deckDatas);
    }

    private void RefreshCardList(List<DeckData> deckDatas)
    {
        // 1단계: 모든 카드 숨기기
        ClearAllCards();
        
        // 2단계: 선택된 덱의 카드들 표시
        var cardIndex = 0;

        foreach (var deck in deckDatas)
        {
            foreach (var card in deck.Cards)
            {
                if (cardIndex >= cardButtons.Count)                                                                                                                                                                                  
                {                                                                                                                                                                                                                  
                    var cardButton = Instantiate(cardButtonPrefab, listParent);
                    cardButtons.Add(cardButton.GetComponent<CardInfoButton>());
                }

                cardButtons[cardIndex].Setup(deck, card);
                cardIndex++;
            }
        }
        
        for (int i = 0; i < cardIndex; i++)
            cardButtons[i].gameObject.SetActive(true);

        StartCoroutine(RebuildLayoutNextFrame());
    }

    private IEnumerator RebuildLayoutNextFrame()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(listParent as RectTransform);
    }

    private void ClearAllCards()
    {
        foreach (var button in cardButtons) button.gameObject.SetActive(false);
    }

}

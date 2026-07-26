using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class CardListUI : BaseUI, ILocalizable
  {
      [SerializeField] private TextMeshProUGUI cardListText;
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

      protected override void OnEnable()
      {
          base.OnEnable();
          UpdateTexts();
          DeckManager.Instance.OnSelectDeck += HandleDeckSelected;
          LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
      }

      protected override void OnDisable()
      {
          ClearAllCards();
          DeckManager.Instance.OnSelectDeck -= HandleDeckSelected;
          LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
          base.OnDisable();
      }
      
      private void OnLocaleChanged(Locale _) => UpdateTexts();

      public void UpdateTexts()
      {
          cardListText.text = LocalizationSettings.StringDatabase.GetLocalizedString("CommonUI", "CardList");
      }

      private void HandleDeckSelected(List<DeckData> selectedDecks)
      {
          // 덱이 선택되든 취소되든 항상 전체 갱신
          RefreshCardList(selectedDecks);
      }

      private void RefreshCardList(List<DeckData> deckDatas)
      {
          ClearAllCards();

          int cardIndex = 0;
          foreach (var deck in deckDatas)
          {
              foreach (var card in deck.Cards)
              {
                  // 비활성 상태에서 데이터 세팅
                  CardInfoButton btn = cardButtons.Count > cardIndex
                      ? cardButtons[cardIndex]
                      : GetOrCreateCardButton(cardIndex).GetComponent<CardInfoButton>();

                  btn.Setup(deck, card);
                  cardIndex++;
              }
          }

          // 한 번에 활성화
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
          foreach (var button in cardButtons)
          {
              button.gameObject.SetActive(false);
          }
      }
      
      private GameObject GetOrCreateCardButton(int index) 
      {
          if (index >= cardButtons.Count)
          {
              GameObject cardButton = Instantiate(cardButtonPrefab, listParent);                                                                                                                                                   
              cardButtons.Add(cardButton.GetComponent<CardInfoButton>());
          }
          return cardButtons[index].gameObject;
      }

  }
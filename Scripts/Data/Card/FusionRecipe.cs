using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Scriptable Objects/FusionRecipe")]
public class FusionRecipe : ScriptableObject
{
    [Header("재료 카드 (2개)")]
    [SerializeField] 
    private List<CardData> ingredients;

    [Header("조합 결과")]
    [SerializeField] 
    private CardData result;

    public List<CardData> Ingredients => ingredients;
    public CardData Result => result;

}

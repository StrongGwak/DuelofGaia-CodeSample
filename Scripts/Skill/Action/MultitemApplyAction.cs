using NUnit.Framework.Internal;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ItemBundle
{
    [SerializeField] private SkillData itemSkillData;

    public string ItemName => itemSkillData.DisplayName;
    public SkillData ItemSkillData => itemSkillData;
}

[System.Serializable]
public class MultitemApplyAction : SkillAction
{
    [Header("Random Item Settings")]
    [SerializeField] private ItemBundle[] possibleItems;

    private CategoryData hasItemCat;

    public override void Start(Skill skill)
    {
        if (!hasItemCat)
            hasItemCat = Managers.Data.GetByCode<CategoryData>("HAS_ITEM");
    }

    public override void Apply(Skill skill)
    {
        Debug.Log("MultitemApplyAction Apply");
        if (possibleItems == null || possibleItems.Length == 0)
        {
            Debug.LogWarning("No items to apply!");
            return;
        }

        var randomItem = possibleItems[Random.Range(0, possibleItems.Length)];

        if (randomItem.ItemSkillData == null)
        {
            Debug.LogWarning($"ItemBundle '{randomItem.ItemName}' has no SkillData assigned!");
            return;
        }

        foreach (var target in skill.Targets)
        {
            if (target.HasCategory(hasItemCat))
            {
                Debug.Log($"{target.name} already has an item. Skipping item application.");
                continue;
            }
            //Debug.Log($"Applying item '{randomItem.ItemName}' to {target.name}");

            Skill itemSkill = new Skill(randomItem.ItemSkillData);
            itemSkill.Setup(skill.Owner);
            target.SkillSystem.Apply(itemSkill);
        }
    }

    protected override IReadOnlyDictionary<string, string> GetStringsByKeyword()
    {
        var descriptionValuesByKeyword = new Dictionary<string, string>();

        for (int i = 0; i < possibleItems.Length; i++)
        {
            if (possibleItems[i] != null)
            {
                descriptionValuesByKeyword[$"itemName{i}"] = possibleItems[i].ItemName;
            }
        }

        return descriptionValuesByKeyword;
    }

    public override object Clone()
    {
        return new MultitemApplyAction()
        {
            possibleItems = possibleItems,
        };
    }
}
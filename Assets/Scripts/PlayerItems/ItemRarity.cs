using UnityEngine;

public enum ItemRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public static class ItemRarityUtility
{
    public static float GetWeight(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return 10f;
            case ItemRarity.Uncommon:
                return 6f;
            case ItemRarity.Rare:
                return 3f;
            case ItemRarity.Epic:
                return 1.5f;
            case ItemRarity.Legendary:
                return 0.75f;
            default:
                return 1f;
        }
    }
}

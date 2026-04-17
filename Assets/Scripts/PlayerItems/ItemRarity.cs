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

    public static Color GetDisplayColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Color.white;
            case ItemRarity.Uncommon:
                return new Color(0.45f, 0.95f, 0.55f, 1f);
            case ItemRarity.Rare:
                return new Color(0.35f, 0.65f, 1f, 1f);
            case ItemRarity.Epic:
                return new Color(0.78f, 0.45f, 1f, 1f);
            case ItemRarity.Legendary:
                return new Color(1f, 0.72f, 0.2f, 1f);
            default:
                return Color.white;
        }
    }
}

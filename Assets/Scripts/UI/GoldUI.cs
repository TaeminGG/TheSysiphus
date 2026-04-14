using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour
{
    private TextMeshProUGUI text;
    private PlayerStats playerStats;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            return;

        playerStats = player.GetComponent<PlayerStats>();
        if (playerStats == null)
            return;

        playerStats.OnGoldChanged += UpdateGoldText;
        UpdateGoldText(playerStats.GetGold());
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnGoldChanged -= UpdateGoldText;
    }

    private void UpdateGoldText(int gold)
    {
        if (text != null)
            text.text = gold.ToString();
    }
}

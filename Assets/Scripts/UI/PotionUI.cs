using TMPro;
using UnityEngine;

public class PotionUI : MonoBehaviour
{
    public TextMeshProUGUI text;

    public PlayerStats player;

    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        player = GameObject.Find("Player").GetComponent<PlayerStats>();
    }
    private void Update()
    {
        text.text = player.GetPotionCount().ToString();
    }
}

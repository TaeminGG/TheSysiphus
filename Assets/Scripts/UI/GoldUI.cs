using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
        playerStats = GameObject.Find("Player").GetComponent<PlayerStats>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = playerStats.GetGold().ToString();
    }
}

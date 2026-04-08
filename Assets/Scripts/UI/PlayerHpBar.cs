using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHpBar : MonoBehaviour
{
    private PlayerStats playerStats;
    private float currentHp = 0;
    private float maxHp = 0;

    [Header("fill")]
    public float minFill = 0.09f;
    public float maxFill = 0.96f;

    [Header("Texts")]
    public TextMeshProUGUI currentHpText;
    public TextMeshProUGUI slashText;
    public TextMeshProUGUI maxHpText;

    [Header("image")]
    public Image hpbar;

    private void Awake()
    {

    }
    void Start()
    {
        playerStats = GameObject.Find("Player").GetComponent<PlayerStats>();
        slashText.text = "/";
    }

    void Update()
    {
        currentHp = Mathf.RoundToInt(playerStats.GetCurrentHp());
        maxHp = Mathf.RoundToInt(playerStats.GetMaxHp());

        float ratio = currentHp / maxHp;

        hpbar.fillAmount = Mathf.Lerp(minFill, maxFill, ratio);

        currentHpText.text = currentHp.ToString();
        maxHpText.text = maxHp.ToString();
    }
}

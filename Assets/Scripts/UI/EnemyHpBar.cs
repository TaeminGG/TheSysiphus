using UnityEngine;
using UnityEngine.UI;
public class EnemyHpBar : MonoBehaviour
{
    [SerializeField]
    private Image bar;


    public void HpSetting(float amount)
    {
        bar.fillAmount = amount;

        if(bar.fillAmount <= 0f)
        {
            HideObject();
        }

    }

    private void HideObject()
    {
        if(bar.fillAmount <= 0f || bar.fillAmount >= 1f)
        {
            bar.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}

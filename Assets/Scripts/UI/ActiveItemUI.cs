using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ActiveItemUI : MonoBehaviour
{
    public Image currentIcon;
    public Image nextIcon;
    public Image thirdIcon;
    public TextMeshProUGUI text;

    private ActiveInventory inventory;

    private Coroutine fade;


    public float duration = 1f;             // 아이템 교체 시 현재 아이템 이름 표시 시간

    private void Start()
    {
        inventory = GameObject.Find("Player").GetComponent<ActiveInventory>();
    }

    private void Update()
    {
        if (inventory.isChanged)
        {
            inventory.isChanged = false;
            text.text = inventory.GetItems()[0].itemName;

            if(fade != null)
                StopCoroutine(fade);
            fade = StartCoroutine(Fade());
        }

        if (inventory.GetItems().Count > 2)
        {
            currentIcon.enabled = true;
            nextIcon.enabled = true;
            thirdIcon.enabled = true;

            currentIcon.sprite = inventory.GetItems()[0].icon;
            nextIcon.sprite = inventory.GetItems()[1].icon;
            thirdIcon.sprite = inventory.GetItems()[2].icon;
        }
        else
        {
            if (inventory.GetItems().Count == 2)
            {
                currentIcon.enabled = true;
                nextIcon.enabled = true;
                thirdIcon.enabled = false;

                currentIcon.sprite = inventory.GetItems()[0].icon;
                nextIcon.sprite = inventory.GetItems()[1].icon;
            }
            else if (inventory.GetItems().Count == 1)
            {
                currentIcon.enabled = true;
                nextIcon.enabled = false;
                thirdIcon.enabled = false;

                currentIcon.sprite = inventory.GetItems()[0].icon;
            }
            else
            {
                currentIcon.enabled = false;
                nextIcon.enabled = false;
                thirdIcon.enabled = false;
            }
        }
    }

    private IEnumerator Fade()
    {
        float timer = 0f;

        Color startColor = text.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // 점점 투명하게
            text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }
    }
}

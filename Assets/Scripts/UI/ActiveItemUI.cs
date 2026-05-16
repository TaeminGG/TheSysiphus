using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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
        GameObject player = GameObject.Find("Player");
        if (player != null)
            inventory = player.GetComponent<ActiveInventory>();
    }

    private void Update()
    {
        if (inventory == null)
        {
            HideIcons();
            return;
        }

        List<ActiveItem> items = inventory.GetItems();
        int itemCount = items != null ? items.Count : 0;

        if (inventory.isChanged)
        {
            inventory.isChanged = false;
            if (text != null)
                text.text = itemCount > 0 && items[0] != null ? items[0].itemName : string.Empty;

            if (fade != null)
                StopCoroutine(fade);

            if (itemCount > 0 && text != null)
                fade = StartCoroutine(Fade());
        }

        if (itemCount > 2)
        {
            SetIcon(currentIcon, items[0]);
            SetIcon(nextIcon, items[1]);
            SetIcon(thirdIcon, items[2]);
            return;
        }

        if (itemCount == 2)
        {
            SetIcon(currentIcon, items[0]);
            SetIcon(nextIcon, items[1]);
            SetIcon(thirdIcon, null);
            return;
        }

        if (itemCount == 1)
        {
            SetIcon(currentIcon, items[0]);
            SetIcon(nextIcon, null);
            SetIcon(thirdIcon, null);
            return;
        }

        HideIcons();
    }

    private void SetIcon(Image image, ActiveItem item)
    {
        if (image == null)
            return;

        image.enabled = item != null && item.icon != null;
        image.sprite = item != null ? item.icon : null;
    }

    private void HideIcons()
    {
        SetIcon(currentIcon, null);
        SetIcon(nextIcon, null);
        SetIcon(thirdIcon, null);
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

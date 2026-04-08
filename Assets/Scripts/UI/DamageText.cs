using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    private TextMeshProUGUI text;

    public float speed = 1f;
    public float duration = 1f;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void Setup(float damage)
    {
        text.text = Mathf.RoundToInt(damage).ToString();
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float timer = 0f;

        Color startColor = text.color;
        Vector3 startPos = transform.position;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // 위로 올라가게
            transform.position = startPos + Vector3.up * speed * t;

            // 점점 투명하게
            text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }

}

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class HealEffect : MonoBehaviour
{
    public float speed = 1f;
    public float duration = 1f;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    void Start()
    {
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float timer = 0f;

        Color startColor = image.color;
        Vector3 startPos = transform.position;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // 위로 올라가게
            transform.position = startPos + Vector3.up * speed * t;

            // 점점 투명하게
            image.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }
}

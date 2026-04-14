using System.Collections;
using TMPro;
using UnityEngine;

public class MerchantDialogueUI : MonoBehaviour
{
    public Canvas canvas;
    public TextMeshProUGUI dialogueText;
    public float defaultDuration = 2.5f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);

        if (canvas != null)
            canvas.enabled = false;
    }

    public void Show(string message, float duration = -1f)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        EnsurePresentationObjectsAreActive();

        if (dialogueText != null)
            dialogueText.text = message;

        if (canvas != null)
            canvas.enabled = true;

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        float hideDelay = duration > 0f ? duration : defaultDuration;
        if (isActiveAndEnabled)
            hideRoutine = StartCoroutine(HideAfterDelay(hideDelay));
    }

    public void HideImmediately()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (canvas != null)
            canvas.enabled = false;
    }

    private void EnsurePresentationObjectsAreActive()
    {
        ActivateHierarchy(transform);

        if (canvas != null)
            ActivateHierarchy(canvas.transform);

        if (dialogueText != null)
            ActivateHierarchy(dialogueText.transform);
    }

    private static void ActivateHierarchy(Transform target)
    {
        if (target == null)
            return;

        Transform current = target;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            current = current.parent;
        }
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        hideRoutine = null;

        if (canvas != null)
            canvas.enabled = false;
    }
}

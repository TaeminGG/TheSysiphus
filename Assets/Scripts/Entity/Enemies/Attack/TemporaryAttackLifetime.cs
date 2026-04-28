using System.Collections;
using UnityEngine;

public class TemporaryAttackLifetime : MonoBehaviour
{
    [SerializeField] private float fallbackLifetime = 0.5f;

    private Coroutine lifetimeRoutine;

    public void Begin(float lifetime)
    {
        RestartLifetime(Mathf.Max(0.01f, lifetime));
    }

    public void BeginDefault()
    {
        Begin(fallbackLifetime);
    }

    private void RestartLifetime(float lifetime)
    {
        if (lifetimeRoutine != null)
            StopCoroutine(lifetimeRoutine);

        lifetimeRoutine = StartCoroutine(DestroyAfter(lifetime));
    }

    private IEnumerator DestroyAfter(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }
}

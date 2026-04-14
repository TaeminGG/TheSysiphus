using System.Collections;
using UnityEngine;

public class HitStopController : MonoBehaviour
{
    public static HitStopController Instance { get; private set; }

    [SerializeField] private TimeControlService timeControlService;
    [SerializeField] private float defaultDuration = 0.04f;
    [SerializeField] private float defaultTimeScale = 0f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveTimeControlService();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Play(float duration, float timeScale = -1f, int priority = 0)
    {
        ResolveTimeControlService();
        if (timeControlService == null || timeControlService.IsPaused)
            return;

        float actualDuration = duration > 0f ? duration : defaultDuration;
        float actualTimeScale = timeScale >= 0f ? timeScale : defaultTimeScale;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(PlayRoutine(actualDuration, actualTimeScale, priority));
    }

    private IEnumerator PlayRoutine(float duration, float timeScale, int priority)
    {
        int token = timeControlService.BeginHitStop(timeScale, priority);
        yield return new WaitForSecondsRealtime(duration);
        timeControlService.EndHitStop(token);
        activeRoutine = null;
    }

    private void ResolveTimeControlService()
    {
        if (timeControlService != null)
            return;

        timeControlService = TimeControlService.Instance;

        if (timeControlService == null)
        {
            timeControlService = FindFirstObjectByType<TimeControlService>();
        }
    }
}

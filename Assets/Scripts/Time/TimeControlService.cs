using UnityEngine;

public class TimeControlService : MonoBehaviour
{
    public static TimeControlService Instance { get; private set; }

    [SerializeField] private float defaultTimeScale = 1f;

    private bool isPaused;
    private bool isHitStopActive;
    private int hitStopPriority;
    private float hitStopScale = 1f;
    private int hitStopTokenCounter;

    public bool IsPaused => isPaused;
    public bool IsHitStopActive => isHitStopActive;
    public float DefaultTimeScale => defaultTimeScale;
    public float CurrentTimeScale => Time.timeScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyTimeScale();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = defaultTimeScale;
        }
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        ApplyTimeScale();
    }

    public int BeginHitStop(float timeScale, int priority = 0)
    {
        int token = ++hitStopTokenCounter;
        timeScale = Mathf.Clamp(timeScale, 0f, 1f);

        if (!isHitStopActive || priority >= hitStopPriority)
        {
            isHitStopActive = true;
            hitStopPriority = priority;
            hitStopScale = timeScale;
            ApplyTimeScale();
        }

        return token;
    }

    public void EndHitStop(int token)
    {
        if (!isHitStopActive)
            return;

        if (token == hitStopTokenCounter)
        {
            ClearHitStop();
        }
    }

    public void ClearHitStop()
    {
        isHitStopActive = false;
        hitStopPriority = 0;
        hitStopScale = defaultTimeScale;
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        if (isPaused)
        {
            Time.timeScale = 0f;
            return;
        }

        if (isHitStopActive)
        {
            Time.timeScale = hitStopScale;
            return;
        }

        Time.timeScale = defaultTimeScale;
    }
}

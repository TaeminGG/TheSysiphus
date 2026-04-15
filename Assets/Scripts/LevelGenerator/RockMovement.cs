using System;
using System.Collections.Generic;
using UnityEngine;

public class RockMovement : MonoBehaviour
{
    [Min(0f)] public float moveSpeed = 1.0f;
    public LayerMask hitLayer;

    [Header("Progress Tracking")]
    [SerializeField] private float targetY = -10f;
    [SerializeField] private bool stopAtTargetY = false;

    private readonly List<SpeedModifierInstance> activeSpeedModifiers = new List<SpeedModifierInstance>();

    private float progressStartY;
    private float lastProgress = -1f;
    private float lastReportedSpeed = -1f;
    private int speedModifierTokenCounter;
    private bool isMovementStopped;
    private bool hasNotifiedTargetReached;

    public event Action<float> OnProgressChanged;
    public event Action<float> OnSpeedChanged;
    public event Action OnTargetReached;

    public float BaseMoveSpeed => moveSpeed;
    public float CurrentSpeedMultiplier => CalculateSpeedMultiplier();
    public float CurrentMoveSpeed => isMovementStopped ? 0f : Mathf.Max(0f, moveSpeed) * CurrentSpeedMultiplier;
    public float TargetY => targetY;
    public float ProgressStartY => progressStartY;
    public float Progress01 => CalculateProgress(progressStartY, targetY, transform.position.y);
    public float TravelledDistance => Mathf.Abs(transform.position.y - progressStartY);
    public float TotalDistanceToTarget => Mathf.Abs(targetY - progressStartY);
    public float RemainingDistanceToTarget => Mathf.Abs(targetY - transform.position.y);
    public bool HasReachedTarget => CheckReachedTarget(transform.position.y);
    public bool IsMovementStopped => isMovementStopped;

    private void Awake()
    {
        ResetProgress();
        NotifySpeedChangedIfNeeded(true);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        UpdateSpeedModifiers(deltaTime);
        MoveRock(deltaTime);
        UpdateProgressState();
        NotifySpeedChangedIfNeeded();
    }

    public void SetBaseSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
        NotifySpeedChangedIfNeeded(true);
    }

    public void SetTargetY(float newTargetY, bool resetProgressFromCurrentPosition = false)
    {
        targetY = newTargetY;
        hasNotifiedTargetReached = false;

        if (resetProgressFromCurrentPosition)
            ResetProgress();
        else
            UpdateProgressState(true);
    }

    public void ResetProgress()
    {
        progressStartY = transform.position.y;
        hasNotifiedTargetReached = false;
        UpdateProgressState(true);
    }

    public int AddSpeedMultiplier(float multiplier)
    {
        return AddSpeedModifierInternal(multiplier, 0f, false);
    }

    public int AddTimedSpeedMultiplier(float multiplier, float duration)
    {
        return AddSpeedModifierInternal(multiplier, duration, true);
    }

    public int StopForDuration(float duration)
    {
        return AddTimedSpeedMultiplier(0f, duration);
    }

    public int StopUntilReleased()
    {
        return AddSpeedMultiplier(0f);
    }

    public bool RemoveSpeedModifier(int token)
    {
        for (int i = 0; i < activeSpeedModifiers.Count; i++)
        {
            if (activeSpeedModifiers[i].Token != token)
                continue;

            activeSpeedModifiers.RemoveAt(i);
            NotifySpeedChangedIfNeeded(true);
            return true;
        }

        return false;
    }

    public void ClearSpeedModifiers()
    {
        if (activeSpeedModifiers.Count == 0)
            return;

        activeSpeedModifiers.Clear();
        NotifySpeedChangedIfNeeded(true);
    }

    public void StopMovement()
    {
        if (isMovementStopped)
            return;

        isMovementStopped = true;
        NotifySpeedChangedIfNeeded(true);
    }

    public void ResumeMovement()
    {
        if (!isMovementStopped)
            return;

        isMovementStopped = false;
        NotifySpeedChangedIfNeeded(true);
    }

    private void MoveRock(float deltaTime)
    {
        float currentMoveSpeed = CurrentMoveSpeed;
        if (deltaTime <= 0f || currentMoveSpeed <= 0f)
            return;

        Vector3 currentPosition = transform.position;
        float nextY = currentPosition.y - currentMoveSpeed * deltaTime;

        if (stopAtTargetY && WouldCrossTarget(currentPosition.y, nextY))
            nextY = targetY;

        if (Mathf.Approximately(currentPosition.y, nextY))
            return;

        transform.position = new Vector3(currentPosition.x, nextY, currentPosition.z);
    }

    private void UpdateSpeedModifiers(float deltaTime)
    {
        for (int i = activeSpeedModifiers.Count - 1; i >= 0; i--)
        {
            SpeedModifierInstance modifier = activeSpeedModifiers[i];
            if (!modifier.IsTimed)
                continue;

            modifier.RemainingDuration -= deltaTime;
            if (modifier.RemainingDuration <= 0f)
            {
                activeSpeedModifiers.RemoveAt(i);
            }
            else
            {
                activeSpeedModifiers[i] = modifier;
            }
        }
    }

    private int AddSpeedModifierInternal(float multiplier, float duration, bool isTimed)
    {
        int token = ++speedModifierTokenCounter;

        activeSpeedModifiers.Add(new SpeedModifierInstance
        {
            Token = token,
            Multiplier = Mathf.Max(0f, multiplier),
            RemainingDuration = Mathf.Max(0f, duration),
            IsTimed = isTimed && duration > 0f
        });

        NotifySpeedChangedIfNeeded(true);
        return token;
    }

    private float CalculateSpeedMultiplier()
    {
        float multiplier = 1f;

        for (int i = 0; i < activeSpeedModifiers.Count; i++)
        {
            multiplier *= activeSpeedModifiers[i].Multiplier;
        }

        return Mathf.Max(0f, multiplier);
    }

    private void UpdateProgressState(bool forceNotify = false)
    {
        float progress = Progress01;
        if (forceNotify || !Mathf.Approximately(progress, lastProgress))
        {
            lastProgress = progress;
            OnProgressChanged?.Invoke(progress);
        }

        bool hasReachedTarget = HasReachedTarget;
        if (hasReachedTarget && !hasNotifiedTargetReached)
        {
            hasNotifiedTargetReached = true;
            OnTargetReached?.Invoke();
        }
        else if (!hasReachedTarget)
        {
            hasNotifiedTargetReached = false;
        }
    }

    private void NotifySpeedChangedIfNeeded(bool forceNotify = false)
    {
        float currentSpeed = CurrentMoveSpeed;
        if (forceNotify || !Mathf.Approximately(currentSpeed, lastReportedSpeed))
        {
            lastReportedSpeed = currentSpeed;
            OnSpeedChanged?.Invoke(currentSpeed);
        }
    }

    private bool WouldCrossTarget(float currentY, float nextY)
    {
        if (currentY >= targetY)
            return nextY <= targetY;

        return nextY >= targetY;
    }

    private bool CheckReachedTarget(float currentY)
    {
        if (progressStartY >= targetY)
            return currentY <= targetY;

        return currentY >= targetY;
    }

    private static float CalculateProgress(float startY, float endY, float currentY)
    {
        if (Mathf.Approximately(startY, endY))
            return 1f;

        return Mathf.Clamp01(Mathf.InverseLerp(startY, endY, currentY));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer.value) == 0)
            return;

        PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats == null)
            return;

        playerStats.HitByRock();
    }

    private struct SpeedModifierInstance
    {
        public int Token;
        public float Multiplier;
        public float RemainingDuration;
        public bool IsTimed;
    }
}

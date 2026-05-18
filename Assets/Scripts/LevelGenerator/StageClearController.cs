using System;
using UnityEngine;
using UnityEngine.Events;

public class StageClearController : MonoBehaviour
{
    [SerializeField] private bool isCleared;

    [Header("Events")]
    public UnityEvent onStageCleared = new UnityEvent();

    public bool IsCleared => isCleared;

    public event Action StageCleared;

    public void CompleteStage()
    {
        if (isCleared)
            return;

        isCleared = true;
        StageCleared?.Invoke();
        onStageCleared?.Invoke();
    }

    public void ResetStageClearState()
    {
        isCleared = false;
    }
}

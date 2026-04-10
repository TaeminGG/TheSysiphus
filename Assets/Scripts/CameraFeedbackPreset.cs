using UnityEngine;

[CreateAssetMenu(menuName = "Camera/Feedback Preset", fileName = "CameraFeedbackPreset")]
public class CameraFeedbackPreset : ScriptableObject
{
    [Header("Shake")]
    public float shakeDuration = 0.08f;
    public float shakeMagnitude = 0.12f;
    public float shakeFrequency = 28f;
    public AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Zoom")]
    public bool enableZoomPunch = true;
    public float zoomSize = 4.7f;
    public float zoomHoldTime = 0.03f;
    public float zoomRecoverSpeed = 12f;
}

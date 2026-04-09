using System.Collections;
using UnityEngine;

public class CameraChase : MonoBehaviour
{
    // 카메라 최대 위치 제한
    [Header("Bounds")]
    public float minX = -50f;
    public float maxX = -50f;
    public float minY = -50f;
    public float maxY = -50f;
    public bool useBounds = false;

    [Header("Follow")]
    public Vector3 cameraDepth = new Vector3(0f, 0f, -10f);
    public float cameraSpeed = 5.0f;
    [SerializeField] private Transform followTarget;
    [SerializeField] private string fallbackTargetName = "PlayerSprite";

    [Header("Zoom")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float defaultZoom = 5f;
    [SerializeField] private float zoomLerpSpeed = 10f;

    [Header("Preset")]
    [SerializeField] private CameraFeedbackPreset defaultImpactPreset;

    private Coroutine shakeRoutine;
    private Coroutine zoomRoutine;
    private Vector3 shakeOffset = Vector3.zero;
    private float targetZoom;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        targetZoom = defaultZoom;

        if (targetCamera != null && targetCamera.orthographic)
        {
            targetCamera.orthographicSize = defaultZoom;
        }
    }

    private void Start()
    {
        ResolveTarget();
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            ResolveTarget();
            if (followTarget == null)
            {
                return;
            }
        }

        Vector3 desiredPosition = followTarget.position;
        desiredPosition.z = 0f;
        desiredPosition += cameraDepth;
        desiredPosition += shakeOffset;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * cameraSpeed);

        if (targetCamera != null && targetCamera.orthographic)
        {
            targetCamera.orthographicSize = Mathf.Lerp(
                targetCamera.orthographicSize,
                targetZoom,
                Time.deltaTime * zoomLerpSpeed);
        }
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    public void PlayPreset(CameraFeedbackPreset presetOverride = null)
    {
        CameraFeedbackPreset preset = presetOverride != null ? presetOverride : defaultImpactPreset;
        if (preset == null)
        {
            return;
        }

        Shake(preset.shakeDuration, preset.shakeMagnitude, preset.shakeFrequency, preset.shakeCurve);

        if (preset.enableZoomPunch)
        {
            ZoomPunch(preset.zoomSize, preset.zoomHoldTime, preset.zoomRecoverSpeed);
        }
    }

    public void Shake(float duration, float magnitude, float frequency, AnimationCurve curve = null)
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude, frequency, curve));
    }

    public void ZoomPunch(float zoomSize, float holdTime, float recoverSpeed)
    {
        if (targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        if (zoomRoutine != null)
        {
            StopCoroutine(zoomRoutine);
        }

        zoomRoutine = StartCoroutine(ZoomPunchRoutine(zoomSize, holdTime, recoverSpeed));
    }

    private void ResolveTarget()
    {
        if (followTarget != null)
        {
            return;
        }

        GameObject targetObject = GameObject.Find(fallbackTargetName);
        if (targetObject != null)
        {
            followTarget = targetObject.transform;
        }
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude, float frequency, AnimationCurve curve)
    {
        if (duration <= 0f || magnitude <= 0f)
        {
            shakeOffset = Vector3.zero;
            yield break;
        }

        float elapsed = 0f;
        float noiseSeed = Random.Range(0f, 1000f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float strength = curve != null ? curve.Evaluate(normalizedTime) : 1f - normalizedTime;

            float sampleTime = elapsed * Mathf.Max(0.01f, frequency);
            float x = Mathf.PerlinNoise(noiseSeed, sampleTime) * 2f - 1f;
            float y = Mathf.PerlinNoise(noiseSeed + 100f, sampleTime) * 2f - 1f;

            shakeOffset = new Vector3(x, y, 0f) * magnitude * strength;
            yield return null;
        }

        shakeOffset = Vector3.zero;
        shakeRoutine = null;
    }

    private IEnumerator ZoomPunchRoutine(float zoomSize, float holdTime, float recoverSpeed)
    {
        targetZoom = zoomSize;

        if (holdTime > 0f)
        {
            yield return new WaitForSeconds(holdTime);
        }

        while (Mathf.Abs(targetZoom - defaultZoom) > 0.02f)
        {
            targetZoom = Mathf.Lerp(targetZoom, defaultZoom, Time.deltaTime * recoverSpeed);
            yield return null;
        }

        targetZoom = defaultZoom;
        zoomRoutine = null;
    }
}

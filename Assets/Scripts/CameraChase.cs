using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraChase : MonoBehaviour
{
    // 카메라 최대 위치 제한
    [Header("Bounds")]
    public float minX = -50f;
    public float maxX = -50f;
    public float minY = -50f;
    public float maxY = -50f;
    public bool useBounds = false;

    [Header("Map Bounds")]
    [SerializeField] private bool useHorizontalMapBounds = true;
    [SerializeField] private float horizontalBoundsCenterX = 0f;
    [SerializeField] private float horizontalBoundsWidth = 50f;
    [SerializeField] private bool keepViewportInsideHorizontalBounds = true;

    [Header("Follow")]
    public Vector3 cameraDepth = new Vector3(0f, 0f, -10f);
    public float cameraSpeed = 5.0f;
    [SerializeField] private Transform followTarget;
    [SerializeField] private string fallbackTargetName = "PlayerSprite";

    [Header("Mouse Look Offset")]
    [SerializeField] private bool useMouseLookOffset = true;
    [SerializeField] private float maxMouseLookDistance = 1.5f;
    [SerializeField, Range(0f, 0.95f)] private float mouseLookDeadZone = 0.15f;
    [SerializeField] private float mouseLookSmoothSpeed = 8f;
    [SerializeField] private AnimationCurve mouseLookCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Zoom")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float defaultZoom = 5f;
    [SerializeField] private float zoomLerpSpeed = 10f;
    [SerializeField] private bool useUnscaledDeltaTime = false;

    [Header("Preset")]
    [SerializeField] private CameraFeedbackPreset defaultImpactPreset;

    private Coroutine shakeRoutine;
    private Coroutine zoomRoutine;
    private Vector3 shakeOffset = Vector3.zero;
    private Vector3 mouseLookOffset = Vector3.zero;
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

        Vector3 desiredPosition = GetDesiredPosition(includeShakeOffset: true);
        float deltaTime = GetDeltaTime();

        transform.position = Vector3.Lerp(transform.position, desiredPosition, deltaTime * cameraSpeed);

        if (targetCamera != null && targetCamera.orthographic)
        {
            targetCamera.orthographicSize = Mathf.Lerp(
                targetCamera.orthographicSize,
                targetZoom,
                deltaTime * zoomLerpSpeed);
        }
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    public void SetMouseLookOffsetEnabled(bool enabled, bool clearOffsetImmediately = false)
    {
        useMouseLookOffset = enabled;

        if (clearOffsetImmediately)
            mouseLookOffset = Vector3.zero;
    }

    public void SetCameraSpeed(float newSpeed)
    {
        cameraSpeed = Mathf.Max(0f, newSpeed);
    }

    public void SetUseUnscaledTime(bool enabled)
    {
        useUnscaledDeltaTime = enabled;
    }

    public void SetZoomImmediate(float zoomSize)
    {
        if (zoomRoutine != null)
        {
            StopCoroutine(zoomRoutine);
            zoomRoutine = null;
        }

        targetZoom = zoomSize;

        if (targetCamera != null && targetCamera.orthographic)
            targetCamera.orthographicSize = zoomSize;
    }

    public void SnapToFollowTarget()
    {
        if (followTarget == null)
        {
            ResolveTarget();
            if (followTarget == null)
                return;
        }

        transform.position = GetDesiredPosition(includeShakeOffset: false);
    }

    public void ZoomTo(float zoomSize, float duration, bool useUnscaledTime = true)
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        if (zoomRoutine != null)
            StopCoroutine(zoomRoutine);

        zoomRoutine = StartCoroutine(ZoomToRoutine(zoomSize, duration, useUnscaledTime));
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

    private Vector3 UpdateMouseLookOffset()
    {
        if (!useMouseLookOffset || Mouse.current == null || Screen.width <= 0 || Screen.height <= 0)
        {
            mouseLookOffset = Vector3.Lerp(mouseLookOffset, Vector3.zero, GetDeltaTime() * mouseLookSmoothSpeed);
            return mouseLookOffset;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 screenHalfSize = screenCenter;

        Vector2 normalizedFromCenter = new Vector2(
            (mousePosition.x - screenCenter.x) / screenHalfSize.x,
            (mousePosition.y - screenCenter.y) / screenHalfSize.y);

        float rawDistance = Mathf.Clamp01(normalizedFromCenter.magnitude);
        float intensity = Mathf.InverseLerp(mouseLookDeadZone, 1f, rawDistance);
        intensity = mouseLookCurve != null ? mouseLookCurve.Evaluate(intensity) : intensity;

        Vector2 direction = rawDistance > 0.001f ? normalizedFromCenter.normalized : Vector2.zero;
        Vector3 targetOffset = new Vector3(direction.x, direction.y, 0f) * maxMouseLookDistance * intensity;

        mouseLookOffset = Vector3.Lerp(mouseLookOffset, targetOffset, GetDeltaTime() * mouseLookSmoothSpeed);
        return mouseLookOffset;
    }

    private Vector3 GetDesiredPosition(bool includeShakeOffset)
    {
        Vector3 desiredPosition = followTarget.position;
        desiredPosition.z = 0f;
        desiredPosition += cameraDepth;
        desiredPosition += UpdateMouseLookOffset();

        if (includeShakeOffset)
            desiredPosition += shakeOffset;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        desiredPosition.x = ClampCameraXToHorizontalMapBounds(desiredPosition.x);

        return desiredPosition;
    }

    private float ClampCameraXToHorizontalMapBounds(float cameraX)
    {
        if (!useHorizontalMapBounds || horizontalBoundsWidth <= 0f)
            return cameraX;

        float halfWidth = horizontalBoundsWidth * 0.5f;
        float minCameraX = horizontalBoundsCenterX - halfWidth;
        float maxCameraX = horizontalBoundsCenterX + halfWidth;

        if (keepViewportInsideHorizontalBounds && targetCamera != null && targetCamera.orthographic)
        {
            float cameraHalfWidth = targetCamera.orthographicSize * targetCamera.aspect;
            minCameraX += cameraHalfWidth;
            maxCameraX -= cameraHalfWidth;
        }

        if (minCameraX > maxCameraX)
            return horizontalBoundsCenterX;

        return Mathf.Clamp(cameraX, minCameraX, maxCameraX);
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
            elapsed += GetDeltaTime();
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
            targetZoom = Mathf.Lerp(targetZoom, defaultZoom, GetDeltaTime() * recoverSpeed);
            yield return null;
        }

        targetZoom = defaultZoom;
        zoomRoutine = null;
    }

    private IEnumerator ZoomToRoutine(float zoomSize, float duration, bool useUnscaledTime)
    {
        float startZoom = targetCamera.orthographicSize;

        if (duration <= 0f)
        {
            SetZoomImmediate(zoomSize);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentZoom = Mathf.Lerp(startZoom, zoomSize, t);

            targetZoom = currentZoom;
            targetCamera.orthographicSize = currentZoom;
            yield return null;
        }

        targetZoom = zoomSize;
        targetCamera.orthographicSize = zoomSize;
        zoomRoutine = null;
    }

    private float GetDeltaTime()
    {
        return useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}

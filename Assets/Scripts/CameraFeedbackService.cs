using UnityEngine;

public class CameraFeedbackService : MonoBehaviour
{
    public static CameraFeedbackService Instance { get; private set; }

    [SerializeField] private CameraChase cameraChase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveCameraChase();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Play(CameraFeedbackPreset preset = null)
    {
        ResolveCameraChase();
        cameraChase?.PlayPreset(preset);
    }

    public void Shake(float duration, float magnitude, float frequency, AnimationCurve curve = null)
    {
        ResolveCameraChase();
        cameraChase?.Shake(duration, magnitude, frequency, curve);
    }

    public void ZoomPunch(float zoomSize, float holdTime, float recoverSpeed)
    {
        ResolveCameraChase();
        cameraChase?.ZoomPunch(zoomSize, holdTime, recoverSpeed);
    }

    public CameraChase GetCameraChase()
    {
        ResolveCameraChase();
        return cameraChase;
    }

    private void ResolveCameraChase()
    {
        if (cameraChase != null)
        {
            return;
        }

        if (Camera.main != null)
        {
            cameraChase = Camera.main.GetComponent<CameraChase>();
        }

        if (cameraChase == null)
        {
            cameraChase = FindFirstObjectByType<CameraChase>();
        }
    }
}

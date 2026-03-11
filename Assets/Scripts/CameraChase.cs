using UnityEngine;

public class CameraChase : MonoBehaviour
{
    public float minX = -50;
    public float maxX = -50;
    public float minY = -50;
    public float maxY = -50;

    public Vector3 cameraDepth = new Vector3(0, 0, -10);
    public float cameraSpeed = 5.0f;
    private Transform playerTransform;

    void Start()
    {
        playerTransform = GameObject.Find("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, playerTransform.position + cameraDepth, Time.deltaTime * cameraSpeed);
    }
}

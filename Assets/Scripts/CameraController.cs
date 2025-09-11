using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform cameraTransform;  // Referanse til det faktiske kameraet
    public float zoomSpeed = 1.0f;
    public float minZoom = 0.1f;
    public float maxZoom = 10000f;
    public float rotationSpeed = 100f;

    private float distance = 10f;
    private Vector2 rotation = Vector2.zero;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        distance = Vector3.Distance(transform.position, cameraTransform.position);
    }

    void Update()
    {
        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            float logDistance = Mathf.Log10(distance);
            logDistance -= scroll * zoomSpeed;
            logDistance = Mathf.Clamp(logDistance, Mathf.Log10(minZoom), Mathf.Log10(maxZoom));
            distance = Mathf.Pow(10, logDistance);
        }
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(1)) // Høyre musetast
        {
            rotation.x += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            rotation.y -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            rotation.y = Mathf.Clamp(rotation.y, -89f, 89f);
        }
    }

    void UpdateCameraPosition()
    {
        Quaternion rot = Quaternion.Euler(rotation.y, rotation.x, 0);
        Vector3 dir = rot * Vector3.forward;
        cameraTransform.position = transform.position - dir * distance;
        cameraTransform.rotation = rot;
    }

    public void ResetView()
    {
        rotation = Vector2.zero;
        distance = 10f; // eller et annet standard zoom-nivå
    }
}

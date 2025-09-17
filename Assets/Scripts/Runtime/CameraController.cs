using UnityEngine;

/// <summary>
/// Управляет движением камеры во время игры (вращение и приближение).
/// Вешается на объект CameraRig.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Настройки вращения")]
    public float rotationSpeed = 300f;
    public float zoomSpeed = 10f;

    [Header("Ограничения")]
    public float minZoom = 5f;
    public float maxZoom = 20f;
    public float minPitch = 10f;
    public float maxPitch = 60f;

    private Transform _camera;
    private float _distance;
    private float _pitch;
    private float _yaw;

    private void Start()
    {
        _camera = Camera.main.transform;

        // Текущий оффсет от Rig
        Vector3 offset = _camera.position - transform.position;
        _distance = offset.magnitude;
        if (_distance < 0.01f)
            _distance = Mathf.Lerp(minZoom, maxZoom, 0.5f);

        // ⚡ Берём углы ПРЯМО из инспектора камеры
        Vector3 angles = _camera.eulerAngles;
        _yaw = angles.y;
        _pitch = Mathf.Clamp(angles.x, minPitch, maxPitch);

        // Ставим камеру в стартовое положение
        UpdateCameraPosition();
    }

    private void Update()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            _yaw += mouseX * rotationSpeed * Time.deltaTime;
            _pitch -= mouseY * rotationSpeed * Time.deltaTime * 0.3f;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            UpdateCameraPosition();
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _distance -= scroll * zoomSpeed;
            _distance = Mathf.Clamp(_distance, minZoom, maxZoom);
            UpdateCameraPosition();
        }
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 dir = rotation * Vector3.back;

        _camera.position = transform.position + dir * _distance;
        _camera.LookAt(transform.position);
    }
}


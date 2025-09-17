using UnityEngine;

/// <summary>
/// Делает объект всегда повёрнутым лицом к главной камере.
/// Используем для HealthBar.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera _mainCamera;

    private void Start()
    {
        // Находим камеру только один раз
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_mainCamera == null) return;

        // Разворачиваем объект в сторону камеры
        transform.forward = _mainCamera.transform.forward;
    }
}

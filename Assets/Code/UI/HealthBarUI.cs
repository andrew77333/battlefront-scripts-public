using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет отображением полоски здоровья.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    // Это ссылка на зелёную часть полоски (Fill)
    [SerializeField] private Image _fill;

    /// <summary>
    /// Установить здоровье в диапазоне 0..1
    /// </summary>
    public void SetValue(float value01)   // <-- оставляем SetValue (важно!)
    {
        // Ограничим входное значение между 0 и 1
        value01 = Mathf.Clamp01(value01);

        // Изменяем "заливку" по ширине
        _fill.fillAmount = value01;
    }

    private void Start()
    {
        // Тест: уменьшаем здоровье до 50%
        SetValue(0.5f);
    }
}


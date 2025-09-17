using UnityEngine;
using Game.Units;   // Unit
using Game.Runtime; // Runtime-статы

/// <summary>
/// Спавнит и держит полоску ХП над головой юнита.
/// </summary>
[RequireComponent(typeof(Unit))]
public class HealthBarLink : MonoBehaviour
{
    [SerializeField] private GameObject healthBarPrefab;

    [Header("Position")]
    [Tooltip("Доп. смещение вверх над макушкой")]
    //[SerializeField] private float yOffset = 0.15f;
    [SerializeField] private float yOffset = 1.8f; // выставишь в инспекторе

    private HealthBarUI _ui;
    private Unit _unit;
    private CharacterController _cc;
    private CapsuleCollider _capsule; // на случай, если CharacterController отсутствует

    private void Start()
    {
        _unit = GetComponent<Unit>();
        _cc = GetComponent<CharacterController>();
        _capsule = GetComponent<CapsuleCollider>();

        if (_unit == null)
        {
            Debug.LogError($"{name}: Unit not found!");
            return;
        }
        if (healthBarPrefab == null)
        {
            Debug.LogError($"{name}: HealthBar prefab not assigned!");
            return;
        }

        // Создаём бар как дочерний объект
        var bar = Instantiate(healthBarPrefab, transform);

        // Высота макушки: центр капсулы + половина высоты
        float headY;
        if (_cc != null)
            headY = _cc.center.y + _cc.height * 0.5f;
        else if (_capsule != null)
            headY = _capsule.center.y + _capsule.height * 0.5f;
        else
            headY = 2.0f; // запасной вариант, если капсулы нет

        //bar.transform.localPosition = new Vector3(0f, headY + yOffset, 0f);
        bar.transform.localPosition = new Vector3(0f, yOffset, 0f);
        bar.transform.localRotation = Quaternion.identity;

        _ui = bar.GetComponent<HealthBarUI>();
        if (_ui == null)
            Debug.LogWarning($"{name}: HealthBarUI component not found on prefab {healthBarPrefab.name}");
    }

    private void Update()
    {
        if (_unit == null || _ui == null) return;

        var stats = _unit.Runtime;
        if (stats == null) return;

        float max = Mathf.Max(0.0001f, stats.CurrentStats.Health);
        float ratio = Mathf.Clamp01(stats.CurrentHealth / max);
        _ui.SetValue(ratio);
    }

    public void HideBar()
    {
        if (_ui != null)
        {
            Destroy(_ui.gameObject);
            _ui = null;
        }
    }
}

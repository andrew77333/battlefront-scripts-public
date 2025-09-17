using UnityEngine;

public partial class CombatAgent : MonoBehaviour
{
    [Header("Steering LOD & Culling")]
    [SerializeField] private bool EnableSteeringLOD = true;   // включить LOD для сенсоров руления
    [SerializeField] private bool UseOffscreenThrottle = true;// замедлять вне камеры
    [SerializeField, Tooltip("Частота выборок (Гц), когда объект в кадре и рядом.")]
    private float OnscreenPerceptionHz = 20f;
    [SerializeField, Tooltip("Частота выборок (Гц), когда объект вне кадра.")]
    private float OffscreenPerceptionHz = 5f;
    [SerializeField, Tooltip("Частота выборок (Гц), когда далеко от цели.")]
    private float FarPerceptionHz = 8f;
    [SerializeField, Tooltip("Дистанция, после которой считаем 'далеко'.")]
    private float FarDistance = 12f;
    [SerializeField, Tooltip("Случайный разброс интервалов (±доля).")]
    private float LODJitter = 0.2f;

    // Флаг видимости для offscreen-throttling
    private bool _isVisible;
    private float _nextManualVisCheckAt;

    // Времена следующей выборки (стаггер на сенсор по типу)
    private float _nextSepSampleAt;
    private float _nextBypassSampleAt;
    private float _nextCohSampleAt;

    // Кэши значений сенсоров
    private Vector3 _cachedSeparation;
    private Vector3 _cachedBypass;
    private Vector3 _cachedCohDir;
    private int _cachedCohCount;
    private float _cachedCohDist;

    // --- Инициализация LOD ---
    private void LODAwakeSetup()
    {
        // Небольшой начальный джиттер, чтобы агенты не синхронизировались
        float t = Time.time;
        float jitter = Random.Range(0f, 0.25f);
        _nextSepSampleAt = t + jitter;
        _nextBypassSampleAt = t + jitter * 0.7f;
        _nextCohSampleAt = t + jitter * 0.4f;

        // Попробуем грубо определить видимость хотя бы раз в начале (для случаев без Renderer на корне)
        _isVisible = true; // по умолчанию считаем видимым, чтобы не занижать частоту в первый кадр
    }

    // Эти события сработают, если на том же GameObject есть Renderer.
    private void OnBecameVisible() { _isVisible = true; }
    private void OnBecameInvisible() { _isVisible = false; }

    // Фоллбэк: если Renderer на корне отсутствует — раз в N сек. оцениваем видимость по камере
    private void LateUpdate()
    {
        if (!UseOffscreenThrottle) return;
        if (GetComponent<Renderer>() != null) return; // нормальные события уже работают
        if (Time.time < _nextManualVisCheckAt) return;

        _nextManualVisCheckAt = Time.time + 0.75f;

        var cam = Camera.main;
        if (cam == null) return;
        Vector3 vp = cam.WorldToViewportPoint(transform.position);
        _isVisible = (vp.z > 0f && vp.x > -0.05f && vp.x < 1.05f && vp.y > -0.05f && vp.y < 1.05f);
    }

    // Текущий интервал выборок по LOD (0 => каждый кадр)
    private float GetPerceptionInterval(float distToTarget)
    {
        if (!EnableSteeringLOD) return 0f;

        float hz = OnscreenPerceptionHz; // в кадре по умолчанию быстрее
        if (UseOffscreenThrottle && !_isVisible) hz = OffscreenPerceptionHz;
        if (distToTarget > FarDistance) hz = Mathf.Min(hz, FarPerceptionHz); // когда далеко — медленнее

        hz = Mathf.Max(1f, hz);
        return 1f / hz;
    }

    // Проверка/планирование следующей выборки конкретного сенсора
    private bool LOD_ShouldRecompute(float distToTarget, ref float nextAt)
    {
        if (!EnableSteeringLOD) return true;
        if (Time.time >= nextAt)
        {
            float interval = GetPerceptionInterval(distToTarget);
            float jitter = 1f + Random.Range(-LODJitter, LODJitter);
            nextAt = Time.time + interval * jitter;
            return true;
        }
        return false;
    }

    // === LOD-обёртки над "тяжёлыми" сенсорами (вызываются из основного Update) ===

    // Separation c кэшем/LOD
    private Vector3 LOD_Separation(float radius, float distToTarget)
    {
        if (LOD_ShouldRecompute(distToTarget, ref _nextSepSampleAt))
            _cachedSeparation = ComputeSeparationXZ(radius); // реальный расчёт в partial: Steering
        return _cachedSeparation;
    }

    // Bypass c кэшем/LOD
    private Vector3 LOD_Bypass(Vector3 toTargetNorm, float aheadDist, float distToTarget)
    {
        if (LOD_ShouldRecompute(distToTarget, ref _nextBypassSampleAt))
            _cachedBypass = ComputeBypassXZ(toTargetNorm, aheadDist); // Steering
        return _cachedBypass;
    }

    // Cohesion c кэшем/LOD
    private Vector3 LOD_Cohesion(float radius, out int count, out float distToCentroid, float distToTarget)
    {
        if (LOD_ShouldRecompute(distToTarget, ref _nextCohSampleAt))
            _cachedCohDir = ComputeCohesionXZ(radius, out _cachedCohCount, out _cachedCohDist); // Steering

        count = _cachedCohCount;
        distToCentroid = _cachedCohDist;
        return _cachedCohDir;
    }
}

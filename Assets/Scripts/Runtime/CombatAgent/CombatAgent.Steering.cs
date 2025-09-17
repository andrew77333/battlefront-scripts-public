// CombatAgent.Steering.cs
// ЧАСТЬ класса CombatAgent: локальные векторы / steering (separation / bypass / cohesion).

// CombatAgent.Steering.cs
// ЧАСТЬ класса CombatAgent: Chaos/Orbit, Avoidance, Cohesion и векторные вычисления.

using Game.Units;
using UnityEngine;

public partial class CombatAgent : MonoBehaviour
{
    // =========================
    // ХАОС / ОРБИТ-СТРАФИНГ
    // =========================
    [Header("Chaos / Orbit")]
    [SerializeField, Range(0f, 1f)] private float ApproachSideBias = 0.35f; // боковой уклон при подходе
    [SerializeField] private float OrbitSideSpeed = 0.8f;  // скорость бокового смещения на месте
    [SerializeField] private float OrbitRadialGain = 1.2f;  // насколько активно держим окружность
    [SerializeField] private float OrbitRadiusJitter = 0.15f; // небольшой разброс радиуса у каждого

    private float _orbitSign = 1f;         // направление орбиты (+1/-1)
    private float _orbitRadiusOffset = 0f; // индивидуальный сдвиг радиуса

    // =========================
    // AVOIDANCE / ОБХОД
    // =========================
    [Header("Avoidance")]
    [SerializeField] private float SeparationRadius = 0.6f; // радиус "разлипалки"
    [SerializeField] private float SeparationStrength = 1.2f; // сила "разлипалки"
    [SerializeField] private float BypassAheadDistance = 0.9f; // как далеко вперёд смотрим для обхода союзника
    [SerializeField] private float BypassStrength = 0.8f; // сила бокового обхода

    // =========================
    // COHESION / "НЕ УЕЗЖАТЬ ВДВОЁМ"
    // =========================
    [Header("Cohesion (anti-runaway)")]
    [SerializeField] private float CohesionRadius = 6f;   // радиус поиска своих для центроида
    [SerializeField] private float CohesionStrength = 0.8f; // сила притяжения к "стае", зависит от isolation
    [SerializeField] private float IsolationThreshold = 3f;   // дистанция, после которой считаем, что "изолировался"
    [SerializeField, Range(0f, 1f)] private float OrbitIsolationDampen = 0.6f; // гашение орбиты при изоляции

    // =========================
    // СЛОИ ВОСПРИЯТИЯ (узкий LayerMask вместо ~0)
    // =========================
    [Header("Perception Layers")]
    [Tooltip("Слои, на которых расположены капсулы/коллайдеры персонажей (союзники/враги).")]
    [SerializeField] private LayerMask CharactersMask = ~0; // по умолчанию 'все', чтобы ничего не сломать

    // Буферы (общие, создаются один раз)
    private readonly Collider[] _overlap = new Collider[64];   // сфера соседей
    private readonly UnitTeam[] _tmpTeams = new UnitTeam[64];  // дедуп UnitTeam

    // Одноразовые настройки руления (орбита/джиттер)
    private void SteeringAwakeSetup()
    {
        _orbitSign = (Random.value < 0.5f) ? -1f : 1f;                            // случайное направление орбиты
        _orbitRadiusOffset = Random.Range(-OrbitRadiusJitter, OrbitRadiusJitter); // индивидуальный сдвиг радиуса
    }

    /// <summary>
    /// Локальная "разлипалка": суммарный вектор отталкивания от СОЮЗНИКОВ в радиусе.
    /// Быстро: OverlapSphereNonAlloc + дедуп UnitTeam.
    /// </summary>
    private Vector3 ComputeSeparationXZ(float radius)
    {
        Vector3 sum = Vector3.zero;
        Vector3 self = transform.position;
        float r2 = radius * radius;

        int count = Physics.OverlapSphereNonAlloc(
            self, radius, _overlap, CharactersMask, QueryTriggerInteraction.Ignore);

        int seen = 0; // сколько уникальных UnitTeam уже учли

        for (int i = 0; i < count && i < _overlap.Length; i++)
        {
            var col = _overlap[i];
            if (col == null) continue;

            var ally = col.GetComponentInParent<UnitTeam>();
            // Нужны ТОЛЬКО СОЮЗНИКИ (и живые)
            if (ally == null || ally == _team) continue;
            if (ally.Team != _team.Team) continue;
            if (ally.Unit == null || ally.Unit.Runtime == null || !ally.Unit.Runtime.IsAlive()) continue;

            // дедуп одного и того же юнита (если у него несколько коллайдеров)
            bool dup = false;
            for (int s = 0; s < seen; s++) { if (_tmpTeams[s] == ally) { dup = true; break; } }
            if (dup) continue;
            if (seen < _tmpTeams.Length) _tmpTeams[seen++] = ally;

            Vector3 delta = self - ally.transform.position;
            delta.y = 0f;
            float d2 = delta.sqrMagnitude;
            if (d2 < 0.0001f || d2 > r2) continue;

            float d = Mathf.Sqrt(d2);
            float weight = (radius - d) / radius; // чем ближе — тем сильнее отталкивание
            sum += (delta / Mathf.Max(d, 0.0001f)) * weight;
        }

        return sum;
    }

    /// <summary>
    /// Обход союзника, который "впереди" по направлению к цели:
    /// если кто-то прямо перед нами на малой дистанции — добавляем боковой сдвиг (в сторону _orbitSign).
    /// Быстро: OverlapSphereNonAlloc локально.
    /// </summary>
    private Vector3 ComputeBypassXZ(Vector3 toTargetNorm, float aheadDist)
    {
        Vector3 self = transform.position;

        int count = Physics.OverlapSphereNonAlloc(
            self, aheadDist, _overlap, CharactersMask, QueryTriggerInteraction.Ignore);

        UnitTeam best = null;
        float bestForward = 0.85f; // 1.0 = строго впереди; 0.85 — достаточно "впереди"
        float minDist = aheadDist;

        for (int i = 0; i < count && i < _overlap.Length; i++)
        {
            var col = _overlap[i];
            if (col == null) continue;

            var ally = col.GetComponentInParent<UnitTeam>();
            // Нужны ТОЛЬКО СОЮЗНИКИ (и живые)
            if (ally == null || ally == _team) continue;
            if (ally.Team != _team.Team) continue;
            if (ally.Unit == null || ally.Unit.Runtime == null || !ally.Unit.Runtime.IsAlive()) continue;

            Vector3 delta = ally.transform.position - self;
            delta.y = 0f;
            float d = delta.magnitude;
            if (d < 0.0001f || d > minDist) continue;

            // Насколько ally "впереди" относительно направления к цели
            Vector3 dir = delta / Mathf.Max(d, 0.0001f);
            float forward = Vector3.Dot(toTargetNorm, dir);
            if (forward > bestForward)
            {
                bestForward = forward;
                minDist = d;
                best = ally;
            }
        }

        if (best == null) return Vector3.zero;

        // Правый вектор относительно направления на цель (только XZ)
        Vector3 right = new Vector3(toTargetNorm.z, 0f, -toTargetNorm.x);
        return right * _orbitSign;
    }

    /// <summary>
    /// Локальный cohesion: нормализованный вектор к центроиду СОЮЗНИКОВ в радиусе.
    /// Отдаёт count и distToCentroid для оценки "изоляции".
    /// </summary>
    private Vector3 ComputeCohesionXZ(float radius, out int count, out float distToCentroid)
    {
        Vector3 sumPos = Vector3.zero;
        int localCount = 0;
        Vector3 self = transform.position;

        int n = Physics.OverlapSphereNonAlloc(
            self, radius, _overlap, CharactersMask, QueryTriggerInteraction.Ignore);

        int seen = 0; // дедуп по UnitTeam

        for (int i = 0; i < n && i < _overlap.Length; i++)
        {
            var col = _overlap[i];
            if (col == null) continue;

            var ally = col.GetComponentInParent<UnitTeam>();
            // Нужны ТОЛЬКО СОЮЗНИКИ (и живые)
            if (ally == null || ally == _team) continue;
            if (ally.Team != _team.Team) continue;
            if (ally.Unit == null || ally.Unit.Runtime == null || !ally.Unit.Runtime.IsAlive()) continue;

            bool dup = false;
            for (int s = 0; s < seen; s++) { if (_tmpTeams[s] == ally) { dup = true; break; } }
            if (dup) continue;
            if (seen < _tmpTeams.Length) _tmpTeams[seen++] = ally;

            sumPos += ally.transform.position;
            localCount++;
        }

        count = localCount;

        if (localCount == 0)
        {
            distToCentroid = 0f;
            return Vector3.zero; // рядом нет "стаи" — тянуть некуда
        }

        Vector3 centroid = sumPos / localCount;
        Vector3 toC = centroid - self; toC.y = 0f;
        distToCentroid = toC.magnitude;
        if (distToCentroid < 0.0001f) return Vector3.zero;
        return toC / distToCentroid;
    }
}

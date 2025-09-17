using UnityEngine;
using Game.Units;
using Game.Runtime;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitTeam))]
[RequireComponent(typeof(CharacterController))]
public partial class CombatAgent : MonoBehaviour
{
    // =========================
    // ДВИЖЕНИЕ / АНИМАЦИЯ (параметры движения)
    // =========================
    private const float TurnSpeed = 10f;   // скорость поворота к цели
    private const float SlowRange = 2.5f;  // зона замедления перед StopDistance
    private const float WalkCoef = 0.6f;   // доля walk-скорости от MoveSpeed

    // =========================
    // КАП НА КОЛ-ВО АТАКУЮЩИХ ОДНУ ЦЕЛЬ (используется Targeting)
    // =========================
    private const int MaxAttackersPerTarget = 2;          // максимум атакующих по одной цели
    private const float OccupancyPenaltyPerAttacker = 2f; // мягкий штраф к score за каждого уже-атакующего
    private const float HardBlockPenalty = 1000f;         // жёсткий запрет (если есть свободные цели)

    // =========================
    // ССЫЛКИ / СОСТОЯНИЯ
    // =========================
    private Unit _unit;
    private UnitTeam _team;
    private CharacterController _cc;
    private UnitAnimator _uAnim;

    private UnitTeam _target;         // текущая цель
    private UnitTeam _occupiedTarget; // цель, на которую заняли "слот атакующего"

    private UnitRuntimeStats Stats => _unit.Runtime;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _team = GetComponent<UnitTeam>();
        _cc = GetComponent<CharacterController>();
        TryGetComponent(out _uAnim);

        // Тонкая настройка контроллера и подготовка «земли»
        GroundingAwakeSetup();   // partial: Grounding
        // Стохастика орбит/радиусов и служебные буферы руления
        SteeringAwakeSetup();    // partial: Steering
        // Инициализация тикера умного ретаргета
        RetargetAwakeSetup();    // partial: Retarget
        // Инициализация LOD/стаггера восприятия
        LODAwakeSetup();         // partial: LOD
        // >>> Новое: подтягиваем глобальные дефолты фильтров контакта (вариант C)
        AttackAwakeSetup();      // partial: Attack
    }

    private void OnDisable()
    {
        if (_occupiedTarget != null)
        {
            AttackOccupancy.Release(_occupiedTarget);
            _occupiedTarget = null;
        }
    }

    private void Update()
    {
        if (_unit == null || Stats == null) return;
        if (!Stats.IsAlive()) return;

        // Таймеры удара / lazy-init КД — partial: Attack
        AttackTimersTick(Time.deltaTime);

        // Валидация/поиск цели
        if (_target == null || _target.Unit == null || _target.Unit.Runtime == null ||
            !_target.Unit.Runtime.IsAlive() || _target.Team == _team.Team)
        {
            if (_occupiedTarget != null)
            {
                AttackOccupancy.Release(_occupiedTarget);
                _occupiedTarget = null;
            }
            _target = FindEnemyConsideringOccupancy(); // partial: Targeting
            if (_target == null) return;
        }

        var tStats = _target.Unit.Runtime;
        if (!tStats.IsAlive())
        {
            if (_occupiedTarget != null)
            {
                AttackOccupancy.Release(_occupiedTarget);
                _occupiedTarget = null;
            }
            _target = null;
            return;
        }

        // =========================
        // ОБЩИЕ ВЕЛИЧИНЫ
        // =========================
        float dist = Vector3.Distance(transform.position, _target.transform.position);
        float stopAt = Mathf.Max(Stats.CurrentStats.StopDistance, 0.5f);
        float desiredRadius = Mathf.Max(0.2f, stopAt + _orbitRadiusOffset); // _orbitRadiusOffset из Steering

        // --- Cohesion локально (ТОЛЬКО союзники) — через LOD-обёртку ---
        int cohCount;
        float cohDist;
        Vector3 cohesionDir = LOD_Cohesion(CohesionRadius, out cohCount, out cohDist, dist); // partial: LOD -> Steering
        float isolation = 0f;
        if (cohCount > 0)
        {
            isolation = Mathf.Clamp01((cohDist - IsolationThreshold) /
                                      Mathf.Max(0.001f, CohesionRadius - IsolationThreshold));
        }

        // Почти дошли — считаем "прогресс" (поле хранится в partial: Retarget)
        if (dist <= stopAt + 0.2f) _lastProgressTime = Time.time;

        // --- SMART RETARGET (вынесен) ---
        RetargetTick(cohesionDir, cohCount, cohDist, isolation); // partial: Retarget

        // =========================
        // ПОДХОД К ЦЕЛИ
        // =========================
        if (dist > stopAt)
        {
            Vector3 toTarget = _target.transform.position - transform.position; // вектор к цели
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                toTarget.Normalize();

                // Итоговое направление: toTarget + side-bias + separation + bypass + cohesion
                Vector3 right = new Vector3(toTarget.z, 0f, -toTarget.x);
                Vector3 separation = LOD_Separation(SeparationRadius, dist) * SeparationStrength;   // LOD
                Vector3 bypass = LOD_Bypass(toTarget, BypassAheadDistance, dist) * BypassStrength; // LOD
                Vector3 cohesion = cohesionDir * (CohesionStrength * isolation);

                Vector3 dir = (toTarget + right * _orbitSign * ApproachSideBias + separation + bypass + cohesion);
                if (dir.sqrMagnitude > 0.0001f) dir = dir.normalized;

                FaceTowards(_target.transform.position, TurnSpeed);

                float t = Mathf.InverseLerp(stopAt, stopAt + SlowRange, dist);
                float walkSpeed = Stats.CurrentStats.MoveSpeed * WalkCoef;
                float runSpeed = Stats.CurrentStats.MoveSpeed;
                float chosen = Mathf.Lerp(walkSpeed, runSpeed, t);

                _cc.SimpleMove(dir * chosen);
                StickToGroundIfNeeded(); // partial: Grounding

                // Анимация бега/ходьбы — централизованно
                float targetBlend = Mathf.Lerp(0.5f, 1f, t);
                SetMoveBlendSafe(targetBlend); // partial: Animation
            }
            return; // пока подходим — не бьём
        }

        // =========================
        // У ЦЕЛИ: орбитим + разлипание + cohesion при изоляции
        // =========================
        FaceTowards(_target.transform.position, TurnSpeed);

        Vector3 toT = _target.transform.position - transform.position;
        toT.y = 0f;
        if (toT.sqrMagnitude > 0.0001f)
        {
            toT.Normalize();
            Vector3 right = new Vector3(toT.z, 0f, -toT.x);
            Vector3 tangent = right * _orbitSign; // касательный (боковой) вектор по окружности

            float orbitSpeed = OrbitSideSpeed * (1f - OrbitIsolationDampen * isolation); // из Steering

            // Держим нужный радиус: ближе — отталкиваемся, дальше — подтягиваемся
            float radialError = desiredRadius - dist;
            Vector3 radial = (-toT) * radialError * OrbitRadialGain;

            Vector3 sepOrbit = LOD_Separation(SeparationRadius, dist) * (SeparationStrength * 0.6f); // LOD
            Vector3 cohesion = cohesionDir * (CohesionStrength * isolation);

            Vector3 vel = tangent * orbitSpeed + radial + sepOrbit + cohesion;
            _cc.SimpleMove(vel);
            StickToGroundIfNeeded(); // partial

            // Лёгкое движение в анимации — централизованно
            float targetBlend = Mathf.Clamp01(0.2f + orbitSpeed * 0.2f);
            SetMoveBlendSafe(targetBlend); // partial: Animation
        }

        // =========================
        // АТАКА — попытка стартовать (только рядом с целью)
        // =========================
        AttackTryStart(); // partial: Attack (декремент КД и старт клипа)
    }

    private void FaceTowards(Vector3 worldPos, float turnSpeed)
    {
        Vector3 lookDir = worldPos - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.0001f) return;
        Quaternion to = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, to, Time.deltaTime * turnSpeed);
    }
}

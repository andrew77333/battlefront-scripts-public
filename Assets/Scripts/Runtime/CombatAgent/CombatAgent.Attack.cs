using UnityEngine;
using Game.Units;
using Game.Runtime;

public partial class CombatAgent : MonoBehaviour
{
    // =========================
    // УДАР / ТАЙМИНГ (параметры)
    // =========================
    [Header("Hit reach / timing")]
    [SerializeField] private float ExtraHitRange = 0.4f;                    // допуск к StopDistance
    [SerializeField, Range(0f, 1f)] private float HitTimeNormalized = 0.4f; // момент удара без AnimEvent

    // =========================
    // Вариант C: глобальные дефолты + локальный override
    // =========================
    [Header("Contact Defaults (Variant C)")]
    [Tooltip("Глобальные дефолты фильтров контакта (front-arc/LOS). " +
             "Если не задано, попытаемся найти CombatContactDefaults через Resources.")]
    [SerializeField] private CombatContactDefaults ContactDefaults = null;

    [Tooltip("Если включено — использовать локальные значения ниже вместо глобальных дефолтов.")]
    [SerializeField] private bool OverrideContactSettings = false;

    // --- Локальные поля (работают ТОЛЬКО если OverrideContactSettings == true) ---
    [Header("Contact Overrides (effective only if OverrideContactSettings)")]
    [Tooltip("Требовать фронтальный сектор у цели.")]
    [SerializeField] private bool UseFrontArc = true;

    [Tooltip("Ширина фронтального сектора (180° = полукруг впереди).")]
    [SerializeField, Range(10f, 180f)] private float FrontArcAngleDeg = 150f;

    [Space(6)]
    [Tooltip("Проверять прямую видимость (Raycast) между атакующим и целью.")]
    [SerializeField] private bool UseLOS = false;

    [Tooltip("Высота старта LOS-луча от позиции атакующего (м).")]
    [SerializeField] private float LOSRayHeight = 1.2f;

    [Tooltip("Слои, которые блокируют melee-удары. Исключи персонажей.")]
    [SerializeField] private LayerMask LOSMask = ~0;

    // =========================
    // Debug / Gizmos
    // =========================
    [Header("Debug / Gizmos")]
    [Tooltip("Рисовать фронт-сектор и LOS в Scene при выбранном агенте.")]
    [SerializeField] private bool DebugDraw = false;

    // =========================
    // Runtime-состояния атаки
    // =========================
    private UnitTeam _pendingTarget;   // цель на момент начала анимации удара
    private float _pendingDamage;      // зафиксированный урон
    private bool _hitArmed;            // "оружие взведено" — ждём окно удара
    private float _attackPhaseTimer;   // таймер текущего окна удара
    private float _attackPhaseDuration;
    private bool _pendingHitFired;     // защита от повторной регистрации удара

    private float _attackCd;           // перезарядка атаки (сек)
    private bool _attackCdInitialized; // рандомизация стартовой КД один раз

    // Флажок, чтобы один раз подтянуть глобальные дефолты (лениво, без правок Awake() в основном файле)
    private bool _attackSetupDone;

    // -------------------------
    // Инициализация варианта C (подтягиваем global asset при необходимости)
    // -------------------------
    private void AttackAwakeSetup()
    {
        // Если ссылку на дефолты не задали на компоненте — пробуем глобальный singleton из Resources.
        if (ContactDefaults == null)
            ContactDefaults = CombatContactDefaults.Instance;
    }

    // -------------------------
    // Получение "эффективных" значений с учётом Override или глобала
    // -------------------------
    private bool EffUseFrontArc =>
        OverrideContactSettings ? UseFrontArc :
        (ContactDefaults != null ? ContactDefaults.UseFrontArc : true);

    private float EffFrontArcAngleDeg =>
        OverrideContactSettings ? FrontArcAngleDeg :
        (ContactDefaults != null ? ContactDefaults.FrontArcAngleDeg : 150f);

    private bool EffUseLOS =>
        OverrideContactSettings ? UseLOS :
        (ContactDefaults != null ? ContactDefaults.UseLOS : false);

    private float EffLOSRayHeight =>
        OverrideContactSettings ? LOSRayHeight :
        (ContactDefaults != null ? ContactDefaults.LOSRayHeight : 1.2f);

    private LayerMask EffLOSMask =>
        OverrideContactSettings ? LOSMask :
        (ContactDefaults != null ? ContactDefaults.LOSMask : ~0);

    /// <summary>
    /// Тикает таймер окна удара (fallback) и проводит ленивую инициализацию КД.
    /// </summary>
    private void AttackTimersTick(float dt)
    {
        // ЛЕНИВАЯ инициализация глобальных дефолтов (Variant C).
        if (!_attackSetupDone)
        {
            AttackAwakeSetup();
            _attackSetupDone = true;
        }

        // Инициализируем перезарядку один раз и случайным смещением, чтобы не били в унисон
        if (!_attackCdInitialized)
        {
            float aps = Mathf.Max(0.1f, Stats.CurrentStats.AttackSpeed);
            _attackCd = Random.Range(0f, 1f / aps);
            _attackCdInitialized = true;
        }

        // Если ждём окно удара — тикаем локальный таймер и, если дошли до точки удара,
        // вызываем обработчик (fallback на случай отсутствия Animation Event)
        if (_hitArmed)
        {
            _attackPhaseTimer += dt;
            if (!_pendingHitFired && _attackPhaseTimer >= HitTimeNormalized * _attackPhaseDuration)
                DoHitIfPossible();
        }
    }

    /// <summary>
    /// Пытается запустить атаку: декремент КД и старт клипа, если готовы.
    /// </summary>
    private void AttackTryStart()
    {
        _attackCd -= Time.deltaTime;
        if (_attackCd > 0f) return;
        if (_target == null) return;

        // При начале атаки занимаем "слот" у цели (для лимита атакующих)
        if (_occupiedTarget != _target)
        {
            if (_occupiedTarget != null) AttackOccupancy.Release(_occupiedTarget);
            AttackOccupancy.Acquire(_target);
            _occupiedTarget = _target;
        }

        // Запускаем анимацию удара (централизованно через safe-обёртку)
        PlayAttackSafe();

        // Фиксируем урон и цель на эту атаку
        int min = Stats.CurrentStats.DamageMin;
        int max = Mathf.Max(min, Stats.CurrentStats.DamageMax);
        _pendingDamage = Random.Range(min, max + 1);
        _pendingTarget = _target;
        _hitArmed = true;
        _pendingHitFired = false;

        // Подготавливаем таймер окна удара (для fallback)
        float atkPerSec = Mathf.Max(0.1f, Stats.CurrentStats.AttackSpeed);
        _attackPhaseDuration = 1f / atkPerSec;
        _attackPhaseTimer = 0f;

        // Перезарядка до следующей атаки независима от длины клипа
        _attackCd = 1f / atkPerSec;
    }

    /// <summary>Вызывается из Animation Event (момент контакта клинка/кулака).</summary>
    public void OnAttackHit() => DoHitIfPossible();

    // -------------------------
    // ВСПОМОГАТЕЛЬНОЕ: проверка фронтального сектора
    // -------------------------
    /// <summary>
    /// Возвращает true, если цель находится в фронтальном секторе (угол EffFrontArcAngleDeg).
    /// Сектор рассчитываем в плоскости XZ.
    /// </summary>
    private bool IsInFrontArc(Vector3 attackerPos, Vector3 attackerForward, Vector3 targetPos)
    {
        // Вектор вперёд атакующего (XZ)
        Vector3 fwd = attackerForward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = transform.forward; // на всякий случай
        fwd.Normalize();

        // Нормализованный вектор на цель (XZ)
        Vector3 toTarget = targetPos - attackerPos; toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return true; // совпали позиции — считать "спереди"
        toTarget.Normalize();

        // Порог по косинусу половины угла сектора
        float cosHalf = Mathf.Cos(0.5f * EffFrontArcAngleDeg * Mathf.Deg2Rad);

        // Dot = cos угла между forward и направлением на цель
        float cos = Vector3.Dot(fwd, toTarget);
        return cos >= cosHalf;
    }

    // -------------------------
    // ВСПОМОГАТЕЛЬНОЕ: проверка прямой видимости (LOS)
    // -------------------------
    /// <summary>
    /// True, если между from и to нет препятствий по указанной маске слоёв.
    /// Персонажи не должны входить в эту маску (исключи их из LOSMask в asset’е).
    /// </summary>
    private bool HasLineOfSight(Vector3 from, Vector3 to, float rayHeight, LayerMask mask)
    {
        // Начинаем луч немного выше "пола", чтобы не цеплять землю/ступеньки
        Vector3 origin = from + Vector3.up * Mathf.Max(0f, rayHeight);
        // Бьём примерно в корпус цели
        Vector3 target = to + Vector3.up * 0.5f;

        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist < 0.001f) return true; // почти в одной точке

        dir /= dist;

        // Если что-то поймали — считаем, что LOS заблокирован
        // (персонажи должны быть исключены из mask).
        return !Physics.Raycast(origin, dir, dist, mask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Общий обработчик удара (и для события, и для fallback-таймера).</summary>
    private void DoHitIfPossible()
    {
        if (!_hitArmed) return;
        _hitArmed = false;
        _pendingHitFired = true;

        var tgt = _pendingTarget;
        if (tgt == null || tgt.Unit == null || tgt.Unit.Runtime == null) return;

        var tStats = tgt.Unit.Runtime;
        if (!tStats.IsAlive()) return;

        // Проверяем досягаемость (чуть больше StopDistance)
        float stopAt = Mathf.Max(Stats.CurrentStats.StopDistance, 0.5f);
        float maxDist = stopAt + Mathf.Max(0f, ExtraHitRange);
        float dist = Vector3.Distance(transform.position, tgt.transform.position);
        Vector3 popupPos = tgt.transform.position + Vector3.up * 2f;

        // 1) Слишком далеко — промах
        if (dist > maxDist)
        {
            DamagePopup.Spawn(popupPos, "MISS", Color.gray);
            return;
        }

        // 2) Фронтальный сектор (если включён)
        if (EffUseFrontArc)
        {
            if (!IsInFrontArc(transform.position, transform.forward, tgt.transform.position))
            {
                DamagePopup.Spawn(popupPos, "MISS", Color.gray);
                return;
            }
        }

        // 3) Line-of-Sight (если включён)
        if (EffUseLOS)
        {
            if (!HasLineOfSight(transform.position, tgt.transform.position, EffLOSRayHeight, EffLOSMask))
            {
                DamagePopup.Spawn(popupPos, "MISS", Color.gray);
                return;
            }
        }

        // 4) Случайный промах по статам атакующего
        if (Random.value < Mathf.Clamp01(Stats.CurrentStats.MissChance))
        {
            DamagePopup.Spawn(popupPos, "MISS", Color.gray);
            return;
        }

        // 5) Крит по статам
        bool crit = Random.value < Mathf.Clamp01(Stats.CurrentStats.CritChance);
        float mult = Mathf.Max(1f, Stats.CurrentStats.CritMultiplier);
        float dmg = _pendingDamage * (crit ? mult : 1f);

        // === Применяем урон через фасад с типом и атакующим ===
        DamageSystem.Apply(
            target: tStats,
            amount: dmg,
            isCrit: crit,
            popupPos: popupPos,
            attacker: Stats,
            type: DamageType.Physical // базовый ближний бой — физический
        );

        // Удар произошёл — это "прогресс" (поле в partial: Retarget)
        _lastProgressTime = Time.time;

        // Если цель умерла — сыграем победную и дадим ранг/килл
        if (!tStats.IsAlive())
        {
            PlayVictorySafe();
            Stats.AddKillAndCheckRank();
        }
    }

#if UNITY_EDITOR
    // ---------- Debug Gizmos ----------
    private void OnDrawGizmosSelected()
    {
        if (!DebugDraw) return;

        // Подтянем дефолты, если не инициализированы (в редакторе)
        if (ContactDefaults == null) ContactDefaults = CombatContactDefaults.Instance;

        // Рисуем фронт-сектор
        if (EffUseFrontArc)
        {
            float half = EffFrontArcAngleDeg * 0.5f;
            Vector3 fwd = transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize(); else fwd = Vector3.forward;

            float radius = 2.0f;
            if (Application.isPlaying)
            {
                float stopAt = Mathf.Max(Stats.CurrentStats.StopDistance, 0.5f);
                radius = stopAt + Mathf.Max(0f, ExtraHitRange);
            }

            Gizmos.color = new Color(0f, 1f, 1f, 0.8f);

            Quaternion qL = Quaternion.AngleAxis(-half, Vector3.up);
            Quaternion qR = Quaternion.AngleAxis(+half, Vector3.up);
            Vector3 left = qL * fwd * radius;
            Vector3 right = qR * fwd * radius;

            Vector3 o = transform.position;
            Gizmos.DrawLine(o, o + left);
            Gizmos.DrawLine(o, o + right);

            // Простейшая дуга из сегментов
            int seg = 18;
            Vector3 prev = o + (qL * fwd) * radius;
            for (int i = 1; i <= seg; i++)
            {
                float t = Mathf.Lerp(-half, +half, i / (float)seg);
                Vector3 p = o + (Quaternion.AngleAxis(t, Vector3.up) * fwd) * radius;
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }

        // Рисуем LOS-луч до текущей цели (если есть)
        if (EffUseLOS && _target != null)
        {
            Vector3 o = transform.position + Vector3.up * Mathf.Max(0f, EffLOSRayHeight);
            Vector3 tpos = _target.transform.position + Vector3.up * 0.5f;
            Vector3 dir = tpos - o;
            float dist = dir.magnitude;
            if (dist > 0.001f)
            {
                dir /= dist;
                bool los = !Physics.Raycast(o, dir, dist, EffLOSMask, QueryTriggerInteraction.Ignore);
                Gizmos.color = los ? Color.green : Color.red;
                Gizmos.DrawLine(o, tpos);
            }
        }
    }
#endif
}


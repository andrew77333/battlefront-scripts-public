// CombatAgent.Retarget.cs
// ЧАСТЬ класса CombatAgent: расписание и логика Smart Retarget.

using Game.Runtime;
using UnityEngine;

public partial class CombatAgent : MonoBehaviour
{
    // =========================
    // SMART RETARGET (серилизуемые параметры)
    // =========================
    [Header("Smart Retarget")]
    [SerializeField] private float RetargetCheckInterval = 0.5f;   // периодичность проверки
    [SerializeField] private float RetargetAfterStuck = 1.6f;   // нет прогресса — можно ретаргетиться
    [SerializeField] private float CohesionTargetBias = 0.35f;  // bias к целям ближе к «своим»
    [SerializeField] private float IsolationBiasMultiplier = 2.0f; // усиление bias при изоляции
    [SerializeField, Range(0f, 1f)] private float IsolationRetargetThreshold = 0.6f;

    // Runtime
    private float _lastProgressTime;    // время последнего «прогресса»
    private float _nextRetargetCheckAt; // следующее время проверки ретаргета

    // Вызывается из Awake ядра
    private void RetargetAwakeSetup()
    {
        _lastProgressTime = Time.time;
        _nextRetargetCheckAt = Time.time + Random.Range(0f, RetargetCheckInterval);
    }

    /// <summary>
    /// Единая точка тика smart-ретаргета. При необходимости меняет цель и освобождает слоты.
    /// </summary>
    /// <param name="cohesionDir">направление к центроиду союзников</param>
    /// <param name="cohCount">сколько союзников учтено</param>
    /// <param name="cohDist">дистанция до центроида</param>
    /// <param name="isolation">0..1, насколько мы оторваны от стаи</param>
    private void RetargetTick(Vector3 cohesionDir, int cohCount, float cohDist, float isolation)
    {
        if (Time.time < _nextRetargetCheckAt) return;
        _nextRetargetCheckAt = Time.time + RetargetCheckInterval;

        bool stuckTooLong = (Time.time - _lastProgressTime) >= RetargetAfterStuck;
        bool isolated = isolation >= IsolationRetargetThreshold;

        if (!stuckTooLong && !isolated) return;

        // Смещаем «центр притяжения» к своим — bias к целям возле стаи работает лучше
        Vector3 centroidPos = (cohCount > 0)
            ? (transform.position + cohesionDir * cohDist)
            : transform.position;

        var newTarget = FindEnemyWithOccupancyAndCentroidBias(centroidPos, isolation); // partial: Targeting
        if (newTarget == null || newTarget == _target) return;

        // Меняем цель корректно: освобождаем старый "слот"
        if (_occupiedTarget != null)
        {
            AttackOccupancy.Release(_occupiedTarget);
            _occupiedTarget = null;
        }

        _target = newTarget;
        _lastProgressTime = Time.time; // смена цели — тоже прогресс
    }
}

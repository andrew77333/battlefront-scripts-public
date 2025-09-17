// CombatAgent.Targeting.cs
// ЧАСТЬ класса CombatAgent: только логика выбора цели.
// Поведение не меняем, просто выносим из «большого» файла ради читаемости.
// CombatAgent.Targeting.cs
// ЧАСТЬ класса CombatAgent: выбор цели (без изменений поведения).

using UnityEngine;
using Game.Units;
using Game.Runtime;

public partial class CombatAgent : MonoBehaviour
{
    /// <summary>
    /// Проверяем, нет ли «свободной» цели. Если все живые уже имеют максимум атакующих — вернёт true.
    /// Нужна для мягкого лимита: когда все заняты, не применяем жёсткий HardBlockPenalty.
    /// </summary>
    private bool AreAllOpponentsFull()
    {
        var opponents = UnitRegistry.GetOpponents(_team.Team);
        for (int i = 0; i < opponents.Count; i++)
        {
            var cand = opponents[i];
            if (cand == null || cand == _team) continue;
            if (cand.Team == _team.Team) continue;
            if (cand.Unit == null || cand.Unit.Runtime == null) continue;
            if (!cand.Unit.Runtime.IsAlive()) continue;

            int occ = AttackOccupancy.GetCount(cand);
            if (occ < MaxAttackersPerTarget) return false; // нашёлся свободный — не все full
        }
        return true; // все живые переполнены
    }

    /// <summary>
    /// Базовый выбор врага: ближе + свободнее (через реестр, без FindObjectsByType).
    /// </summary>
    private UnitTeam FindEnemyConsideringOccupancy()
    {
        var opponents = UnitRegistry.GetOpponents(_team.Team);

        UnitTeam best = null;
        float bestScore = float.MaxValue;
        Vector3 self = transform.position;

        // Важная логика мягкого лимита:
        bool allFull = AreAllOpponentsFull(); // если все уже заняты по лимиту — не ставим жёсткий запрет

        foreach (var cand in opponents)
        {
            if (cand == null || cand == _team) continue;
            if (cand.Team == _team.Team) continue;
            if (cand.Unit == null || cand.Unit.Runtime == null) continue;
            if (!cand.Unit.Runtime.IsAlive()) continue;

            float dist = Vector3.Distance(self, cand.transform.position);
            int occ = AttackOccupancy.GetCount(cand);

            float penalty = allFull
                ? (occ * OccupancyPenaltyPerAttacker) // только мягкий штраф
                : ((occ >= MaxAttackersPerTarget) ? HardBlockPenalty : occ * OccupancyPenaltyPerAttacker);

            float score = dist + penalty;
            if (score < bestScore) { bestScore = score; best = cand; }
        }
        return best;
    }

    /// <summary>
    /// Выбор врага: ближе + свободнее + bias к целям около "своих".
    /// Используется в умном ретаргете (когда застрял или изолирован).
    /// </summary>
    private UnitTeam FindEnemyWithOccupancyAndCentroidBias(Vector3 allyCentroid, float isolation)
    {
        var opponents = UnitRegistry.GetOpponents(_team.Team);

        UnitTeam best = null;
        float bestScore = float.MaxValue;
        Vector3 self = transform.position;

        float bias = CohesionTargetBias * Mathf.Lerp(1f, IsolationBiasMultiplier, Mathf.Clamp01(isolation));
        bool allFull = AreAllOpponentsFull();

        foreach (var cand in opponents)
        {
            if (cand == null || cand == _team) continue;
            if (cand.Team == _team.Team) continue;
            if (cand.Unit == null || cand.Unit.Runtime == null) continue;
            if (!cand.Unit.Runtime.IsAlive()) continue;

            float dist = Vector3.Distance(self, cand.transform.position);
            int occ = AttackOccupancy.GetCount(cand);
            float occPenalty = allFull
                ? (occ * OccupancyPenaltyPerAttacker)
                : ((occ >= MaxAttackersPerTarget) ? HardBlockPenalty : occ * OccupancyPenaltyPerAttacker);

            // Чем дальше кандидат от центроида "своих", тем больше штраф
            float centroidDist = Vector3.Distance(allyCentroid, cand.transform.position);
            float centroidPenalty = centroidDist * bias;

            float score = dist + occPenalty + centroidPenalty;
            if (score < bestScore) { bestScore = score; best = cand; }
        }
        return best;
    }
}

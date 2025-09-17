using System;
using UnityEngine;
using Game.Data;
using System.Linq;

namespace Game.Runtime
{
    public class UnitRuntimeStats : MonoBehaviour
    {
        public UnitArchetype Archetype;

        public StatBlock CurrentStats;
        public float CurrentHealth;

        public int CurrentLevel = 1;
        public int CurrentRankIndex = 0;

        [Header("Progression")]
        [Tooltip("Сколько врагов убил этот юнит (счётчик для рангов).")]
        public int KillCount = 0;

        [HideInInspector] public UnitRankStep CurrentRank;

        public bool IsDead { get; private set; }
        public event Action<UnitRuntimeStats> Died;
        public event Action<float, UnitRuntimeStats> Damaged;
        public event Action<float, UnitRuntimeStats> Healed; // новое событие

        public void RecalculateStats()
        {
            if (Archetype == null) return;

            // базовая копия
            CurrentStats = Archetype.BaseStats.Clone();

            // уровень
            if (Archetype.UnitLevelListRef != null)
            {
                var levels = Archetype.UnitLevelListRef.Levels;
                if (levels != null && CurrentLevel - 1 < levels.Length)
                {
                    var step = levels[CurrentLevel - 1];
                    CurrentStats.ApplyBonuses(step.FlatBonuses, step.PercentBonuses);
                }
            }

            // звание
            if (Archetype.UnitRankListRef != null)
            {
                var ranks = Archetype.UnitRankListRef.Ranks;
                if (ranks != null && CurrentRankIndex < ranks.Length)
                {
                    var rank = ranks[CurrentRankIndex];
                    CurrentStats.ApplyBonuses(rank.FlatBonuses, rank.PercentBonuses);
                }
            }

            CurrentStats.Clamp();
        }

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;

            CurrentHealth -= dmg;
            Damaged?.Invoke(dmg, this);
            if (CurrentHealth <= 0f)
            {
                CurrentHealth = 0f;
                Die();
            }
        }

        /// <summary>Лечение. Возвращает фактически восстановленное здоровье.</summary>
        public float Heal(float amount)
        {
            if (IsDead) return 0f;
            if (amount <= 0f) return 0f;

            float maxHp = CurrentStats != null ? CurrentStats.Health : CurrentHealth;
            float before = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHp, CurrentHealth + amount);
            float applied = Mathf.Max(0f, CurrentHealth - before);
            if (applied > 0f) Healed?.Invoke(applied, this);
            return applied;
        }

        public bool IsAlive() => !IsDead && CurrentHealth > 0f;

        public void AddKillAndCheckRank()
        {
            KillCount++;

            if (Archetype == null || Archetype.UnitRankListRef == null) return;
            var list = Archetype.UnitRankListRef.Ranks;
            if (list == null || list.Length == 0) return;

            var newRank = list
                .Where(r => KillCount >= r.RequiredKills)
                .OrderByDescending(r => r.RequiredKills)
                .FirstOrDefault();

            if (newRank != null && CurrentRank != newRank)
            {
                CurrentRank = newRank;

                if (newRank.FlatBonuses != null)
                    CurrentStats.ApplyBonuses(newRank.FlatBonuses, null);
                if (newRank.PercentBonuses != null)
                    CurrentStats.ApplyBonuses(null, newRank.PercentBonuses);

                CurrentStats.Clamp();
                Debug.Log($"{name} получил новый ранг: {newRank.RankName}");
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Debug.Log($"{name} DEAD.");

            Died?.Invoke(this);
        }
    }
}

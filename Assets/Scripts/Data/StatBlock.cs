using System;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Универсальный набор характеристик юнита (без логики).
    /// Храним ТОЛЬКО данные — это фундамент для уровней (A) и званий (B).
    /// </summary>
    [Serializable]
    public class StatBlock
    {
        // -------------------------
        // OFFENSE (атака)
        // -------------------------

        [Tooltip("Минимальный урон за один успешный удар/выстрел.")]
        public int DamageMin = 5;

        [Tooltip("Максимальный урон за один успешный удар/выстрел.")]
        public int DamageMax = 10;

        [Tooltip("Скорость атаки: сколько атак в секунду (напр., 1.0 = одна атака/сек).")]
        public float AttackSpeed = 1.0f;

        [Tooltip("Дальность атаки в метрах (ближний бой ~1.5-2.0, лучник 6-12).")]
        public float AttackRange = 1.8f;

        [Tooltip("Шанс промаха атакующего в [0..1], напр. 0.05 = 5%.")]
        public float MissChance = 0.05f;

        [Tooltip("Шанс критического удара в [0..1], напр. 0.2 = 20%.")]
        public float CritChance = 0.20f;

        [Tooltip("Множитель урона при крите (напр., 1.5 = x1.5 урон).")]
        public float CritMultiplier = 1.5f;

        [Tooltip("Плоское пробитие брони (снимается из брони цели до расчёта урона).")]
        public float ArmorPenetrationFlat = 0.0f;

        [Tooltip("Процентное пробитие брони (0..1). 0.3 = игнорировать 30% брони.")]
        public float ArmorPenetrationPct = 0.0f;

        // Создаёт полную копию текущих статов (на базе уже существующего CopyFrom).
        public StatBlock Clone()
        {
            var copy = new StatBlock();
            copy.CopyFrom(this);
            return copy;
        }

        // Унифицированное применение бонусов: сначала flat, затем процентные; в конце Clamp().
        public void ApplyBonuses(StatBlock flat, StatBlock percent)
        {
            if (flat != null) AddFlat(flat);
            if (percent != null) AddPercent(percent);
            Clamp();
        }

        // -------------------------
        // DEFENSE (защита)
        // -------------------------

        [Tooltip("Максимальный запас здоровья юнита.")]
        public int Health = 100;

        [Tooltip("Восстановление здоровья в секунду (может быть 0).")]
        public float HealthRegen = 0.0f;

        [Tooltip("Броня против физического урона (чем больше, тем меньше урон).")]
        public float Armor = 0.0f;

        [Tooltip("Сопротивление магии/особому урону (по необходимости).")]
        public float MagicResist = 0.0f;

        [Tooltip("Шанс уклонения в [0..1] (напр., 0.1 = 10% шанс полностью избежать урона).")]
        public float Evasion = 0.0f;

        [Tooltip("Шанс блокирования части урона щитом/бронёй в [0..1].")]
        public float BlockChance = 0.0f;

        // -------------------------
        // MOBILITY / PERCEPTION (подвижность/радиусы)
        // -------------------------

        [Tooltip("Скорость перемещения в м/с (гуманоид обычно 2–4).")]
        public float MoveSpeed = 2.5f;

        [Tooltip("Радиус агро: на каком расстоянии юнит замечает врага и начинает реагировать.")]
        public float AggroRadius = 6.0f;

        [Tooltip("Дистанция остановки при подходе к цели (чуть меньше AttackRange).")]
        public float StopDistance = 1.4f;

        // -------------------------
        // ECONOMY / META (экономика/прочее)
        // -------------------------

        [Tooltip("Стоимость в золоте (если есть экономика).")]
        public int CostGold = 0;

        [Tooltip("Стоимость в редком ресурсе (если есть).")]
        public int CostCrystal = 0;

        [Tooltip("Слот населения/лимит армии (если используется).")]
        public int Supply = 1;

        [Tooltip("Время производства (сек).")]
        public float ProductionTime = 2.0f;

        // -------------------------
        // ВСПОМОГАТЕЛЬНО: простые операции
        // -------------------------

        public void CopyFrom(StatBlock other)
        {
            // OFFENSE
            DamageMin = other.DamageMin;
            DamageMax = other.DamageMax;
            AttackSpeed = other.AttackSpeed;
            AttackRange = other.AttackRange;
            MissChance = other.MissChance;
            CritChance = other.CritChance;
            CritMultiplier = other.CritMultiplier;
            ArmorPenetrationFlat = other.ArmorPenetrationFlat;
            ArmorPenetrationPct = other.ArmorPenetrationPct;

            // DEFENSE
            Health = other.Health;
            HealthRegen = other.HealthRegen;
            Armor = other.Armor;
            MagicResist = other.MagicResist;
            Evasion = other.Evasion;
            BlockChance = other.BlockChance;

            // MOBILITY
            MoveSpeed = other.MoveSpeed;
            AggroRadius = other.AggroRadius;
            StopDistance = other.StopDistance;

            // ECONOMY
            CostGold = other.CostGold;
            CostCrystal = other.CostCrystal;
            Supply = other.Supply;
            ProductionTime = other.ProductionTime;
        }

        public void AddFlat(StatBlock add)
        {
            // OFFENSE
            DamageMin += add.DamageMin;
            DamageMax += add.DamageMax;
            AttackSpeed += add.AttackSpeed;
            AttackRange += add.AttackRange;
            MissChance += add.MissChance;
            CritChance += add.CritChance;
            CritMultiplier += add.CritMultiplier;
            ArmorPenetrationFlat += add.ArmorPenetrationFlat;
            ArmorPenetrationPct += add.ArmorPenetrationPct;

            // DEFENSE
            Health += add.Health;
            HealthRegen += add.HealthRegen;
            Armor += add.Armor;
            MagicResist += add.MagicResist;
            Evasion += add.Evasion;
            BlockChance += add.BlockChance;

            // MOBILITY
            MoveSpeed += add.MoveSpeed;
            AggroRadius += add.AggroRadius;
            StopDistance += add.StopDistance;

            // ECONOMY
            CostGold += add.CostGold;
            CostCrystal += add.CostCrystal;
            Supply += add.Supply;
            ProductionTime += add.ProductionTime;
        }

        public void AddPercent(StatBlock pct)
        {
            // OFFENSE
            DamageMin = Mathf.RoundToInt(DamageMin * (1f + pct.DamageMin));
            DamageMax = Mathf.RoundToInt(DamageMax * (1f + pct.DamageMax));
            AttackSpeed *= (1f + pct.AttackSpeed);
            AttackRange *= (1f + pct.AttackRange);
            MissChance *= (1f + pct.MissChance);
            CritChance *= (1f + pct.CritChance);
            CritMultiplier *= (1f + pct.CritMultiplier);
            ArmorPenetrationFlat *= (1f + pct.ArmorPenetrationFlat);
            ArmorPenetrationPct *= (1f + pct.ArmorPenetrationPct);

            // DEFENSE
            Health = Mathf.RoundToInt(Health * (1f + pct.Health));
            HealthRegen *= (1f + pct.HealthRegen);
            Armor *= (1f + pct.Armor);
            MagicResist *= (1f + pct.MagicResist);
            Evasion *= (1f + pct.Evasion);
            BlockChance *= (1f + pct.BlockChance);

            // MOBILITY
            MoveSpeed *= (1f + pct.MoveSpeed);
            AggroRadius *= (1f + pct.AggroRadius);
            StopDistance *= (1f + pct.StopDistance);

            // ECONOMY
            CostGold = Mathf.RoundToInt(CostGold * (1f + pct.CostGold));
            CostCrystal = Mathf.RoundToInt(CostCrystal * (1f + pct.CostCrystal));
            Supply = Mathf.RoundToInt(Supply * (1f + pct.Supply));
            ProductionTime *= (1f + pct.ProductionTime);
        }

        public void Clamp()
        {
            // Вероятности 0..1
            MissChance = Mathf.Clamp01(MissChance);
            CritChance = Mathf.Clamp01(CritChance);
            ArmorPenetrationPct = Mathf.Clamp01(ArmorPenetrationPct);
            Evasion = Mathf.Clamp01(Evasion);
            BlockChance = Mathf.Clamp01(BlockChance);

            // Ненегативные величины
            DamageMin = Mathf.Max(0, DamageMin);
            DamageMax = Mathf.Max(DamageMin, DamageMax);
            AttackSpeed = Mathf.Max(0f, AttackSpeed);
            AttackRange = Mathf.Max(0f, AttackRange);
            CritMultiplier = Mathf.Max(1f, CritMultiplier); // минимум x1

            Health = Mathf.Max(1, Health);
            HealthRegen = Mathf.Max(0f, HealthRegen);
            Armor = Mathf.Max(0f, Armor);
            MagicResist = Mathf.Max(0f, MagicResist);

            MoveSpeed = Mathf.Max(0f, MoveSpeed);
            AggroRadius = Mathf.Max(0f, AggroRadius);
            StopDistance = Mathf.Clamp(StopDistance, 0f, AttackRange);

            CostGold = Mathf.Max(0, CostGold);
            CostCrystal = Mathf.Max(0, CostCrystal);
            Supply = Mathf.Max(0, Supply);
            ProductionTime = Mathf.Max(0f, ProductionTime);
        }
    }
}

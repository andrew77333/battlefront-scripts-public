using UnityEngine;
using Game.Runtime; // UnitRuntimeStats

public enum DamageType
{
    Physical,
    Magic,
    True // игнорирует броню/резисты
}

/// <summary>
/// Фасад применения урона/лечения.
/// v1: уклонение (Evasion), блок (Block), броня/резисты, попапы, события.
/// v2: умеет работать через очередь (DamageResolver) — строгий порядок в конце кадра.
/// </summary>
public static class DamageSystem
{
    // Чем больше K, тем слабее влияние брони/резистов (подбираем под игру)
    private const float ARMOR_K = 100f;
    private const float MAGIC_K = 100f;

    // Сколько урона "съедает" блок (50% по умолчанию)
    private const float BLOCK_REDUCTION = 0.5f;

    // ----- События (для логов/UI/аур и т.п.) -----
    public struct DamageEvent
    {
        public UnitRuntimeStats Attacker;
        public UnitRuntimeStats Target;

        public float BaseAmount;   // входящее (после крита со стороны атакующего)
        public float FinalAmount;  // фактически нанесённый
        public DamageType Type;

        public bool IsCrit;
        public bool WasEvaded;     // цель уклонилась
        public bool WasBlocked;    // цель заблокировала часть урона

        public Vector3 WorldPos;   // где рисовали попап
    }

    public struct HealEvent
    {
        public UnitRuntimeStats Healer;
        public UnitRuntimeStats Target;
        public float Amount;
        public Vector3 WorldPos;
    }

    public static System.Action<DamageEvent> OnApplied;
    public static System.Action<HealEvent> OnHealed;

    // ----- Перегрузка "как раньше": физ. урон без указания атакующего -----
    public static void Apply(UnitRuntimeStats target, float amount, bool isCrit, Vector3 popupPos)
    {
        Apply(target, amount, isCrit, popupPos, attacker: null, type: DamageType.Physical);
    }

    /// <summary>
    /// Публичная точка входа. Если включён DamageResolver — кладём заявку в очередь и выходим.
    /// Иначе применяем немедленно (как раньше).
    /// </summary>
    public static void Apply(
        UnitRuntimeStats target,
        float amount,
        bool isCrit,
        Vector3 popupPos,
        UnitRuntimeStats attacker,
        DamageType type = DamageType.Physical)
    {
        if (DamageResolver.ExistsAndEnabled)
        {
            DamageResolver.Instance.EnqueueDamage(new DamageResolver.DamageRequest
            {
                target = target,
                amount = amount,
                isCrit = isCrit,
                popupPos = popupPos,
                attacker = attacker,
                type = type
            });
            return;
        }

        ApplyImmediate(target, amount, isCrit, popupPos, attacker, type);
    }

    /// <summary>Простое лечение (без модификаторов). С очередью — аналогично.</summary>
    public static void Heal(UnitRuntimeStats target, float amount, Vector3 popupPos, UnitRuntimeStats healer = null)
    {
        if (DamageResolver.ExistsAndEnabled)
        {
            DamageResolver.Instance.EnqueueHeal(new DamageResolver.HealRequest
            {
                target = target,
                amount = amount,
                popupPos = popupPos,
                healer = healer
            });
            return;
        }

        HealImmediate(target, amount, popupPos, healer);
    }

    // ===================== ИММЕДИАТНЫЕ ВЕРСИИ (для резолвера/фоллбэка) =====================

    internal static void ApplyImmediate(
        UnitRuntimeStats target,
        float amount,
        bool isCrit,
        Vector3 popupPos,
        UnitRuntimeStats attacker,
        DamageType type = DamageType.Physical)
    {
        if (target == null || !target.IsAlive()) return;
        if (amount <= 0f) return;

        var tStats = target.CurrentStats;
        var aStats = attacker != null ? attacker.CurrentStats : null;

        // 1) EVASION (уклонение цели)
        if (Random.value < Mathf.Clamp01(tStats.Evasion))
        {
            DamagePopup.Spawn(popupPos, "MISS", Color.gray);

            OnApplied?.Invoke(new DamageEvent
            {
                Attacker = attacker,
                Target = target,
                BaseAmount = amount,
                FinalAmount = 0f,
                Type = type,
                IsCrit = isCrit,
                WasEvaded = true,
                WasBlocked = false,
                WorldPos = popupPos
            });
            return;
        }

        // 2) Редукция бронёй/резистами
        float final = ComputeDamageAfterResists(aStats, tStats, amount, type);
        if (final > 0f) final = Mathf.Max(1f, final);

        bool wasBlocked = false;

        // 3) BLOCK (частичное поглощение)
        if (Random.value < Mathf.Clamp01(tStats.BlockChance))
        {
            wasBlocked = true;
            final *= (1f - Mathf.Clamp01(BLOCK_REDUCTION));
        }

        final = Mathf.Max(0f, final);

        // 4) Применяем
        target.TakeDamage(final);

        // Цвет попапа
        Color col = isCrit
            ? new Color(1f, 0.45f, 0.2f)
            : (wasBlocked ? new Color(0.55f, 0.8f, 1f) : new Color(1f, 0.95f, 0.35f));

        DamagePopup.Spawn(popupPos, Mathf.RoundToInt(final).ToString(), col);

        // 5) Событие
        OnApplied?.Invoke(new DamageEvent
        {
            Attacker = attacker,
            Target = target,
            BaseAmount = amount,
            FinalAmount = final,
            Type = type,
            IsCrit = isCrit,
            WasEvaded = false,
            WasBlocked = wasBlocked,
            WorldPos = popupPos
        });
    }

    internal static void HealImmediate(UnitRuntimeStats target, float amount, Vector3 popupPos, UnitRuntimeStats healer = null)
    {
        if (target == null || !target.IsAlive()) return;
        if (amount <= 0f) return;

        float applied = target.Heal(amount);

        DamagePopup.Spawn(popupPos, $"+{Mathf.RoundToInt(applied)}", new Color(0.35f, 1f, 0.5f));

        OnHealed?.Invoke(new HealEvent
        {
            Healer = healer,
            Target = target,
            Amount = applied,
            WorldPos = popupPos
        });
    }

    // ===== ВНУТРЕННЕЕ: редукция бронёй/резистами (+ Penetration атакующего) =====
    private static float ComputeDamageAfterResists(
        Game.Data.StatBlock attackerStats,
        Game.Data.StatBlock targetStats,
        float baseAmount,
        DamageType type)
    {
        float dmg = baseAmount;

        switch (type)
        {
            case DamageType.Physical:
                {
                    float armor = Mathf.Max(0f, targetStats.Armor);

                    float flatPen = attackerStats != null ? Mathf.Max(0f, attackerStats.ArmorPenetrationFlat) : 0f;
                    float pctPen = attackerStats != null ? Mathf.Clamp01(attackerStats.ArmorPenetrationPct) : 0f;

                    armor = Mathf.Max(0f, armor * (1f - pctPen) - flatPen);

                    float reduction = armor / (armor + ARMOR_K);
                    dmg *= (1f - reduction);
                    break;
                }
            case DamageType.Magic:
                {
                    float mr = Mathf.Max(0f, targetStats.MagicResist);
                    float reduction = mr / (mr + MAGIC_K);
                    dmg *= (1f - reduction);
                    break;
                }
            case DamageType.True:
            default:
                // true damage — без изменений
                break;
        }

        return Mathf.Max(0f, dmg);
    }
}

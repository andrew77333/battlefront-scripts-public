using System;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Один шаг прокачки юнита через апгрейд здания.
    /// Например, Level 2 даёт +20 HP, +5 Damage.
    /// </summary>
    [Serializable]
    public class UnitLevelStep
    {
        [Tooltip("Номер уровня (начинаем с 1).")]
        public int Level = 1;

        [Tooltip("Flat-бонусы (прибавляются к базовым статам).")]
        public StatBlock FlatBonuses = new StatBlock();

        [Tooltip("Percent-бонусы (например, +0.1 = +10%).")]
        public StatBlock PercentBonuses = new StatBlock();

        [Tooltip("Опционально: ссылка на альтернативный префаб (если у юнита меняется внешний вид).")]
        public GameObject VisualPrefabOverride;
    }

    /// <summary>
    /// Список шагов прокачки для юнита.
    /// Например: Level 1 (базовый), Level 2 (+20 HP), Level 3 (+ещё бонусы).
    /// </summary>
    [CreateAssetMenu(
        fileName = "UnitLevelList",
        menuName = "Game/Units/Unit Level List",
        order = 1)]
    public class UnitLevelList : ScriptableObject
    {
        [Tooltip("Шаги уровней для этого юнита.")]
        public UnitLevelStep[] Levels;
    }
}

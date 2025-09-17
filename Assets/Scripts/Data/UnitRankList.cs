using System;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Один шаг "звания" юнита (за опыт/киллы).
    /// </summary>
    [Serializable]
    public class UnitRankStep
    {
        [Tooltip("Название звания (например: Private, Veteran, Officer).")]
        public string RankName = "Rank";

        [Tooltip("Сколько убийств/опыта нужно для перехода на это звание.")]
        public int RequiredKills = 0;

        [Tooltip("Flat-бонусы (прибавляются к базовым статам).")]
        public StatBlock FlatBonuses = new StatBlock();

        [Tooltip("Percent-бонусы (например, +0.1 = +10%).")]
        public StatBlock PercentBonuses = new StatBlock();

        [Tooltip("Опционально: ссылка на визуальный апгрейд (например, другой материал или украшение).")]
        public GameObject VisualPrefabOverride;
    }

    /// <summary>
    /// Список всех званий для юнита.
    /// Пример: Private → Veteran → Officer.
    /// </summary>
    [CreateAssetMenu(
        fileName = "UnitRankList",
        menuName = "Game/Units/Unit Rank List",
        order = 2)]
    public class UnitRankList : ScriptableObject
    {
        [Tooltip("Все звания (по порядку).")]
        public UnitRankStep[] Ranks;
    }
}

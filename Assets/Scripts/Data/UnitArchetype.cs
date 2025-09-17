using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Описывает "архетип" юнита: базовые данные без логики.
    /// Это то, что крупные студии держат как конфиг: имя, иконка, префаб, базовые статы и ссылки на визуал.
    /// Никакой игровой логики здесь нет — только данные.
    /// </summary>
    [CreateAssetMenu(
        fileName = "UnitArchetype",
        menuName = "Game/Units/Unit Archetype",
        order = 0)]
    public class UnitArchetype : ScriptableObject
    {
        // ========== ИДЕНТИФИКАЦИЯ / ВИЗУАЛ ==========

        [Header("Identity / Visual")]

        [Tooltip("Человекочитаемое имя юнита для UI/отладки (например, Paladin, Mutant).")]
        public string DisplayName = "Unnamed Unit";

        [Tooltip("Условный ID (уникальная строка). Можно оставить пустым на старте и заполнять позже.")]
        public string Id = "";

        [Tooltip("Иконка для UI (по желанию). Не обязательна на старте.")]
        public Sprite Icon;

        // ========== ПРЕФАБ / АНИМАЦИИ ==========

        [Header("Prefab / Animation")]

        [Tooltip("Префаб юнита (с компонентами: Animator, CharacterController и пр.).")]
        public GameObject UnitPrefab;

        [Tooltip("Animator Override Controller для этого юнита (замена клипов поверх базового контроллера).")]
        public AnimatorOverrideController AnimatorOverride;

        // ========== БАЗОВЫЕ СТАТЫ ==========

        [Header("Base Stats")]

        [Tooltip("Базовые характеристики юнита без уровней (A) и званий (B).")]
        public StatBlock BaseStats = new StatBlock();

        // ========== РАСШИРЕНИЯ (добавим позже) ==========

        [Header("Future Links (optional)")]

        //[Tooltip("Список уровней от здания (A). Пока можно оставить пустым — добавим после создания типов.")]
        //public ScriptableObject UnitLevelListRef; // временный placeholder; заменим конкретным типом, когда создадим его

        //[Tooltip("Список званий/звезд от киллов (B). Пока пусто — добавим после создания типов.")]
        //public ScriptableObject UnitRankListRef;  // временный placeholder; заменим конкретным типом, когда создадим его

        // правильные типы вместо ScriptableObject
        public UnitLevelList UnitLevelListRef;
        public UnitRankList UnitRankListRef;

        [Tooltip("Профиль ИИ (радиусы, ретаргет и пр.). Оставляем пустым до добавления AIProfile.")]
        public ScriptableObject AIProfileRef;     // временный placeholder; заменим конкретным типом позже
    }
}

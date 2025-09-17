using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// Профиль анимаций для юнита (кол-во вариантов по состояниям и описания атак).
    /// Мы НЕ храним тут ссылки на AnimationClip — замены делаются через AnimatorOverrideController.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Animations/UnitAnimationProfile")]
    public class UnitAnimationProfile : ScriptableObject
    {
        [Header("Randomized states (not level-gated)")]
        [Tooltip("Сколько вариантов Idle предусмотрено (Idle_0..Idle_N-1). Если пока нет — можно временно указать 1 и привязать один и тот же клип во все слоты Override-контроллера.")]
        public int IdleVariants = 1;

        [Tooltip("Сколько вариантов Death (Death_0..Death_N-1).")]
        public int DeathVariants = 1;

        [Tooltip("Сколько вариантов Victory (Victory_0..Victory_N-1).")]
        public int VictoryVariants = 1;

        [System.Serializable]
        public struct AttackEntry
        {
            [Tooltip("Минимальный уровень юнита, с которого эта анимация атаки может выпадать.")]
            public int UnlockLevel;

            [Tooltip("Множитель урона для этой атаки (1.0 = без изменений). Будем применять позже, когда подключим уровни и расчет урона.")]
            public float DamageMultiplier;

            [Range(0f, 1f)]
            [Tooltip("Нормализованное время (0..1) момента удара/выстрела в клипе. Позже используем для синхрона хитбокса/снаряда.")]
            public float FireTimeNormalized;
        }

        [Header("Attack (level-gated)")]
        [Tooltip("Варианты атаки. Сейчас на первом шаге используем только их количество; уровни и множители подключим следующим шагом.")]
        public AttackEntry[] AttackVariants = new AttackEntry[] { new AttackEntry { UnlockLevel = 1, DamageMultiplier = 1f, FireTimeNormalized = 0.35f } };
    }
}

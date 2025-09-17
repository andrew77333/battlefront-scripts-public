using UnityEngine;
using Game.Data;
using Game.Runtime;

namespace Game.Units
{
    /// <summary>
    /// Главный компонент юнита в сцене.
    /// Хранит ссылку на архетип и создаёт живые статы.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Архетип (конфиг) для этого юнита.")]
        public UnitArchetype Archetype;

        [Header("Lifecycle")]
        [Tooltip("Если включено — удалить объект после смерти.")]
        public bool DestroyOnDeath = false;

        [Tooltip("Задержка удаления после смерти, сек.")]
        public float DestroyDelay = 3f;

        [HideInInspector] public UnitRuntimeStats Runtime; // ссылка для удобства

        private void Start()
        {
            // добавляем/находим Runtime
            if (!TryGetComponent(out Runtime))
                Runtime = gameObject.AddComponent<UnitRuntimeStats>();

            // инициализируем из архетипа
            Runtime.Archetype = Archetype;
            Runtime.RecalculateStats();
            Runtime.CurrentHealth = Runtime.CurrentStats.Health;

            // подписка на смерть
            Runtime.Died += OnDied;

            // стартовый лог
            Debug.Log($"=== RUNTIME {Archetype?.DisplayName ?? name} ===");
            Debug.Log($"Health: {Runtime.CurrentStats.Health}");
            Debug.Log($"Damage: {Runtime.CurrentStats.DamageMin} - {Runtime.CurrentStats.DamageMax}");
            Debug.Log($"Attack Speed: {Runtime.CurrentStats.AttackSpeed}");
            Debug.Log("===============================");
        }

        private void OnDestroy()
        {
            if (Runtime != null) Runtime.Died -= OnDied;
        }

        private void OnDied(UnitRuntimeStats stats)
        {
            // 1) отключаем боевой ИИ
            if (TryGetComponent<CombatAgent>(out var ai)) ai.enabled = false;

            // 2) отключаем перемещение
            if (TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;

            // 3) если есть наш прослоечный аниматор — выбираем случайный индекс смерти ДО триггера
            if (TryGetComponent<UnitAnimator>(out var uAnim))
                uAnim.SetRandomDeathIndex();

            // 4) триггерим смерть в Animator (Trigger "Death")
            if (TryGetComponent<Animator>(out var anim))
            {
                if (HasParam(anim, "Death", AnimatorControllerParameterType.Trigger))
                    anim.SetTrigger("Death");
            }

            // 5) прячем HealthBar
            if (TryGetComponent<HealthBarLink>(out var bar))
                bar.HideBar();

            // 6) удаление по желанию
            if (DestroyOnDeath)
                Destroy(gameObject, Mathf.Max(0f, DestroyDelay));
        }

        private static bool HasParam(Animator a, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in a.parameters)
                if (p.type == type && p.name == name) return true;
            return false;
        }
    }
}

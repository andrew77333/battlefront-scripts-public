using UnityEngine;
using Game.Units;
using Game.Runtime;

namespace Game.Runtime
{
    /// <summary>
    /// Удаляет объект через Delay секунд после смерти юнита.
    /// Никаких зависимостей от анимации.
    /// Можно ставить на любые префабы с Unit.
    /// </summary>
    [RequireComponent(typeof(Unit))]
    public class DestroyAfterDeath : MonoBehaviour
    {
        [Tooltip("Сколько секунд держать труп перед удалением.")]
        public float Delay = 1f;

        private Unit _unit;
        private bool _subscribed;

        private void Awake()
        {
            _unit = GetComponent<Unit>();
        }

        private void Update()
        {
            // Unit.Runtime создаётся в Unit.Start — подождём и подпишемся один раз.
            if (!_subscribed && _unit != null && _unit.Runtime != null)
            {
                _unit.Runtime.Died += OnDied;
                _subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_subscribed && _unit != null && _unit.Runtime != null)
                _unit.Runtime.Died -= OnDied;
        }

        private void OnDied(UnitRuntimeStats stats)
        {
            // Даже если в Unit включено DestroyOnDeath — двойной Destroy безопасен:
            // более ранний таймер победит, лишний проигнорируется.
            Destroy(gameObject, Mathf.Max(0f, Delay));
        }
    }
}

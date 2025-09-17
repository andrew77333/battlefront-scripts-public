using UnityEngine;
using Game.Runtime; // для UnitRegistry

namespace Game.Units
{
    public enum TeamId { A = 0, B = 1 }

    /// <summary>
    /// Маркер принадлежности юнита к команде..
    /// </summary>
    [RequireComponent(typeof(Unit))]
    public class UnitTeam : MonoBehaviour
    {
        [Tooltip("Команда этого юнита.")]
        public TeamId Team = TeamId.A;

        /// <summary>Кеш ссылки на Unit (удобно для доступа).</summary>
        public Unit Unit { get; private set; }

        private void OnEnable() => UnitRegistry.Add(this); // для UnitRegistry
        private void OnDisable() => UnitRegistry.Remove(this); // для UnitRegistry


        private void Awake()
        {
            Unit = GetComponent<Unit>();
        }
    }
}

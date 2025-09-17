// Assets/Game/Runtime/BattleSpawner.cs
using UnityEngine;
using Game.Units;

namespace Game.Runtime
{
    /// <summary>
    /// Простейший спавнер для массовой расстановки двух армий в виде сетки.
    /// Никаких зависимостей от NavMesh/AI — только позиционирование и назначение команды.
    ///
    /// Примечания:
    /// - Если на префабе нет UnitTeam, компонент будет добавлен автоматически и команда назначена.
    /// - Родители (FriendlyRoot/EnemyRoot) удобны, чтобы не захламлять сцену (опционально).
    /// - Центры построений задаются трансформами (удобно двигать в сцене).
    /// </summary>
    public class BattleSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Префаб союзника (команда A). Должен содержать Unit. UnitTeam добавим при необходимости.")]
        public GameObject friendlyPrefab;

        [Tooltip("Префаб врага (команда B). Должен содержать Unit. UnitTeam добавим при необходимости.")]
        public GameObject enemyPrefab;

        [Header("Grid (Friendly)")]
        [Min(1)] public int friendlyRows = 8;
        [Min(1)] public int friendlyCols = 8;

        [Header("Grid (Enemy)")]
        [Min(1)] public int enemyRows = 8;
        [Min(1)] public int enemyCols = 8;

        [Header("Layout")]
        [Tooltip("Расстояние между юнитами в метрах.")]
        [Min(0.1f)] public float spacing = 1.6f;

        [Tooltip("Мировой якорь центра построения союзников (слева, условно).")]
        public Transform friendlyCenter;

        [Tooltip("Мировой якорь центра построения врагов (справа, условно).")]
        public Transform enemyCenter;

        [Header("Parent Containers (optional)")]
        [Tooltip("Куда сваливать спавн союзников. Если null — будут детьми этого объекта.")]
        public Transform friendlyRoot;

        [Tooltip("Куда сваливать спавн врагов. Если null — будут детьми этого объекта.")]
        public Transform enemyRoot;

        [Header("Runtime")]
        [Tooltip("Если включено — пересоздавать армию при старте Play.")]
        public bool autoSpawnOnPlay = true;

        [Tooltip("Очищать предыдущих детей-потомков перед спавном.")]
        public bool clearBeforeSpawn = true;

        private void Start()
        {
            if (autoSpawnOnPlay)
                ClearAndSpawn();
        }

        /// <summary>Быстрый запуск из контекстного меню компонента.</summary>
        [ContextMenu("Clear & Spawn Now")]
        public void ClearAndSpawn()
        {
            if (clearBeforeSpawn)
            {
                if (friendlyRoot == null) friendlyRoot = this.transform;
                if (enemyRoot == null) enemyRoot = this.transform;

                ClearChildren(friendlyRoot);
                ClearChildren(enemyRoot);
            }

            if (friendlyPrefab != null && friendlyCenter != null)
                SpawnGrid(friendlyPrefab, TeamId.A, friendlyRows, friendlyCols, friendlyCenter.position, friendlyRoot ?? this.transform);

            if (enemyPrefab != null && enemyCenter != null)
                SpawnGrid(enemyPrefab, TeamId.B, enemyRows, enemyCols, enemyCenter.position, enemyRoot ?? this.transform);
        }

        /// <summary>Удаляет всех детей у родителя (только верхний уровень).</summary>
        private void ClearChildren(Transform parent)
        {
            // В редакторе корректнее использовать DestroyImmediate, но в рантайме — Destroy.
#if UNITY_EDITOR
            // Если в playMode — используем Destroy, иначе DestroyImmediate
            if (Application.isPlaying)
            {
                for (int i = parent.childCount - 1; i >= 0; i--)
                    Destroy(parent.GetChild(i).gameObject);
            }
            else
            {
                for (int i = parent.childCount - 1; i >= 0; i--)
                    DestroyImmediate(parent.GetChild(i).gameObject);
            }
#else
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
#endif
        }

        /// <summary>
        /// Спавнит сетку rows×cols вокруг заданного центра.
        /// Центрируем сетку так, чтобы якорь был в её геометрическом центре.
        /// </summary>
        private void SpawnGrid(GameObject prefab, TeamId team, int rows, int cols, Vector3 center, Transform parent)
        {
            if (rows <= 0 || cols <= 0) return;

            // Полуширина/полувысота для центрирования относительно центральной точки
            float width = (cols - 1) * spacing;
            float height = (rows - 1) * spacing;

            Vector3 origin = center - new Vector3(width * 0.5f, 0f, height * 0.5f);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Vector3 pos = origin + new Vector3(c * spacing, 0f, r * spacing);
                    Quaternion rot = Quaternion.identity;

                    var go = Instantiate(prefab, pos, rot, parent);

                    // Обеспечим наличие UnitTeam и назначим сторону
                    if (!go.TryGetComponent<UnitTeam>(out var ut))
                        ut = go.AddComponent<UnitTeam>();
                    ut.Team = team;

                    // На всякий случай — разместим на поверхности, если под ногами есть коллайдер
                    SnapToGroundIfPossible(go);
                }
            }
        }

        /// <summary>
        /// Опционально «прижать» юнит к земле (Raycast вниз на 5м).
        /// Полезно на неровном террейне.
        /// </summary>
        private void SnapToGroundIfPossible(GameObject go)
        {
            Vector3 start = go.transform.position + Vector3.up * 5f;
            if (Physics.Raycast(start, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                go.transform.position = hit.point;
            }
        }

        // Наглядные гизмо-иконки центров
        private void OnDrawGizmos()
        {
            if (friendlyCenter != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(friendlyCenter.position, 0.25f);
            }
            if (enemyCenter != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(enemyCenter.position, 0.25f);
            }
        }
    }
}

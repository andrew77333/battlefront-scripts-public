using UnityEngine;
using Game.Units;

namespace Game.Runtime
{
    /// <summary>
    /// Аккуратно "прижимает" объект к земле при старте (луч вниз),
    /// игнорируя капсулы персонажей. По желанию — лёгкий сдвиг, если
    /// в точке спавна чьи-то ноги.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GroundSnapOnStart : MonoBehaviour
    {
        [Header("Raycast")]
        [Tooltip("С какой высоты пускать луч вниз.")]
        public float RayHeight = 10f;

        [Tooltip("Макс. глубина луча вниз.")]
        public float MaxDownDistance = 50f;

        [Tooltip("Какие слои считаем землёй (оставь по умолчанию, если не используешь слоя).")]
        public LayerMask GroundMask = ~0; // все слои

        [Tooltip("Игнорировать коллайдеры персонажей (CharacterController/Unit/UnitTeam) при выборе точки земли.")]
        public bool IgnoreCharacters = true;

        [Header("Nudge (опционально)")]
        [Tooltip("Лёгкий сдвиг, если стоим вплотную к другим персонажам.")]
        public bool NudgeIfOverlapping = true;

        [Tooltip("Радиус проверки на тесный контакт.")]
        public float NudgeCheckRadius = 0.35f;

        [Tooltip("Величина сдвига, если тесно.")]
        public float NudgeDistance = 0.4f;

        private void Start()
        {
            SnapNow();
        }

        public void SnapNow()
        {
            var pos = transform.position;
            Vector3 start = pos + Vector3.up * Mathf.Max(0.1f, RayHeight);

            if (TryFindGround(start, out var hitPoint))
            {
                transform.position = hitPoint;

                if (NudgeIfOverlapping)
                    TryNudgeFromCharacters();
            }
        }

        private bool TryFindGround(Vector3 start, out Vector3 point)
        {
            point = transform.position;

            // Берём все пересечения и идём сверху вниз, пропуская персонажей
            var hits = Physics.RaycastAll(start, Vector3.down, MaxDownDistance, GroundMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (!IgnoreCharacters)
                {
                    point = h.point;
                    return true;
                }

                // пропускаем коллайдеры персонажей
                if (IsCharacterCollider(h.collider)) continue;

                point = h.point;
                return true;
            }
            // если все попали в персонажей — возьмём самый верхний хит
            point = hits[0].point;
            return true;
        }

        private void TryNudgeFromCharacters()
        {
            // Лёгкое отталкивание по XZ, если стоим слишком близко к другим персонажам
            var overlaps = Physics.OverlapSphere(transform.position + Vector3.up * 0.1f, NudgeCheckRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var col in overlaps)
            {
                if (col == null || col.attachedRigidbody == null) continue; // быстрее отсекает мусор
                if (!IsCharacterCollider(col)) continue;

                Vector3 dir = (transform.position - col.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitSphere; // на всякий случай
                dir.y = 0f;
                dir.Normalize();

                transform.position += dir * NudgeDistance;
                break; // достаточно одного лёгкого сдвига
            }
        }

        private static bool IsCharacterCollider(Collider col)
        {
            if (col == null) return false;
            // ищем контроллеры/наши компоненты выше по иерархии
            if (col.GetComponentInParent<CharacterController>() != null) return true;
            if (col.GetComponentInParent<Unit>() != null) return true;
            if (col.GetComponentInParent<UnitTeam>() != null) return true;
            return false;
        }
    }
}

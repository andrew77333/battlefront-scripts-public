// CombatAgent.Grounding.cs
// ЧАСТЬ класса CombatAgent: мягкое «прилипание» к земле и фильтрация коллайдеров.

// CombatAgent.Grounding.cs
// ЧАСТЬ класса CombatAgent: прилипание к земле и авто-тюнинг CharacterController.

using UnityEngine;
using Game.Units;

public partial class CombatAgent : MonoBehaviour
{
    // =========================
    // ПРИЛИПАНИЕ К ЗЕМЛЕ (анти-бег по головам)
    // =========================
    [Header("Ground Stick")]
    [SerializeField] private bool StickToGround = true;
    [SerializeField] private float GroundSnapMax = 0.6f;   // макс. опускание за кадр
    [SerializeField] private float GroundSnapSpeed = 20f;   // скорость опускания
    [SerializeField] private float GroundRayHeight = 1.2f;  // высота старта луча
    [SerializeField] private LayerMask GroundMask = ~0;     // слои, которые считаем землёй

    // === Controller Tuning (anti-step-on-heads) ===
    [Header("Controller Tuning")]
    [SerializeField] private bool EnforceControllerTuning = true; // авто-ограничения CharacterController
    [SerializeField] private float MaxStepOffset = 0.1f; // лимит ступеньки (анти «по головам»)
    [SerializeField] private float SlopeLimit = 35f;  // максимально «взбираемый» угол
    [SerializeField] private float DesiredSkinWidth = 0.03f;// минимальный skinWidth

    // Буфер для луча вниз
    private readonly RaycastHit[] _rayHits = new RaycastHit[8];

    // Вызывается из Awake ядра
    private void GroundingAwakeSetup()
    {
        if (!EnforceControllerTuning || _cc == null) return;

        // не позволяем «шагать» выше порога
        _cc.stepOffset = Mathf.Min(_cc.stepOffset, MaxStepOffset);
        // ограничим «скалолазание»
        if (_cc.slopeLimit > SlopeLimit) _cc.slopeLimit = SlopeLimit;
        // skinWidth слишком большой иногда даёт «липкие уступы»
        _cc.skinWidth = Mathf.Clamp(_cc.skinWidth, 0.01f, DesiredSkinWidth);
    }

    // Мягко прижимаем к земле: луч вниз игнорирует коллайдеры персонажей.
    // Перемещение только через CharacterController.Move (без телепортов), чтобы не ломать физику.
    private void StickToGroundIfNeeded()
    {
        if (!StickToGround || _cc == null) return;

        Vector3 start = transform.position + Vector3.up * Mathf.Max(0.1f, GroundRayHeight);
        float maxDist = GroundRayHeight + Mathf.Max(0.05f, GroundSnapMax);

        int hitCount = Physics.RaycastNonAlloc(
            start, Vector3.down, _rayHits, maxDist, GroundMask, QueryTriggerInteraction.Ignore);

        if (hitCount <= 0) return;

        // Ищем ближайший НЕ-персонажный коллайдер под нами
        float bestDist = float.MaxValue;
        Vector3 bestPoint = Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            var h = _rayHits[i];
            if (IsCharacterCollider(h.collider)) continue; // пропускаем головы/тела юнитов
            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                bestPoint = h.point;
            }
        }

        if (bestDist == float.MaxValue) return; // под нами только персонажи — ничего не делаем

        float deltaY = transform.position.y - bestPoint.y;
        if (deltaY > 0.01f)
        {
            // Аккуратно опускаем контроллер, чтобы не "проваливаться"
            float step = Mathf.Min(deltaY, GroundSnapSpeed * Time.deltaTime);
            _cc.Move(new Vector3(0f, -step, 0f));
        }
    }

    // Узнаём, относится ли коллайдер к персонажу
    private static bool IsCharacterCollider(Collider col)
    {
        if (col == null) return false;
        if (col.GetComponentInParent<CharacterController>() != null) return true;
        if (col.GetComponentInParent<Unit>() != null) return true;
        if (col.GetComponentInParent<UnitTeam>() != null) return true;
        return false;
    }
}

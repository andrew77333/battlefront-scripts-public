// CombatAgent.Animation.cs
// ЧАСТЬ класса CombatAgent: переменные для анимации движения.

// CombatAgent.Animation.cs
// ЧАСТЬ класса CombatAgent: переменные и безопасные вызовы анимации.

using UnityEngine;

public partial class CombatAgent : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float _blendSpeed = 6f; // скорость сглаживания BlendTree
    private float _moveBlend;                        // текущее (сглаженное) значение BlendTree 0..1

    /// <summary>
    /// Плавно подтягивает параметр BlendTree к целевому значению и отправляет его в аниматор.
    /// Вызывать каждый кадр с целевым значением.
    /// </summary>
    private void SetMoveBlendSafe(float target)
    {
        target = Mathf.Clamp01(target);
        _moveBlend = Mathf.MoveTowards(_moveBlend, target, Time.deltaTime * _blendSpeed);
        _uAnim?.SetMoveBlend(_moveBlend);
    }

    /// <summary>Безопасно проигрывает атаку, если аниматор присутствует.</summary>
    private void PlayAttackSafe() => _uAnim?.PlayAttack();

    /// <summary>Безопасно проигрывает победную анимацию.</summary>
    private void PlayVictorySafe() => _uAnim?.PlayVictory();
}


using System.Collections.Generic;
using UnityEngine;
using Game.Runtime; // UnitRuntimeStats

//Единая точка входа для урона — DamageSystem.Apply(...)/Heal(...) (это и есть наш простой damage-resolver v1).
//Учитывает: Evasion(уклонение цели), Block(частичный блок), броню / маг.резист с penetration от атакующего, крит, спавнит попапы, шлёт события OnApplied/OnHealed.

/// <summary>
/// Очередь урона/лечения с применением в конце кадра (LateUpdate).
/// Главная цель: строгий порядок "сначала урон -> потом хил"
/// + удобная точка расширения (щиты/ауры/повторы/сервер и т.д.).
/// </summary>
public class DamageResolver : MonoBehaviour
{
    public static DamageResolver Instance { get; private set; }
    public static bool ExistsAndEnabled =>
        Instance != null && Instance.enabled && Instance.UseQueue;

    [Header("Queue")]
    [Tooltip("Если включено — DamageSystem будет накапливать заявки и применять их в конце кадра.")]
    public bool UseQueue = true;

    [Tooltip("Применять урон перед лечением (рекомендуется).")]
    public bool DamageThenHeal = true;

    [Tooltip("Стартовая вместимость списков (для уменьшения аллокаций).")]
    public int InitialCapacity = 256;

    // ----- Форматы заявок -----
    public struct DamageRequest
    {
        public UnitRuntimeStats target;
        public float amount;
        public bool isCrit;
        public Vector3 popupPos;
        public UnitRuntimeStats attacker;
        public DamageType type;
    }

    public struct HealRequest
    {
        public UnitRuntimeStats target;
        public float amount;
        public Vector3 popupPos;
        public UnitRuntimeStats healer;
    }

    // ----- Очереди -----
    private readonly List<DamageRequest> _damage = new();
    private readonly List<HealRequest> _heals = new();

    public void EnqueueDamage(DamageRequest r) => _damage.Add(r);
    public void EnqueueHeal(HealRequest r) => _heals.Add(r);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Second DamageResolver ignored.");
            enabled = false;
            return;
        }
        Instance = this;

        if (_damage.Capacity < InitialCapacity) _damage.Capacity = InitialCapacity;
        if (_heals.Capacity < InitialCapacity) _heals.Capacity = InitialCapacity;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        _damage.Clear();
        _heals.Clear();
    }

    private void LateUpdate()
    {
        if (!UseQueue)
        {
            _damage.Clear();
            _heals.Clear();
            return;
        }

        // 1) Урон
        if (DamageThenHeal)
        {
            FlushDamage();
            FlushHeal();
        }
        else
        {
            FlushHeal();
            FlushDamage();
        }
    }

    private void FlushDamage()
    {
        if (_damage.Count == 0) return;

        // Важно: применяем в порядке добавления (стабильность).
        for (int i = 0; i < _damage.Count; i++)
        {
            var r = _damage[i];
            // Мог уже умереть к моменту применения — DamageSystem сам проверит.
            DamageSystem.ApplyImmediate(r.target, r.amount, r.isCrit, r.popupPos, r.attacker, r.type);
        }
        _damage.Clear();
    }

    private void FlushHeal()
    {
        if (_heals.Count == 0) return;

        for (int i = 0; i < _heals.Count; i++)
        {
            var r = _heals[i];
            DamageSystem.HealImmediate(r.target, r.amount, r.popupPos, r.healer);
        }
        _heals.Clear();
    }
}

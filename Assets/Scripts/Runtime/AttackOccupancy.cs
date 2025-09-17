// Assets/Game/Runtime/AttackOccupancy.cs
using System.Collections.Generic;
using Game.Units;

namespace Game.Runtime
{
    /// <summary>
    /// Мини-реестр занятости цели атакующими.
    /// - Хранит счётчик "сколько меня уже бьют".
    /// - Никакой логики выбора цели — только учёт.
    /// </summary>
    public static class AttackOccupancy
    {
        private static readonly Dictionary<UnitTeam, int> _map = new Dictionary<UnitTeam, int>(128);

        /// <summary>Текущее число атакующих у цели.</summary>
        public static int GetCount(UnitTeam target)
        {
            if (target == null) return 0;
            return _map.TryGetValue(target, out var c) ? c : 0;
        }

        /// <summary>Занять слот у цели (инкремент счётчика).</summary>
        public static void Acquire(UnitTeam target)
        {
            if (target == null) return;
            if (_map.TryGetValue(target, out var c)) _map[target] = c + 1;
            else _map[target] = 1;
        }

        /// <summary>Освободить слот у цели (декремент/удаление).</summary>
        public static void Release(UnitTeam target)
        {
            if (target == null) return;
            if (_map.TryGetValue(target, out var c))
            {
                c--;
                if (c <= 0) _map.Remove(target);
                else _map[target] = c;
            }
        }

        /// <summary>Очистить записи по мёртвым целям (опционально вызывать из редких мест).</summary>
        public static void ClearDead()
        {
            var toRemove = new List<UnitTeam>();
            foreach (var kv in _map)
            {
                var t = kv.Key;
                if (t == null || t.Unit == null || t.Unit.Runtime == null || !t.Unit.Runtime.IsAlive())
                    toRemove.Add(t);
            }
            foreach (var k in toRemove) _map.Remove(k);
        }
    }
}

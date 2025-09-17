using Game.Runtime;
using Game.Units;
using System.Collections.Generic;

//Что делает «реестр» и зачем он нужен
//Реестр(UnitRegistry) просто хранит живые ссылки на юниты по командам и обновляется,
//когда юнит включается/выключается (OnEnable/OnDisable). Тогда агент вместо «дорогого» шага (1) делает:
//взять готовый список противников: var opponents = UnitRegistry.GetOpponents(_team.Team);
//дальше всё то же самое: фильтр по живости и расчёт score → выбрать лучшую по score.
//Что именно даёт прирост производительности
//Мы убираем частые вызовы FindObjectsByType у каждого агента каждый кадр — это одна из самых дорогих операций (и с GC-аллокациями).
//Вместо этого берём уже готовый список противников (обычный List<UnitTeam>), что в разы дешевле.
//На небольших толпах разница не бросается в глаза, но на 100+ юнитов фреймтайм заметно стабилизируется.
//Важные детали и крайние случаи
//Реестр не «удаляет мёртвых» — но мы всё равно проверяем Runtime.IsAlive() в цикле. Когда объект реально уничтожится (или UnitTeam отключится), он автоматически выпишется из реестра через OnDisable().
//Если когда-то появятся >2 команд, GetOpponents вернёт всех, кто не твоя команда — логика выбора останется прежней.

namespace Game.Runtime
{
    /// <summary>Лёгкий реестр активных UnitTeam по командам.</summary>
    public static class UnitRegistry
    {
        private static readonly List<UnitTeam>[] _teams;

        static UnitRegistry()
        {
            int count = System.Enum.GetValues(typeof(TeamId)).Length;
            _teams = new List<UnitTeam>[count];
            for (int i = 0; i < count; i++) _teams[i] = new List<UnitTeam>(128);
        }

        public static void Add(UnitTeam u)
        {
            if (u == null) return;
            int idx = (int)u.Team;
            var list = _teams[idx];
            if (!list.Contains(u)) list.Add(u);
        }

        public static void Remove(UnitTeam u)
        {
            if (u == null) return;
            // На случай смены Team — вычистим из всех списков
            for (int i = 0; i < _teams.Length; i++) _teams[i].Remove(u);
        }

        public static IReadOnlyList<UnitTeam> GetAllies(TeamId team) => _teams[(int)team];

        public static IReadOnlyList<UnitTeam> GetOpponents(TeamId team)
        {
            if (_teams.Length == 2)
            {
                int idx = (int)team == 0 ? 1 : 0;
                return _teams[idx];
            }
            _buffer.Clear();
            for (int i = 0; i < _teams.Length; i++)
                if (i != (int)team) _buffer.AddRange(_teams[i]);
            return _buffer;
        }

        private static readonly List<UnitTeam> _buffer = new List<UnitTeam>(256);
    }
}

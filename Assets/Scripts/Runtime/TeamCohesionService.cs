using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Units;

//быстрый кэш центроидов команд (чтобы агенты не искали союзников сами).
//Сервис сам создастся в рантайме. При желании можешь поставить UpdateEveryFrames = 2–3,
//чтобы ещё сильнее разгрузить CPU на толпах (данные чуть менее «живые», но для центроида это ок).

namespace Game.Runtime
{
    /// <summary>
    /// Сервис кэширует центроиды и число живых союзников по командам.
    /// Автосоздаётся при запуске (не нужно вручную класть на сцену).
    /// </summary>
    public class TeamCohesionService : MonoBehaviour
    {
        [Tooltip("Как часто обновлять кэш (в кадрах). 1 = каждый кадр.")]
        public int UpdateEveryFrames = 1;

        private static TeamCohesionService _instance;
        private static TeamData[] _data; // по индексам TeamId

        [Serializable]
        public struct TeamData
        {
            public Vector3 Centroid;
            public int Count;
            public int UpdatedFrame;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("[TeamCohesionService]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<TeamCohesionService>();

            int teamCount = Enum.GetValues(typeof(TeamId)).Length;
            _data = new TeamData[teamCount];
        }

        private void Update()
        {
            if (_data == null) return;
            if (UpdateEveryFrames < 1) UpdateEveryFrames = 1;
            if (Time.frameCount % UpdateEveryFrames != 0) return;

            int teamCount = _data.Length;
            for (int ti = 0; ti < teamCount; ti++)
            {
                var list = UnitRegistry.GetAllies((TeamId)ti);
                Vector3 sum = Vector3.zero;
                int cnt = 0;

                // считаем только живых
                for (int i = 0; i < list.Count; i++)
                {
                    var ut = list[i];
                    if (ut == null || ut.Unit == null || ut.Unit.Runtime == null) continue;
                    if (!ut.Unit.Runtime.IsAlive()) continue;
                    sum += ut.transform.position;
                    cnt++;
                }

                _data[ti] = new TeamData
                {
                    Centroid = (cnt > 0) ? (sum / cnt) : Vector3.zero,
                    Count = cnt,
                    UpdatedFrame = Time.frameCount
                };
            }
        }

        /// <summary>Получить кэшированные данные по команде.</summary>
        public static TeamData Get(TeamId team)
        {
            if (_data == null) return default;
            return _data[(int)team];
        }
    }
}

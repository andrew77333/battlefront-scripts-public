// Scripts/AI/SteeringLOD.cs
// Небольшой «кэш с таймером» для рулящих векторов (separation/bypass/cohesion).
// Не MonoBehaviour. Хранит кэш и решает «пора ли пересчитать».
// Боевой код сам решает КАК считать (мы только храним результат и расписание).
using UnityEngine;

namespace Game.AI
{
    /// <summary>
    /// Кэш рулевых векторов + планировщик пересчёта (LOD).
    /// Идея: дорогие Physics-операции считаем не каждый кадр, а с интервалом.
    /// </summary>
    public class SteeringLOD
    {
        // ===== Интервалы пересчёта =====
        // «Виден камерой»
        public float VisibleIntervalMin = 0.12f;  // нижняя граница джиттера
        public float VisibleIntervalMax = 0.18f;  // верхняя граница джиттера
        // «Не виден камерой» (будем подключать позже; пока можно не использовать)
        public float HiddenIntervalMin = 0.30f;
        public float HiddenIntervalMax = 0.40f;

        // Следующий момент времени, когда «пора пересчитать»
        private float _nextAt;

        // ===== Кэш значений (то, что реально потребляет агент между пересчётами) =====
        public Vector3 Separation;   // суммарный вектор «разлипалки»
        public Vector3 Bypass;       // боковой обход «впереди идущего»
        public Vector3 CohesionDir;  // нормализованный вектор к центроиду своих
        public int CohesionCount;    // сколько союзников попало в расчёт
        public float CohesionDist;   // расстояние до центроида (для «изоляции»)

        /// <summary>Принудительно сделать «пересчитать прямо сейчас».</summary>
        public void ForceNow(float currentTime) => _nextAt = currentTime;

        /// <summary>Нужно ли пересчитывать на этом кадре?</summary>
        public bool Due(float currentTime) => currentTime >= _nextAt;

        /// <summary>
        /// Назначить следующий пересчёт, с джиттером в заданном диапазоне.
        /// </summary>
        public void ScheduleNext(float currentTime, bool isVisible = true)
        {
            float min = isVisible ? VisibleIntervalMin : HiddenIntervalMin;
            float max = isVisible ? VisibleIntervalMax : HiddenIntervalMax;
            // Небольшой разброс, чтобы агенты не дергались синхронно
            float dt = Random.Range(min, max);
            _nextAt = currentTime + Mathf.Max(0.01f, dt);
        }

        // ===== Удобные сеттеры для кэша (чисто для читаемости) =====
        public void SetSeparation(in Vector3 sep) => Separation = sep;
        public void SetBypass(in Vector3 byp) => Bypass = byp;

        public void SetCohesion(in Vector3 dir, int count, float dist)
        {
            CohesionDir = dir;
            CohesionCount = count;
            CohesionDist = dist;
        }
    }
}

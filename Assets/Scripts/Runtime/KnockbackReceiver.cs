using System.Collections;
using UnityEngine;
using Game.Units;

namespace Game.Runtime
{
    /// <summary>
    /// Принимает толчок (knockback) и короткий оглушающий "knockdown".
    /// Во время оглушения отключает CombatAgent и двигает персонажа сам.
    /// </summary>
    [RequireComponent(typeof(Unit))]
    [RequireComponent(typeof(CharacterController))]
    public class KnockbackReceiver : MonoBehaviour
    {
        [Header("Motion")]
        [Tooltip("Макс. горизонтальная скорость от толчка, м/с.")]
        public float MaxSpeed = 6f;

        [Tooltip("Затухание импульса (чем больше — тем быстрее тормозит).")]
        public float Drag = 4f;

        [Tooltip("Гравитация во время нокдауна (м/с^2). 0 — без вертикали.")]
        public float Gravity = 0f;

        [Header("Stars (визуально)")]
        public bool ShowStars = true;
        [Tooltip("Как часто спавнить звёздочки над головой, сек.")]
        public float StarsEvery = 0.25f;
        [Tooltip("Смещение по высоте для звёздочек.")]
        public float StarsYOffset = 2.2f;
        public Color StarsColor = new Color(1f, 0.85f, 0.2f);

        private CharacterController _cc;
        private Unit _unit;
        private CombatAgent _ai;
        private bool _aiDisabledByMe;
        private Vector3 _vel;         // внешняя скорость (XZ + опц. Y)
        private float _stunUntil;     // время конца оглушения
        private Coroutine _starsCo;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _unit = GetComponent<Unit>();
            TryGetComponent(out _ai);
        }

        private void OnDisable()
        {
            StopStars();
        }

        private void Update()
        {
            if (Time.time < _stunUntil)
            {
                // держим AI выключенным, если мы его выключали
                if (_ai != null && !_aiDisabledByMe && _ai.enabled)
                {
                    _ai.enabled = false;
                    _aiDisabledByMe = true;
                }

                // движение от импульса
                Vector3 move = _vel * Time.deltaTime;
                if (Gravity != 0f) _vel.y += Gravity * Time.deltaTime;
                _cc.Move(move);

                // затухание XZ
                Vector3 horiz = new Vector3(_vel.x, 0f, _vel.z);
                horiz = Vector3.Lerp(horiz, Vector3.zero, 1f - Mathf.Exp(-Drag * Time.deltaTime));
                _vel = new Vector3(horiz.x, _vel.y, horiz.z);
            }
            else
            {
                // окончание оглушения: возвращаем AI, если мы его отключали
                if (_ai != null && _aiDisabledByMe)
                {
                    _ai.enabled = true;
                    _aiDisabledByMe = false;
                }
            }
        }

        /// <summary>
        /// Применить толчок и оглушение.
        /// dir — мировое направление (будет нормализовано по XZ), force — м/с, stunSec — длительность оглушения.
        /// </summary>
        public void ApplyKnockback(Vector3 dir, float force, float stunSec, bool spawnStars = true)
        {
            if (_unit != null && _unit.Runtime != null && !_unit.Runtime.IsAlive()) return;

            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward * -1f; // fallback
            dir.Normalize();

            Vector3 addVel = dir * Mathf.Max(0f, force);
            Vector3 newHoriz = new Vector3(_vel.x, 0f, _vel.z) + addVel;
            float mag = newHoriz.magnitude;
            if (mag > MaxSpeed) newHoriz = newHoriz * (MaxSpeed / Mathf.Max(0.0001f, mag));
            _vel = new Vector3(newHoriz.x, _vel.y, newHoriz.z);

            _stunUntil = Mathf.Max(_stunUntil, Time.time + Mathf.Max(0f, stunSec));

            if (ShowStars && spawnStars)
            {
                StopStars();
                _starsCo = StartCoroutine(StarsRoutine(stunSec));
            }
        }

        private IEnumerator StarsRoutine(float duration)
        {
            float end = Time.time + duration;
            while (Time.time < end)
            {
                // Используем DamagePopup как простую «звёздочку»
                Vector3 pos = transform.position + Vector3.up * StarsYOffset;
                DamagePopup.Spawn(pos, "★", StarsColor);
                yield return new WaitForSeconds(StarsEvery);
            }
            _starsCo = null;
        }

        private void StopStars()
        {
            if (_starsCo != null)
            {
                StopCoroutine(_starsCo);
                _starsCo = null;
            }
        }
    }
}

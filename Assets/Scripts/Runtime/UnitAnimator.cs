using UnityEngine;
using Game.Data;
using Game.Units;

namespace Game.Runtime
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Unit))]
    public class UnitAnimator : MonoBehaviour
    {
        private Animator _anim;
        private Unit _unit;

        [Header("Profile")]
        public UnitAnimationProfile Profile;

        private static readonly int HashMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int HashAttack = Animator.StringToHash("Attack");
        private static readonly int HashAttackIndex = Animator.StringToHash("AttackIndex");
        // ВАЖНО: имя как в контроллере!
        private static readonly int HashDeath = Animator.StringToHash("Death");
        private static readonly int HashDeathIndex = Animator.StringToHash("DeathIndex");
        private static readonly int HashVictory = Animator.StringToHash("Victory");
        private static readonly int HashVictoryIndex = Animator.StringToHash("VictoryIndex");

        private bool _warnedNoController;
        private bool AnimatorReady =>
            _anim != null && _anim.isActiveAndEnabled && _anim.runtimeAnimatorController != null;

        private float _moveBlendCurrent; // накопитель для сглаживания MoveSpeed

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            _unit = GetComponent<Unit>();

            if (Profile == null)
            {
                Profile = ScriptableObject.CreateInstance<UnitAnimationProfile>();
                Profile.name = "TempProfile_Runtime";
                Profile.IdleVariants = 1;
                Profile.DeathVariants = 1;
                Profile.VictoryVariants = 1;
                Profile.AttackVariants = new UnitAnimationProfile.AttackEntry[]
                {
                    new UnitAnimationProfile.AttackEntry { UnlockLevel = 1, DamageMultiplier = 1f, FireTimeNormalized = 0.35f }
                };
            }

            WarnIfNoController();
        }

        private void OnEnable() => WarnIfNoController();

        private void WarnIfNoController()
        {
            if (!_warnedNoController && (_anim == null || _anim.runtimeAnimatorController == null))
            {
                _warnedNoController = true;
                Debug.LogWarning($"{name}: Animator has no controller. Assign BaseHumanoidController (or an override) on the prefab.", this);
            }
        }

        //public void SetMoveBlend(float normalizedSpeed)
        //{
        //    if (!AnimatorReady) { WarnIfNoController(); return; }
        //    _anim.SetFloat(HashMoveSpeed, Mathf.Max(0f, normalizedSpeed));
        //}
        public void SetMoveBlend(float value)
        {
            if (!AnimatorReady) { WarnIfNoController(); return; }

            // Наш Blend Tree настроен на [0..2] (0 idle, 1 walk, 2 run)
            float target = Mathf.Clamp(value, 0f, 2f);

            // лёгкое сглаживание, чтобы не дёргалось
            _moveBlendCurrent = Mathf.Lerp(_moveBlendCurrent, target, Time.deltaTime * 10f);
            _anim.SetFloat(HashMoveSpeed, _moveBlendCurrent);
        }


        public void PlayAttack(int currentLevel = 1)
        {
            if (!AnimatorReady || Profile == null) { WarnIfNoController(); return; }

            int available = GetAvailableAttackCount(currentLevel);
            int index = (available > 0) ? Random.Range(0, available) : 0;

            _anim.SetInteger(HashAttackIndex, index);
            _anim.SetTrigger(HashAttack);
        }

        public void SetRandomDeathIndex()
        {
            if (!AnimatorReady || Profile == null) { WarnIfNoController(); return; }
            int n = Mathf.Max(1, Profile.DeathVariants);
            int index = Random.Range(0, n);
            _anim.SetInteger(HashDeathIndex, index);
        }

        public void PlayDeath()
        {
            if (!AnimatorReady) { WarnIfNoController(); return; }
            _anim.ResetTrigger(HashAttack);   // на всякий случай
            _anim.SetTrigger(HashDeath);
        }

        public void PlayVictory()
        {
            if (!AnimatorReady || Profile == null) { WarnIfNoController(); return; }
            int n = Mathf.Max(1, Profile.VictoryVariants);
            int index = Random.Range(0, n);
            _anim.SetInteger(HashVictoryIndex, index);
            _anim.SetTrigger(HashVictory);
        }

        private int GetAvailableAttackCount(int currentLevel)
        {
            if (Profile.AttackVariants == null || Profile.AttackVariants.Length == 0)
                return 1;
            return Mathf.Max(1, Profile.AttackVariants.Length);
        }
    }
}


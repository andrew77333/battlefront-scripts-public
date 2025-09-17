using UnityEngine;

namespace Game.Anim
{
    /// <summary>
    /// Единый справочник имён и hash'ей параметров Animator.
    /// Никаких "сырьевых" строк в коде!
    /// </summary>
    public static class AnimParams
    {
        public static class Floats
        {
            public const string MoveSpeed = "MoveSpeed";
            public static readonly int MoveSpeedId = Animator.StringToHash(MoveSpeed);
        }

        public static class Triggers
        {
            public const string Attack = "Attack";
            public const string Death = "Death";
            public const string Victory = "Victory";

            public static readonly int AttackId = Animator.StringToHash(Attack);
            public static readonly int DeathId = Animator.StringToHash(Death);
            public static readonly int VictoryId = Animator.StringToHash(Victory);
        }

        public static class Ints
        {
            public const string AttackIndex = "AttackIndex";
            public const string DeathIndex = "DeathIndex";
            public const string VictoryIndex = "VictoryIndex";

            public static readonly int AttackIndexId = Animator.StringToHash(AttackIndex);
            public static readonly int DeathIndexId = Animator.StringToHash(DeathIndex);
            public static readonly int VictoryIndexId = Animator.StringToHash(VictoryIndex);
        }
    }
}

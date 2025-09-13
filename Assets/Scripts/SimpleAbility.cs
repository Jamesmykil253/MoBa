using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Simple ability types for MOBA gameplay
    /// </summary>
    public enum AbilityType
    {
        PrimaryAttack,
        Ability1,
        Ability2,
        Ultimate
    }

    /// <summary>
    /// Simple ability data - no complex patterns needed
    /// </summary>
    [CreateAssetMenu(fileName = "Ability", menuName = "MOBA/Ability")]
    public class SimpleAbility : ScriptableObject
    {
        [Header("Basic Info")]
        public string abilityName;
        public string description;
        public Sprite icon;
        public AbilityType abilityType;

        [Header("Combat")]
        public float damage = 50f;
        public DamageType damageType = DamageType.Physical;
        public float range = 5f;
        public float cooldown = 5f;
    }
}

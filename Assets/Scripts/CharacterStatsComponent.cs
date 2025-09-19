using UnityEngine;

namespace MOBA
{
    [DisallowMultipleComponent]
    public class CharacterStatsComponent : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats = CharacterStats.DefaultStats;

        public CharacterStats Stats
        {
            get => stats;
            set => stats = value;
        }
    }
}

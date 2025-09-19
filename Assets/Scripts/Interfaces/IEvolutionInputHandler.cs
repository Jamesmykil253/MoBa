using UnityEngine.InputSystem;

namespace MOBA.Abilities
{
    /// <summary>
    /// Interface for handling evolution selection input in our MOBA system
    /// </summary>
    public interface IEvolutionInputHandler
    {
        /// <summary>
        /// Handle first evolution path selection (typically mapped to "1" key)
        /// </summary>
        void OnEvolutionPath1();

        /// <summary>
        /// Handle second evolution path selection (typically mapped to "2" key)
        /// </summary>
        void OnEvolutionPath2();
    }
}
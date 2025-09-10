using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Command Pattern interface for encapsulating actions that can be executed and undone.
    /// Based on Game Programming Patterns by Robert Nystrom.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Executes the command
        /// </summary>
        void Execute();

        /// <summary>
        /// Undoes the command if possible
        /// </summary>
        void Undo();

        /// <summary>
        /// Checks if the command can be executed
        /// </summary>
        /// <returns>True if the command can be executed</returns>
        bool CanExecute();
    }
}
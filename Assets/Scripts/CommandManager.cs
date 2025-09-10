using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MOBA
{
    /// <summary>
    /// Manages command execution, undo, and redo operations
    /// Implements the Command Pattern for input handling
    /// </summary>
    public class CommandManager : MonoBehaviour
    {
        private Stack<ICommand> executedCommands = new Stack<ICommand>();
        private Stack<ICommand> undoneCommands = new Stack<ICommand>();

        private PlayerInput playerInput;
        private InputActionAsset inputActions;

        // Removed automatic Awake() method to prevent automatic loading
        // Use InitializeInputSystem() method manually instead
        public void InitializeInputSystem()
        {
            // Load the default Input Actions asset
            inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (inputActions == null)
            {
                // Try alternative asset names
                inputActions = Resources.Load<InputActionAsset>("InputActions");
                if (inputActions == null)
                {
                    Debug.LogWarning("InputSystem_Actions or InputActions asset not found in Resources. Input system may not work properly.");
                    return;
                }
            }

            playerInput = GetComponent<PlayerInput>();
            if (playerInput == null)
            {
                playerInput = gameObject.AddComponent<PlayerInput>();
            }

            if (inputActions != null)
            {
                playerInput.actions = inputActions;
            }
        }

        private void OnEnable()
        {
            if (playerInput != null)
            {
                // Note: Previous/Next actions don't exist in current InputSystem_Actions
                // Uncomment when these actions are added to the input asset
                /*
                playerInput.actions["Player/Previous"].performed += OnUndo;
                playerInput.actions["Player/Next"].performed += OnRedo;
                */
                Debug.Log("[CommandManager] Undo/Redo actions not available in current input asset");
            }
        }

        private void OnDisable()
        {
            if (playerInput != null)
            {
                // Note: Previous/Next actions don't exist in current InputSystem_Actions
                /*
                playerInput.actions["Player/Previous"].performed -= OnUndo;
                playerInput.actions["Player/Next"].performed -= OnRedo;
                */
            }
        }

        private void OnUndo(InputAction.CallbackContext context)
        {
            Undo();
        }

        private void OnRedo(InputAction.CallbackContext context)
        {
            Redo();
        }

        /// <summary>
        /// Executes a command and adds it to the executed stack
        /// </summary>
        public void ExecuteCommand(ICommand command)
        {
            if (command.CanExecute())
            {
                command.Execute();
                executedCommands.Push(command);
                undoneCommands.Clear(); // Clear redo stack when new command is executed

                Debug.Log($"Executed command: {command.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"Cannot execute command: {command.GetType().Name}");
            }
        }

        /// <summary>
        /// Undoes the last executed command
        /// </summary>
        public void Undo()
        {
            if (executedCommands.Count > 0)
            {
                ICommand command = executedCommands.Pop();
                command.Undo();
                undoneCommands.Push(command);

                Debug.Log($"Undid command: {command.GetType().Name}");
            }
            else
            {
                Debug.Log("No commands to undo");
            }
        }

        /// <summary>
        /// Redoes the last undone command
        /// </summary>
        public void Redo()
        {
            if (undoneCommands.Count > 0)
            {
                ICommand command = undoneCommands.Pop();
                command.Execute();
                executedCommands.Push(command);

                Debug.Log($"Redid command: {command.GetType().Name}");
            }
            else
            {
                Debug.Log("No commands to redo");
            }
        }

        /// <summary>
        /// Clears all command history
        /// </summary>
        public void ClearHistory()
        {
            executedCommands.Clear();
            undoneCommands.Clear();
        }

        /// <summary>
        /// Gets the number of commands that can be undone
        /// </summary>
        public int UndoCount => executedCommands.Count;

        /// <summary>
        /// Gets the number of commands that can be redone
        /// </summary>
        public int RedoCount => undoneCommands.Count;
    }
}
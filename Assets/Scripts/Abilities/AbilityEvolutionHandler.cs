using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using MOBA.Debugging;
using System.Collections.Generic;
using System;

namespace MOBA.Abilities
{
    /// <summary>
    /// Pokemon Unite style ability evolution system
    /// Handles level-up ability upgrade choices using 1/2 keys
    /// Allows players to choose between two upgrade paths for abilities
    /// </summary>
    public class AbilityEvolutionHandler : NetworkBehaviour
    {
        [Header("Evolution System")]
        [SerializeField] private bool isEvolutionActive;
        [SerializeField] private AbilityEvolutionChoice currentChoice;
        [SerializeField] private float selectionTimeLimit = 10f; // 10 seconds to choose
        [SerializeField] private GameObject evolutionUIInstance;

        [Header("Evolution Database")]
        [SerializeField] private List<AbilityEvolutionData> evolutionDatabase = new List<AbilityEvolutionData>();

        // Events
        public event Action<AbilityEvolutionChoice> OnEvolutionSelected;
        public event Action<float> OnEvolutionTimeRemaining;
        public event Action OnEvolutionTimeExpired;

        // Component references
        private SimplePlayerController playerController;
        private EnhancedAbilitySystem abilitySystem;
        private Canvas evolutionCanvas;

        // State tracking
        private float evolutionStartTime;
        private bool hasSelectedEvolution;
        private Queue<AbilityEvolutionChoice> pendingEvolutions = new Queue<AbilityEvolutionChoice>();

        private GameDebugContext BuildContext()
        {
            return new GameDebugContext(
                GameDebugCategory.Ability,
                GameDebugSystemTag.Ability,
                GameDebugMechanicTag.Input,
                subsystem: nameof(AbilityEvolutionHandler),
                actor: gameObject.name);
        }

        private void Awake()
        {
            playerController = GetComponent<SimplePlayerController>();
            abilitySystem = GetComponent<EnhancedAbilitySystem>();
        }

        private void Start()
        {
            // Initialize evolution database if empty
            if (evolutionDatabase.Count == 0)
            {
                InitializeDefaultEvolutions();
            }
        }

        private void Update()
        {
            if (isEvolutionActive && !hasSelectedEvolution)
            {
                float timeRemaining = selectionTimeLimit - (Time.time - evolutionStartTime);
                OnEvolutionTimeRemaining?.Invoke(timeRemaining);

                if (timeRemaining <= 0f)
                {
                    HandleEvolutionTimeout();
                }
            }
        }

        /// <summary>
        /// Called when player levels up and can evolve abilities
        /// </summary>
        public void CheckForEvolutions(int newLevel)
        {
            if (!IsOwner) return;

            foreach (var evolution in evolutionDatabase)
            {
                if (evolution.unlockLevel == newLevel && !evolution.isCompleted)
                {
                    TriggerEvolutionChoice(evolution);
                    break; // Only one evolution at a time
                }
            }
        }

        /// <summary>
        /// Trigger evolution choice UI for a specific ability
        /// </summary>
        public void TriggerEvolutionChoice(AbilityEvolutionData evolution)
        {
            if (isEvolutionActive) 
            {
                // Queue additional evolutions
                var choice = new AbilityEvolutionChoice
                {
                    evolutionData = evolution,
                    abilityIndex = evolution.abilityIndex
                };
                pendingEvolutions.Enqueue(choice);
                return;
            }

            currentChoice = new AbilityEvolutionChoice
            {
                evolutionData = evolution,
                abilityIndex = evolution.abilityIndex
            };

            StartEvolutionSelection();
        }

        private void StartEvolutionSelection()
        {
            isEvolutionActive = true;
            hasSelectedEvolution = false;
            evolutionStartTime = Time.time;

            // Pause the game or slow time for evolution choice
            Time.timeScale = 0.1f; // Pokemon Unite style slow-motion

            // Show evolution UI
            ShowEvolutionUI();

            GameDebug.Log(BuildContext(),
                "Evolution choice started.",
                ("Ability", currentChoice.evolutionData.abilityName),
                ("Level", currentChoice.evolutionData.unlockLevel));
        }

        /// <summary>
        /// Input handler for ability evolution path 1 (1 key)
        /// </summary>
        public void OnAbilitySelect1(InputAction.CallbackContext context)
        {
            if (!isEvolutionActive || !context.performed) return;

            SelectEvolutionPath(EvolutionPath.PathA);
        }

        /// <summary>
        /// Input handler for ability evolution path 2 (2 key)
        /// </summary>
        public void OnAbilitySelect2(InputAction.CallbackContext context)
        {
            if (!isEvolutionActive || !context.performed) return;

            SelectEvolutionPath(EvolutionPath.PathB);
        }

        public void SelectEvolutionPath(EvolutionPath selectedPath)
        {
            if (hasSelectedEvolution) return;

            hasSelectedEvolution = true;
            currentChoice.selectedPath = selectedPath;

            // Apply evolution
            ApplyEvolution(currentChoice);

            // Network sync
            if (IsOwner)
            {
                SelectEvolutionServerRpc(currentChoice.abilityIndex, selectedPath);
            }

            CompleteEvolution();
        }

        private void ApplyEvolution(AbilityEvolutionChoice choice)
        {
            var evolution = choice.evolutionData;
            var selectedUpgrade = choice.selectedPath == EvolutionPath.PathA ? 
                                  evolution.pathA : evolution.pathB;

            // Apply upgrades to ability system
            if (abilitySystem != null)
            {
                ApplyAbilityUpgrade(choice.abilityIndex, selectedUpgrade);
            }

            // Mark evolution as completed
            evolution.isCompleted = true;
            evolution.selectedPath = choice.selectedPath;

            GameDebug.Log(BuildContext(),
                "Evolution applied.",
                ("Ability", evolution.abilityName),
                ("SelectedPath", choice.selectedPath),
                ("Upgrades", selectedUpgrade.upgradeDescription));
        }

        private void ApplyAbilityUpgrade(int abilityIndex, AbilityUpgrade upgrade)
        {
            // Apply stat modifications
            foreach (var statMod in upgrade.statModifications)
            {
                switch (statMod.statType)
                {
                    case AbilityStatType.Damage:
                        abilitySystem.ModifyAbilityDamage(abilityIndex, statMod.value);
                        break;
                    case AbilityStatType.Cooldown:
                        abilitySystem.ModifyAbilityCooldown(abilityIndex, statMod.value);
                        break;
                    case AbilityStatType.Range:
                        abilitySystem.ModifyAbilityRange(abilityIndex, statMod.value);
                        break;
                    case AbilityStatType.Duration:
                        abilitySystem.ModifyAbilityDuration(abilityIndex, statMod.value);
                        break;
                }
            }

            // Apply new effects
            foreach (var effect in upgrade.newEffects)
            {
                abilitySystem.AddAbilityEffect(abilityIndex, effect);
            }
        }

        private void CompleteEvolution()
        {
            isEvolutionActive = false;
            Time.timeScale = 1f; // Restore normal time

            HideEvolutionUI();
            OnEvolutionSelected?.Invoke(currentChoice);

            // Process pending evolutions
            if (pendingEvolutions.Count > 0)
            {
                var nextEvolution = pendingEvolutions.Dequeue();
                currentChoice = nextEvolution;
                StartEvolutionSelection();
            }
        }

        private void HandleEvolutionTimeout()
        {
            if (hasSelectedEvolution) return;

            // Auto-select Path A on timeout
            GameDebug.LogWarning(BuildContext(),
                "Evolution selection timed out - auto-selecting Path A.");

            SelectEvolutionPath(EvolutionPath.PathA);
            OnEvolutionTimeExpired?.Invoke();
        }

        private void ShowEvolutionUI()
        {
            // Create or show evolution UI
            if (evolutionUIInstance == null)
            {
                // In a real implementation, this would load a UI prefab
                GameDebug.Log(BuildContext(), "Evolution UI would be shown here.");
            }
        }

        private void HideEvolutionUI()
        {
            if (evolutionUIInstance != null)
            {
                evolutionUIInstance.SetActive(false);
            }
        }

        #region Network RPCs

        [ServerRpc]
        private void SelectEvolutionServerRpc(int abilityIndex, EvolutionPath selectedPath)
        {
            // Validate and apply evolution on server
            BroadcastEvolutionClientRpc(abilityIndex, selectedPath);
        }

        [ClientRpc]
        private void BroadcastEvolutionClientRpc(int abilityIndex, EvolutionPath selectedPath)
        {
            if (IsOwner) return; // Owner already applied

            // Apply evolution on all clients
            var evolution = evolutionDatabase.Find(e => e.abilityIndex == abilityIndex);
            if (evolution != null)
            {
                var choice = new AbilityEvolutionChoice
                {
                    evolutionData = evolution,
                    abilityIndex = abilityIndex,
                    selectedPath = selectedPath
                };
                ApplyEvolution(choice);
            }
        }

        #endregion

        private void InitializeDefaultEvolutions()
        {
            // Q Ability Evolution
            evolutionDatabase.Add(new AbilityEvolutionData
            {
                abilityName = "Basic Attack Enhancement",
                abilityIndex = 0,
                unlockLevel = 4,
                pathA = new AbilityUpgrade
                {
                    upgradeName = "Power Strike",
                    upgradeDescription = "Increases attack damage by 50%",
                    statModifications = new List<StatModification>
                    {
                        new StatModification { statType = AbilityStatType.Damage, value = 0.5f }
                    }
                },
                pathB = new AbilityUpgrade
                {
                    upgradeName = "Swift Strike",
                    upgradeDescription = "Reduces attack cooldown by 30%",
                    statModifications = new List<StatModification>
                    {
                        new StatModification { statType = AbilityStatType.Cooldown, value = -0.3f }
                    }
                }
            });

            // E Ability Evolution  
            evolutionDatabase.Add(new AbilityEvolutionData
            {
                abilityName = "Defensive Ability Enhancement",
                abilityIndex = 1,
                unlockLevel = 6,
                pathA = new AbilityUpgrade
                {
                    upgradeName = "Iron Wall",
                    upgradeDescription = "Increases defense duration by 100%",
                    statModifications = new List<StatModification>
                    {
                        new StatModification { statType = AbilityStatType.Duration, value = 1.0f }
                    }
                },
                pathB = new AbilityUpgrade
                {
                    upgradeName = "Reflection Shield",
                    upgradeDescription = "Reflects 25% damage back to attackers",
                    newEffects = new List<string> { "DamageReflection" }
                }
            });

            GameDebug.Log(BuildContext(),
                "Default evolution database initialized.",
                ("EvolutionCount", evolutionDatabase.Count));
        }
    }

    /// <summary>
    /// Data structure for ability evolution choices
    /// </summary>
    [Serializable]
    public struct AbilityEvolutionChoice
    {
        public AbilityEvolutionData evolutionData;
        public int abilityIndex;
        public EvolutionPath selectedPath;
    }

    /// <summary>
    /// Ability evolution data - defines upgrade paths
    /// </summary>
    [Serializable]
    public class AbilityEvolutionData
    {
        [Header("Basic Info")]
        public string abilityName;
        public int abilityIndex;
        public int unlockLevel;

        [Header("Evolution Paths")]
        public AbilityUpgrade pathA;
        public AbilityUpgrade pathB;

        [Header("State")]
        public bool isCompleted;
        public EvolutionPath selectedPath;
    }

    /// <summary>
    /// Ability upgrade definition
    /// </summary>
    [Serializable]
    public class AbilityUpgrade
    {
        [Header("Upgrade Info")]
        public string upgradeName;
        [TextArea(2, 4)]
        public string upgradeDescription;

        [Header("Modifications")]
        public List<StatModification> statModifications = new List<StatModification>();
        public List<string> newEffects = new List<string>();
    }

    /// <summary>
    /// Stat modification for abilities
    /// </summary>
    [Serializable]
    public struct StatModification
    {
        public AbilityStatType statType;
        public float value; // Multiplicative (0.5 = +50%, -0.3 = -30%)
    }

    /// <summary>
    /// Types of ability stats that can be modified
    /// </summary>
    public enum AbilityStatType
    {
        Damage,
        Cooldown,
        Range,
        Duration,
        ManaCost,
        CriticalChance
    }

    /// <summary>
    /// Evolution path selection
    /// </summary>
    public enum EvolutionPath
    {
        None,
        PathA, // Typically "1" key
        PathB  // Typically "2" key
    }
}


using UnityEngine;
using MOBA.ErrorHandling;

namespace MOBA.Configuration
{
    /// <summary>
    /// Configuration data for player characters
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "MOBA/Configuration/Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        public void Validate()
        {
            const string context = "PlayerConfig";
            ErrorHandler.ValidateRange(maxHealth, 50f, 1000f, context, nameof(maxHealth));
            ErrorHandler.ValidateRange(moveSpeed, 1f, 20f, context, nameof(moveSpeed));
            ErrorHandler.ValidateRange(jumpForce, 1f, 20f, context, nameof(jumpForce));
            ErrorHandler.ValidateRange(damage, 1f, 100f, context, nameof(damage));
            ErrorHandler.ValidateRange(attackRange, 1f, 20f, context, nameof(attackRange));
            ErrorHandler.ValidateRange(groundCheckDistance, 0.1f, 3f, context, nameof(groundCheckDistance));
            ErrorHandler.ValidateRange(footstepVolume, 0f, 1f, context, nameof(footstepVolume));
            ErrorHandler.ValidateRange(jumpSoundVolume, 0f, 1f, context, nameof(jumpSoundVolume));
            ErrorHandler.ValidateRange(attackSoundVolume, 0f, 1f, context, nameof(attackSoundVolume));
        }

        [Header("Health Settings")]
        [Range(50f, 1000f)]
        public float maxHealth = 100f;

        [Header("Movement Settings")]
        [Range(1f, 20f)]
        public float moveSpeed = 8f;
        [Range(1f, 20f)]
        public float jumpForce = 8f;

        [Header("Combat Settings")]
        [Range(1f, 100f)]
        public float damage = 25f;
        [Range(1f, 20f)]
        public float attackRange = 5f;

        [Header("Ground Detection")]
        [Range(0.1f, 3f)]
        public float groundCheckDistance = 1.1f;

        [Header("Physics")]
        public LayerMask groundLayerMask = 1;

        [Header("Respawn Settings")]
        public Vector3 respawnPosition = Vector3.zero;
        public float respawnDelay = 3f;

        [Header("Animation")]
        public float animationTransitionSpeed = 0.1f;

        [Header("Audio")]
        [Range(0f, 1f)]
        public float footstepVolume = 0.5f;
        [Range(0f, 1f)]
        public float jumpSoundVolume = 0.7f;
        [Range(0f, 1f)]
        public float attackSoundVolume = 0.8f;
    }

    /// <summary>
    /// Configuration data for game management
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MOBA/Configuration/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public void Validate()
        {
            const string context = "GameConfig";
            ErrorHandler.ValidateRange(maxPlayers, 1, 20, context, nameof(maxPlayers));
            ErrorHandler.ValidateRange(gameTimeInSeconds, 300f, 7200f, context, nameof(gameTimeInSeconds));
            ErrorHandler.ValidateRange(scoreToWin, 10, 1000, context, nameof(scoreToWin));
            ErrorHandler.ValidateRange(baseRespawnTime, 1f, 60f, context, nameof(baseRespawnTime));
            ErrorHandler.ValidateRange(respawnTimePerLevel, 0f, 5f, context, nameof(respawnTimePerLevel));
            ErrorHandler.ValidateRange(targetFrameRate, 30, 144, context, nameof(targetFrameRate));
        }

        [Header("Game Rules")]
        [Range(1, 20)]
        public int maxPlayers = 10;
        [Range(300f, 7200f)]
        public float gameTimeInSeconds = 1800f;
        [Range(10, 1000)]
        public int scoreToWin = 100;

        [Header("Scoring")]
        public int killScore = 10;
        public int assistScore = 5;
        public int objectiveScore = 25;

        [Header("Respawn")]
        [Range(1f, 60f)]
        public float baseRespawnTime = 10f;
        [Range(0f, 5f)]
        public float respawnTimePerLevel = 2f;

        [Header("Performance")]
        [Range(30, 144)]
        public int targetFrameRate = 60;
        public bool enableVSync = true;

        [Header("Debug")]
        public bool showDebugUI = false;
        public bool enableCheats = false;

        public int GameTimeInMinutes => Mathf.RoundToInt(gameTimeInSeconds / 60f);
        public float GetRespawnTime(int level) => baseRespawnTime + (respawnTimePerLevel * level);
    }

    /// <summary>
    /// Configuration data for ability system
    /// </summary>
    [CreateAssetMenu(fileName = "AbilitySystemConfig", menuName = "MOBA/Configuration/Ability System Config")]
    public class AbilitySystemConfig : ScriptableObject
    {
        public void Validate()
        {
            const string context = "AbilitySystemConfig";
            ErrorHandler.ValidateRange(globalCooldown, 0f, 2f, context, nameof(globalCooldown));
            ErrorHandler.ValidateRange(castingInterruptionRadius, 0f, 5f, context, nameof(castingInterruptionRadius));
            ErrorHandler.ValidateRange(baseMana, 50f, 500f, context, nameof(baseMana));
            ErrorHandler.ValidateRange(manaRegenPerSecond, 1f, 20f, context, nameof(manaRegenPerSecond));
            ErrorHandler.ValidateRange(outOfCombatManaMultiplier, 0f, 1f, context, nameof(outOfCombatManaMultiplier));
            ErrorHandler.ValidateRange(maxCooldownReduction, 0f, 0.75f, context, nameof(maxCooldownReduction));
        }

        [Header("Global Settings")]
        [Range(0f, 2f)]
        public float globalCooldown = 0.1f;
        [Range(0f, 5f)]
        public float castingInterruptionRadius = 2f;

        [Header("Resource System")]
        [Range(50f, 500f)]
        public float baseMana = 100f;
        [Range(1f, 20f)]
        public float manaRegenPerSecond = 5f;
        [Range(0f, 1f)]
        public float outOfCombatManaMultiplier = 2f;

        [Header("Effects")]
        public bool enableScreenShake = true;
        public bool enableParticleEffects = true;
        public bool enableSoundEffects = true;

        [Header("Cooldown Reduction")]
        [Range(0f, 0.75f)]
        public float maxCooldownReduction = 0.40f;

        [Header("UI")]
        public bool showCooldownNumbers = true;
        public bool showManaBar = true;
        public bool showAbilityDescriptions = true;
    }

    /// <summary>
    /// Configuration data for network settings
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "MOBA/Configuration/Network Config")]
    public class NetworkConfig : ScriptableObject
    {
        public void Validate()
        {
            const string context = "NetworkConfig";
            ErrorHandler.ValidateRange(maxConnections, 1, 50, context, nameof(maxConnections));
            ErrorHandler.ValidateRange(connectionTimeoutMs, 1000, 60000, context, nameof(connectionTimeoutMs));
            ErrorHandler.ValidateRange(sendRate, 16, 128, context, nameof(sendRate));
            ErrorHandler.ValidateRange(defaultPort, 1024, 65535, context, nameof(defaultPort));
            ErrorHandler.ValidateRange(maxMovementSpeed, 0.1f, 5f, context, nameof(maxMovementSpeed));
            ErrorHandler.ValidateRange(teleportThreshold, 1f, 100f, context, nameof(teleportThreshold));
            ErrorHandler.ValidateRange(maxRetransmissions, 1, 10, context, nameof(maxRetransmissions));
            ErrorHandler.ValidateRange(pingIntervalMs, 10, 1000, context, nameof(pingIntervalMs));
            ErrorHandler.ValidateRange(maxPingMs, 50, 2000, context, nameof(maxPingMs));
            ErrorHandler.ValidateRange(simulatedLatencyMs, 0, 500, context, nameof(simulatedLatencyMs));
            ErrorHandler.ValidateRange(simulatedPacketLoss, 0f, 50f, context, nameof(simulatedPacketLoss));
        }

        [Header("Connection Settings")]
        [Range(1, 50)]
        public int maxConnections = 10;
        [Range(1000, 60000)]
        public int connectionTimeoutMs = 30000;
        [Range(16, 128)]
        public int sendRate = 30;

        [Header("Server Settings")]
        public string defaultServerIP = "127.0.0.1";
        [Range(1024, 65535)]
        public ushort defaultPort = 7777;
        public bool enableRelay = false;
        public string relayJoinCode = "";

        [Header("Anti-Cheat")]
        public bool enableServerAuthority = true;
        public bool enableMovementValidation = true;
        public bool enableCombatValidation = true;
        [Range(0.1f, 5f)]
        public float maxMovementSpeed = 15f;
        [Range(1f, 100f)]
        public float teleportThreshold = 20f;

        [Header("Quality of Service")]
        [Range(1, 10)]
        public int maxRetransmissions = 3;
        [Range(10, 1000)]
        public int pingIntervalMs = 100;
        [Range(50, 2000)]
        public int maxPingMs = 500;

        [Header("Debug")]
        public bool showNetworkStats = false;
        public bool logNetworkEvents = false;
        public bool simulateNetworkConditions = false;
        [Range(0, 500)]
        public int simulatedLatencyMs = 0;
        [Range(0f, 50f)]
        public float simulatedPacketLoss = 0f;
    }

    /// <summary>
    /// Master configuration that references all other configs
    /// </summary>
    [CreateAssetMenu(fileName = "MasterConfig", menuName = "MOBA/Configuration/Master Config")]
    public class MasterConfig : ScriptableObject
    {
        private static MasterConfig _instance;
        public static MasterConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    _instance = Resources.Load<MasterConfig>("MasterConfig");
                    stopwatch.Stop();
                    long ms = stopwatch.ElapsedMilliseconds;
                    if (_instance == null)
                    {
                        Debug.LogError("[MasterConfig] No MasterConfig found in Resources folder!");
                    }
                    else if (ms > 100)
                    {
                        Debug.LogWarning($"[MasterConfig] Loading MasterConfig took {ms} ms. Consider optimizing config loading for large projects.");
                    }
                }
                return _instance;
            }
        }

        public PlayerConfig playerConfig;
        public GameConfig gameConfig;
        public AbilitySystemConfig abilityConfig;
        public NetworkConfig networkConfig;

        public string mainMenuScene = "MainMenu";
        public string gameplayScene = "Gameplay";
        public string loadingScene = "Loading";

        public bool isDevelopmentBuild = true;
        public string buildVersion = "1.0.0";
        public int buildNumber = 1;

        public void ValidateAll()
        {
            if (playerConfig == null)
                Debug.LogError("[MasterConfig] Player config is not assigned");
            else
                playerConfig.Validate();
            if (gameConfig == null)
                Debug.LogError("[MasterConfig] Game config is not assigned");
            else
                gameConfig.Validate();
            if (abilityConfig == null)
                Debug.LogError("[MasterConfig] Ability config is not assigned");
            else
                abilityConfig.Validate();
            if (networkConfig == null)
                Debug.LogError("[MasterConfig] Network config is not assigned");
            else
                networkConfig.Validate();
        }

        private void OnValidate()
        {
            if (playerConfig == null)
                Debug.LogWarning("[MasterConfig] Player config is not assigned");
            if (gameConfig == null)
                Debug.LogWarning("[MasterConfig] Game config is not assigned");
            if (abilityConfig == null)
                Debug.LogWarning("[MasterConfig] Ability config is not assigned");
            if (networkConfig == null)
                Debug.LogWarning("[MasterConfig] Network config is not assigned");
        }
    }
}

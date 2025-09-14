using UnityEngine;

namespace MOBA.Configuration
{
    /// <summary>
    /// Configuration data for player characters
    /// Replaces hardcoded values in SimplePlayerController
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "MOBA/Configuration/Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        /// <summary>
        /// Validates all player config values at runtime.
        /// Logs errors for out-of-range or invalid values.
        /// </summary>
        public void Validate()
        {
            if (maxHealth < 50f || maxHealth > 1000f)
                Debug.LogError($"[PlayerConfig] maxHealth out of range: {maxHealth}");
            if (moveSpeed < 1f || moveSpeed > 20f)
                Debug.LogError($"[PlayerConfig] moveSpeed out of range: {moveSpeed}");
            if (jumpForce < 1f || jumpForce > 20f)
                Debug.LogError($"[PlayerConfig] jumpForce out of range: {jumpForce}");
            if (damage < 1f || damage > 100f)
                Debug.LogError($"[PlayerConfig] damage out of range: {damage}");
            if (attackRange < 1f || attackRange > 20f)
                Debug.LogError($"[PlayerConfig] attackRange out of range: {attackRange}");
            if (groundCheckDistance < 0.1f || groundCheckDistance > 3f)
                Debug.LogError($"[PlayerConfig] groundCheckDistance out of range: {groundCheckDistance}");
            if (footstepVolume < 0f || footstepVolume > 1f)
                Debug.LogError($"[PlayerConfig] footstepVolume out of range: {footstepVolume}");
            if (jumpSoundVolume < 0f || jumpSoundVolume > 1f)
                Debug.LogError($"[PlayerConfig] jumpSoundVolume out of range: {jumpSoundVolume}");
            if (attackSoundVolume < 0f || attackSoundVolume > 1f)
                Debug.LogError($"[PlayerConfig] attackSoundVolume out of range: {attackSoundVolume}");
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
    }
    
    /// <summary>
    /// Configuration data for game management
    /// Replaces hardcoded values in SimpleGameManager
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MOBA/Configuration/Game Config")]
    public class GameConfig : ScriptableObject
    {
        /// <summary>
        /// Validates all game config values at runtime.
        /// Logs errors for out-of-range or invalid values.
        /// </summary>
        public void Validate()
        {
            if (maxPlayers < 1 || maxPlayers > 20)
                Debug.LogError($"[GameConfig] maxPlayers out of range: {maxPlayers}");
            if (gameTimeInSeconds < 300f || gameTimeInSeconds > 7200f)
                Debug.LogError($"[GameConfig] gameTimeInSeconds out of range: {gameTimeInSeconds}");
            if (scoreToWin < 10 || scoreToWin > 1000)
                Debug.LogError($"[GameConfig] scoreToWin out of range: {scoreToWin}");
            if (baseRespawnTime < 1f || baseRespawnTime > 60f)
                Debug.LogError($"[GameConfig] baseRespawnTime out of range: {baseRespawnTime}");
            if (respawnTimePerLevel < 0f || respawnTimePerLevel > 5f)
                Debug.LogError($"[GameConfig] respawnTimePerLevel out of range: {respawnTimePerLevel}");
            if (targetFrameRate < 30 || targetFrameRate > 144)
                Debug.LogError($"[GameConfig] targetFrameRate out of range: {targetFrameRate}");
        }

        [Header("Game Rules")]
        [Range(1, 20)]
        public int maxPlayers = 10;
        
        [Range(300f, 7200f)] // 5 minutes to 2 hours
        public float gameTimeInSeconds = 1800f; // 30 minutes
        
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
        
        // Computed properties
        public int GameTimeInMinutes => Mathf.RoundToInt(gameTimeInSeconds / 60f);
        public float GetRespawnTime(int level) => baseRespawnTime + (respawnTimePerLevel * level);
    }
    
    /// <summary>
    /// Configuration data for ability system
    /// Replaces hardcoded values in SimpleAbilitySystem
    /// </summary>
    [CreateAssetMenu(fileName = "AbilitySystemConfig", menuName = "MOBA/Configuration/Ability System Config")]
    public class AbilitySystemConfig : ScriptableObject
    {
        /// <summary>
        /// Validates all ability system config values at runtime.
        /// Logs errors for out-of-range or invalid values.
        /// </summary>
        public void Validate()
        {
            if (globalCooldown < 0f || globalCooldown > 2f)
                Debug.LogError($"[AbilitySystemConfig] globalCooldown out of range: {globalCooldown}");
            if (castingInterruptionRadius < 0f || castingInterruptionRadius > 5f)
                Debug.LogError($"[AbilitySystemConfig] castingInterruptionRadius out of range: {castingInterruptionRadius}");
            if (baseMana < 50f || baseMana > 500f)
                Debug.LogError($"[AbilitySystemConfig] baseMana out of range: {baseMana}");
            if (manaRegenPerSecond < 1f || manaRegenPerSecond > 20f)
                Debug.LogError($"[AbilitySystemConfig] manaRegenPerSecond out of range: {manaRegenPerSecond}");
            if (outOfCombatManaMultiplier < 0f || outOfCombatManaMultiplier > 1f)
                Debug.LogError($"[AbilitySystemConfig] outOfCombatManaMultiplier out of range: {outOfCombatManaMultiplier}");
            if (maxCooldownReduction < 0f || maxCooldownReduction > 0.75f)
                Debug.LogError($"[AbilitySystemConfig] maxCooldownReduction out of range: {maxCooldownReduction}");
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
        public float maxCooldownReduction = 0.40f; // 40% max CDR
        
        [Header("UI")]
        public bool showCooldownNumbers = true;
        public bool showManaBar = true;
        public bool showAbilityDescriptions = true;
    }
    
    /// <summary>
    /// Configuration data for network settings
    /// Replaces hardcoded values in SimpleNetworkManager
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "MOBA/Configuration/Network Config")]
    public class NetworkConfig : ScriptableObject
    {
        /// <summary>
        /// Validates all network config values at runtime.
        /// Logs errors for out-of-range or invalid values.
        /// </summary>
        public void Validate()
        {
            if (maxConnections < 1 || maxConnections > 50)
                Debug.LogError($"[NetworkConfig] maxConnections out of range: {maxConnections}");
            if (connectionTimeoutMs < 1000 || connectionTimeoutMs > 60000)
                Debug.LogError($"[NetworkConfig] connectionTimeoutMs out of range: {connectionTimeoutMs}");
            if (sendRate < 16 || sendRate > 128)
                Debug.LogError($"[NetworkConfig] sendRate out of range: {sendRate}");
            if (defaultPort < 1024 || defaultPort > 65535)
                Debug.LogError($"[NetworkConfig] defaultPort out of range: {defaultPort}");
            if (maxMovementSpeed < 0.1f || maxMovementSpeed > 5f)
                Debug.LogError($"[NetworkConfig] maxMovementSpeed out of range: {maxMovementSpeed}");
            if (teleportThreshold < 1f || teleportThreshold > 100f)
                Debug.LogError($"[NetworkConfig] teleportThreshold out of range: {teleportThreshold}");
            if (maxRetransmissions < 1 || maxRetransmissions > 10)
                Debug.LogError($"[NetworkConfig] maxRetransmissions out of range: {maxRetransmissions}");
            if (pingIntervalMs < 10 || pingIntervalMs > 1000)
                Debug.LogError($"[NetworkConfig] pingIntervalMs out of range: {pingIntervalMs}");
            if (maxPingMs < 50 || maxPingMs > 2000)
                Debug.LogError($"[NetworkConfig] maxPingMs out of range: {maxPingMs}");
            if (simulatedLatencyMs < 0 || simulatedLatencyMs > 500)
                Debug.LogError($"[NetworkConfig] simulatedLatencyMs out of range: {simulatedLatencyMs}");
            if (simulatedPacketLoss < 0f || simulatedPacketLoss > 50f)
                Debug.LogError($"[NetworkConfig] simulatedPacketLoss out of range: {simulatedPacketLoss}");
        }

        [Header("Connection Settings")]
        [Range(1, 50)]
        public int maxConnections = 10;
        
        [Range(1000, 60000)]
        public int connectionTimeoutMs = 30000; // 30 seconds
        
        [Range(16, 128)]
        public int sendRate = 30; // packets per second
        
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
    /// Provides single point of access to all game configurations
    /// </summary>
    [CreateAssetMenu(fileName = "MasterConfig", menuName = "MOBA/Configuration/Master Config")]
    public class MasterConfig : ScriptableObject
    {
        /// <summary>
        /// Validates all referenced configs at runtime and logs errors for missing/invalid configs.
        /// </summary>
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

        [Header("Configuration References")]
        public PlayerConfig playerConfig;
        public GameConfig gameConfig;
        public AbilitySystemConfig abilityConfig;
        public NetworkConfig networkConfig;
        
        [Header("Scene Management")]
        public string mainMenuScene = "MainMenu";
        public string gameplayScene = "Gameplay";
        public string loadingScene = "Loading";
        
        [Header("Build Settings")]
        public bool isDevelopmentBuild = true;
        public string buildVersion = "1.0.0";
        public int buildNumber = 1;
        
        // Validation
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
        
        // Convenience accessors
        public static MasterConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<MasterConfig>("MasterConfig");
                    if (_instance == null)
                    {
                        Debug.LogError("[MasterConfig] No MasterConfig found in Resources folder!");
                    }
                }
                return _instance;
            }
        }
        
        private static MasterConfig _instance;
    }

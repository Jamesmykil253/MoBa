using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace MOBA.VFX
{
    /// <summary>
    /// Advanced visual effects manager for MOBA gameplay
    /// Handles particle pooling, performance optimization, and combat effects
    /// Reference: Real-Time Rendering Chapter 13, Unity VFX Best Practices
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        #region Singleton
        
        private static VFXManager instance;
        public static VFXManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<VFXManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("VFXManager");
                        instance = go.AddComponent<VFXManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        #endregion
        
        #region Configuration
        
        [Header("VFX System Configuration")]
        [SerializeField, Tooltip("Maximum active particle systems")]
        private int maxActiveParticleSystems = 50;
        
        [SerializeField, Tooltip("Maximum particles per system")]
        private int maxParticlesPerSystem = 200;
        
        [SerializeField, Tooltip("VFX quality level (0-3)")]
        [Range(0, 3)] private int vfxQualityLevel = 2;
        
        [SerializeField, Tooltip("Enable particle pooling")]
        private bool enableParticlePooling = true;
        
        #pragma warning disable 0414 // Field assigned but never used - reserved for automatic LOD
        [SerializeField, Tooltip("Enable automatic LOD")]
        private bool enableAutomaticLOD = true;
        #pragma warning restore 0414
        
        [Header("Performance Settings")]
        [SerializeField, Tooltip("Maximum VFX distance")]
        private float maxVFXDistance = 100f;
        
        [SerializeField, Tooltip("Culling check interval")]
        private float cullingCheckInterval = 0.5f;
        
        #pragma warning disable 0414 // Field assigned but never used - reserved for memory management
        [SerializeField, Tooltip("Memory budget in MB")]
        private float memoryBudgetMB = 32f;
        #pragma warning restore 0414
        
        [SerializeField, Tooltip("Target framerate for auto-quality")]
        private int targetFramerate = 60;
        
        [Header("Effect Categories")]
        [SerializeField, Tooltip("Ability effects container")]
        private Transform abilityEffectsContainer;
        
        [SerializeField, Tooltip("Combat effects container")]
        private Transform combatEffectsContainer;
        
        [SerializeField, Tooltip("Environment effects container")]
        private Transform environmentEffectsContainer;
        
        [SerializeField, Tooltip("UI effects container")]
        private Transform uiEffectsContainer;
        
        #endregion
        
        #region VFX Data Structures
        
        /// <summary>
        /// VFX effect categories for organization
        /// </summary>
        public enum VFXCategory
        {
            Ability,        // Spell and ability effects
            Combat,         // Hit effects, damage numbers
            Environment,    // World environment effects
            UI,            // Interface visual feedback
            Character,     // Character-specific effects
            Projectile,    // Projectile trails and impacts
            Status,        // Buff/debuff indicators
            Audio          // Audio-visual synchronized effects
        }
        
        /// <summary>
        /// VFX priority levels for performance management
        /// </summary>
        public enum VFXPriority
        {
            Critical = 0,   // Must always play (core gameplay)
            High = 1,       // Important for gameplay clarity
            Medium = 2,     // Enhances experience
            Low = 3,        // Polish effects
            Background = 4  // Ambient effects
        }
        
        /// <summary>
        /// VFX playback modes
        /// </summary>
        public enum VFXPlaybackMode
        {
            OneShot,        // Play once and destroy
            Loop,           // Loop continuously
            Burst,          // Multiple short bursts
            Continuous,     // Long-running effect
            Triggered       // Event-driven playback
        }
        
        /// <summary>
        /// VFX effect data for runtime management
        /// </summary>
        [System.Serializable]
        public class VFXEffectData
        {
            public string effectId;
            public string effectName;
            public GameObject effectPrefab;
            public VFXCategory category;
            public VFXPriority priority;
            public VFXPlaybackMode playbackMode;
            
            [Header("Performance Settings")]
            public int maxParticles = 100;
            public float duration = 2f;
            public float cullingDistance = 50f;
            public bool useSimpleShader = false;
            public bool enableGPUInstancing = false;
            
            [Header("Quality Scaling")]
            public float[] qualityScales = { 0.5f, 0.75f, 1f, 1.25f };
            public bool[] qualityEnabled = { true, true, true, true };
            
            [Header("Audio Integration")]
            public string audioClipId;
            public bool syncWithAudio = false;
            public float audioDelay = 0f;
            
            [Header("Behavior")]
            public bool attachToTarget = false;
            public bool inheritTargetRotation = false;
            public bool scaleWithDistance = false;
            public Vector3 positionOffset = Vector3.zero;
            public Vector3 rotationOffset = Vector3.zero;
            
            public VFXEffectData(string id, GameObject prefab, VFXCategory cat = VFXCategory.Ability)
            {
                effectId = id;
                effectPrefab = prefab;
                category = cat;
                priority = VFXPriority.Medium;
                playbackMode = VFXPlaybackMode.OneShot;
            }
        }
        
        /// <summary>
        /// Runtime VFX instance tracking
        /// </summary>
        public class VFXInstance
        {
            public GameObject gameObject;
            public ParticleSystem[] particleSystems;
            public VFXEffectData effectData;
            public string effectId;
            public VFXCategory category;
            public float startTime;
            public float duration;
            public bool isLooping;
            public bool isPooled;
            public Transform target;
            public Vector3 targetOffset;
            public System.Action onComplete;
            
            public VFXInstance(GameObject go, VFXEffectData data)
            {
                gameObject = go;
                effectData = data;
                effectId = data.effectId;
                category = data.category;
                startTime = Time.time;
                duration = data.duration;
                isLooping = data.playbackMode == VFXPlaybackMode.Loop;
                particleSystems = go.GetComponentsInChildren<ParticleSystem>();
            }
            
            public bool IsAlive => gameObject != null && 
                                   (isLooping || Time.time - startTime < duration) &&
                                   (particleSystems.Length == 0 || System.Array.Exists(particleSystems, ps => ps.isPlaying));
            
            public float PlaybackTime => Time.time - startTime;
        }
        
        /// <summary>
        /// VFX pool for performance optimization
        /// </summary>
        public class VFXPool
        {
            public string effectId;
            public Queue<GameObject> availableInstances = new Queue<GameObject>();
            public List<VFXInstance> activeInstances = new List<VFXInstance>();
            public VFXEffectData effectData;
            public int poolSize;
            public int maxPoolSize;
            
            public VFXPool(string id, VFXEffectData data, int initialSize = 5, int maxSize = 20)
            {
                effectId = id;
                effectData = data;
                poolSize = initialSize;
                maxPoolSize = maxSize;
            }
        }
        
        #endregion
        
        #region State Data
        
        [Header("Runtime VFX Data")]
        [SerializeField] private List<VFXEffectData> vfxEffects = new List<VFXEffectData>();
        
        // VFX pools and tracking
        private Dictionary<string, VFXPool> vfxPools = new Dictionary<string, VFXPool>();
        private List<VFXInstance> activeEffects = new List<VFXInstance>();
        private Dictionary<string, VFXEffectData> effectLookup = new Dictionary<string, VFXEffectData>();
        
        // Performance tracking
        private int currentActiveParticles = 0;
        private int currentActiveEffects = 0;
        private float lastCullingCheck = 0f;
        private float lastPerformanceCheck = 0f;
        private Queue<float> frameTimeHistory = new Queue<float>();
        
        // Quality management
        private int currentQualityLevel = 2;
        private bool adaptiveQualityEnabled = true;
        private float lastQualityAdjustment = 0f;
        
        // Camera reference for culling
        private Camera mainCamera;
        private Transform cameraTransform;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event fired when VFX effect starts playing
        /// </summary>
        public System.Action<string, Vector3> OnVFXStarted;
        
        /// <summary>
        /// Event fired when VFX effect completes
        /// </summary>
        public System.Action<string> OnVFXCompleted;
        
        /// <summary>
        /// Event fired when VFX quality level changes
        /// </summary>
        public System.Action<int> OnQualityLevelChanged;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeVFXSystem();
        }
        
        private void Start()
        {
            // Find main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
            
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            
            // Create effect containers if not assigned
            CreateEffectContainers();
            
            // Initialize quality level
            currentQualityLevel = vfxQualityLevel;
        }
        
        private void Update()
        {
            UpdateActiveEffects();
            
            // Performance monitoring
            if (Time.time - lastPerformanceCheck > 1f)
            {
                MonitorPerformance();
                lastPerformanceCheck = Time.time;
            }
            
            // Culling check
            if (Time.time - lastCullingCheck > cullingCheckInterval)
            {
                PerformCullingCheck();
                lastCullingCheck = Time.time;
            }
            
            // Adaptive quality
            if (adaptiveQualityEnabled && Time.time - lastQualityAdjustment > 2f)
            {
                UpdateAdaptiveQuality();
                lastQualityAdjustment = Time.time;
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the VFX system
        /// </summary>
        private void InitializeVFXSystem()
        {
            // Build effect lookup
            BuildEffectLookup();
            
            // Initialize VFX pools
            if (enableParticlePooling)
            {
                InitializeVFXPools();
            }
            
            Debug.Log("[VFXManager] VFX system initialized");
        }
        
        /// <summary>
        /// Build lookup dictionary for VFX effects
        /// </summary>
        private void BuildEffectLookup()
        {
            effectLookup.Clear();
            foreach (var effect in vfxEffects)
            {
                if (!string.IsNullOrEmpty(effect.effectId))
                {
                    effectLookup[effect.effectId] = effect;
                }
            }
            
            Debug.Log($"[VFXManager] Built effect lookup with {effectLookup.Count} effects");
        }
        
        /// <summary>
        /// Initialize VFX object pools
        /// </summary>
        private void InitializeVFXPools()
        {
            foreach (var effect in vfxEffects)
            {
                if (effect.effectPrefab != null)
                {
                    var pool = new VFXPool(effect.effectId, effect);
                    CreatePoolInstances(pool);
                    vfxPools[effect.effectId] = pool;
                }
            }
            
            Debug.Log($"[VFXManager] Initialized {vfxPools.Count} VFX pools");
        }
        
        /// <summary>
        /// Create pool instances for a VFX effect
        /// </summary>
        private void CreatePoolInstances(VFXPool pool)
        {
            for (int i = 0; i < pool.poolSize; i++)
            {
                var instance = CreateVFXInstance(pool.effectData, Vector3.zero, Quaternion.identity, null, true);
                if (instance != null)
                {
                    instance.gameObject.SetActive(false);
                    pool.availableInstances.Enqueue(instance.gameObject);
                }
            }
        }
        
        /// <summary>
        /// Create effect containers if not assigned
        /// </summary>
        private void CreateEffectContainers()
        {
            if (abilityEffectsContainer == null)
            {
                abilityEffectsContainer = CreateContainer("AbilityEffects");
            }
            
            if (combatEffectsContainer == null)
            {
                combatEffectsContainer = CreateContainer("CombatEffects");
            }
            
            if (environmentEffectsContainer == null)
            {
                environmentEffectsContainer = CreateContainer("EnvironmentEffects");
            }
            
            if (uiEffectsContainer == null)
            {
                uiEffectsContainer = CreateContainer("UIEffects");
            }
        }
        
        /// <summary>
        /// Create a container for VFX effects
        /// </summary>
        private Transform CreateContainer(string name)
        {
            var container = new GameObject(name);
            container.transform.SetParent(transform);
            return container.transform;
        }
        
        #endregion
        
        #region VFX Playback
        
        /// <summary>
        /// Play a VFX effect by ID
        /// </summary>
        /// <param name="effectId">VFX effect identifier</param>
        /// <param name="position">World position to play effect</param>
        /// <param name="rotation">World rotation for effect</param>
        /// <param name="target">Transform to attach effect to (optional)</param>
        /// <param name="onComplete">Callback when effect completes</param>
        /// <returns>VFX instance for control</returns>
        public VFXInstance PlayVFX(string effectId, Vector3 position, Quaternion rotation = default, Transform target = null, System.Action onComplete = null)
        {
            if (!effectLookup.TryGetValue(effectId, out var effectData))
            {
                Debug.LogWarning($"[VFXManager] VFX effect not found: {effectId}");
                return null;
            }
            
            // Check performance limits
            if (!CanPlayNewEffect(effectData))
            {
                Debug.LogWarning($"[VFXManager] Cannot play VFX {effectId} - performance limits reached");
                return null;
            }
            
            // Check distance culling
            if (mainCamera != null && Vector3.Distance(position, cameraTransform.position) > effectData.cullingDistance)
            {
                Debug.Log($"[VFXManager] VFX {effectId} culled by distance");
                return null;
            }
            
            // Get VFX instance
            VFXInstance vfxInstance = null;
            
            if (enableParticlePooling && vfxPools.ContainsKey(effectId))
            {
                vfxInstance = GetPooledVFXInstance(effectId, position, rotation, target);
            }
            else
            {
                vfxInstance = CreateVFXInstance(effectData, position, rotation, target);
            }
            
            if (vfxInstance == null)
            {
                return null;
            }
            
            // Configure and play effect
            ConfigureVFXInstance(vfxInstance, effectData, target, onComplete);
            PlayVFXInstance(vfxInstance);
            
            // Track active effect
            activeEffects.Add(vfxInstance);
            currentActiveEffects++;
            
            // Play audio if configured
            if (!string.IsNullOrEmpty(effectData.audioClipId))
            {
                PlayVFXAudio(effectData, position);
            }
            
            // Fire event
            OnVFXStarted?.Invoke(effectId, position);
            
            Debug.Log($"[VFXManager] Playing VFX: {effectId} at {position}");
            return vfxInstance;
        }
        
        /// <summary>
        /// Play ability VFX with enhanced targeting
        /// </summary>
        /// <param name="effectId">VFX effect identifier</param>
        /// <param name="caster">Caster transform</param>
        /// <param name="target">Target transform (can be null)</param>
        /// <param name="targetPosition">Target position</param>
        /// <param name="abilityData">Additional ability data</param>
        /// <returns>VFX instance</returns>
        public VFXInstance PlayAbilityVFX(string effectId, Transform caster, Transform target, Vector3 targetPosition, object abilityData = null)
        {
            Vector3 effectPosition = caster != null ? caster.position : targetPosition;
            Quaternion effectRotation = Quaternion.identity;
            
            // Calculate rotation towards target
            if (target != null)
            {
                Vector3 direction = (target.position - effectPosition).normalized;
                effectRotation = Quaternion.LookRotation(direction);
            }
            else if (targetPosition != effectPosition)
            {
                Vector3 direction = (targetPosition - effectPosition).normalized;
                effectRotation = Quaternion.LookRotation(direction);
            }
            
            var vfxInstance = PlayVFX(effectId, effectPosition, effectRotation, caster);
            
            if (vfxInstance != null)
            {
                // Store ability-specific data
                vfxInstance.target = target;
                vfxInstance.targetOffset = targetPosition - effectPosition;
                
                // Could add ability-specific behavior here
                if (abilityData != null)
                {
                    // Handle ability-specific VFX modifications
                }
            }
            
            return vfxInstance;
        }
        
        /// <summary>
        /// Play combat hit VFX
        /// </summary>
        /// <param name="effectId">VFX effect identifier</param>
        /// <param name="hitPosition">Hit position</param>
        /// <param name="hitNormal">Surface normal at hit point</param>
        /// <param name="damageAmount">Damage amount for scaling</param>
        /// <returns>VFX instance</returns>
        public VFXInstance PlayCombatVFX(string effectId, Vector3 hitPosition, Vector3 hitNormal, float damageAmount = 0f)
        {
            // Calculate rotation from normal
            Quaternion rotation = Quaternion.LookRotation(hitNormal);
            
            var vfxInstance = PlayVFX(effectId, hitPosition, rotation);
            
            if (vfxInstance != null && damageAmount > 0f)
            {
                // Scale effect based on damage
                ScaleVFXByDamage(vfxInstance, damageAmount);
            }
            
            return vfxInstance;
        }
        
        /// <summary>
        /// Stop VFX effect by ID
        /// </summary>
        /// <param name="effectId">VFX effect identifier</param>
        public void StopVFX(string effectId)
        {
            var effectsToStop = activeEffects.FindAll(e => e.effectId == effectId);
            
            foreach (var effect in effectsToStop)
            {
                StopVFXInstance(effect);
            }
        }
        
        /// <summary>
        /// Stop VFX instance
        /// </summary>
        /// <param name="vfxInstance">VFX instance to stop</param>
        public void StopVFXInstance(VFXInstance vfxInstance)
        {
            if (vfxInstance == null || vfxInstance.gameObject == null)
                return;
                
            // Stop particle systems
            foreach (var ps in vfxInstance.particleSystems)
            {
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            
            // Return to pool or destroy
            if (vfxInstance.isPooled && vfxPools.ContainsKey(vfxInstance.effectId))
            {
                ReturnToPool(vfxInstance);
            }
            else
            {
                Destroy(vfxInstance.gameObject);
            }
            
            // Remove from active list
            activeEffects.Remove(vfxInstance);
            currentActiveEffects--;
            
            // Fire completion event
            vfxInstance.onComplete?.Invoke();
            OnVFXCompleted?.Invoke(vfxInstance.effectId);
        }
        
        #endregion
        
        #region VFX Instance Management
        
        /// <summary>
        /// Create new VFX instance
        /// </summary>
        private VFXInstance CreateVFXInstance(VFXEffectData effectData, Vector3 position, Quaternion rotation, Transform parent, bool pooled = false)
        {
            if (effectData.effectPrefab == null)
            {
                Debug.LogError($"[VFXManager] Effect prefab is null for {effectData.effectId}");
                return null;
            }
            
            // Instantiate effect
            var gameObject = Instantiate(effectData.effectPrefab, position, rotation);
            gameObject.name = $"{effectData.effectName}_{Time.time}";
            
            // Set parent container
            Transform container = GetEffectContainer(effectData.category);
            if (container != null)
            {
                gameObject.transform.SetParent(container);
            }
            
            // Create VFX instance
            var vfxInstance = new VFXInstance(gameObject, effectData)
            {
                isPooled = pooled
            };
            
            return vfxInstance;
        }
        
        /// <summary>
        /// Get pooled VFX instance
        /// </summary>
        private VFXInstance GetPooledVFXInstance(string effectId, Vector3 position, Quaternion rotation, Transform target)
        {
            if (!vfxPools.TryGetValue(effectId, out var pool))
            {
                return CreateVFXInstance(effectLookup[effectId], position, rotation, target);
            }
            
            GameObject pooledObject = null;
            
            if (pool.availableInstances.Count > 0)
            {
                pooledObject = pool.availableInstances.Dequeue();
            }
            else if (pool.activeInstances.Count < pool.maxPoolSize)
            {
                pooledObject = CreateVFXInstance(pool.effectData, position, rotation, target, true)?.gameObject;
            }
            
            if (pooledObject == null)
            {
                Debug.LogWarning($"[VFXManager] No available pooled instances for {effectId}");
                return null;
            }
            
            // Reset pooled object
            pooledObject.transform.position = position;
            pooledObject.transform.rotation = rotation;
            pooledObject.SetActive(true);
            
            var vfxInstance = new VFXInstance(pooledObject, pool.effectData)
            {
                isPooled = true
            };
            
            pool.activeInstances.Add(vfxInstance);
            return vfxInstance;
        }
        
        /// <summary>
        /// Configure VFX instance with effect data
        /// </summary>
        private void ConfigureVFXInstance(VFXInstance vfxInstance, VFXEffectData effectData, Transform target, System.Action onComplete)
        {
            vfxInstance.target = target;
            vfxInstance.onComplete = onComplete;
            
            // Apply quality scaling
            ApplyQualityScaling(vfxInstance, effectData);
            
            // Configure particle systems
            foreach (var ps in vfxInstance.particleSystems)
            {
                ConfigureParticleSystem(ps, effectData);
            }
            
            // Handle attachment
            if (effectData.attachToTarget && target != null)
            {
                vfxInstance.gameObject.transform.SetParent(target);
                vfxInstance.gameObject.transform.localPosition = effectData.positionOffset;
                
                if (effectData.inheritTargetRotation)
                {
                    vfxInstance.gameObject.transform.localRotation = Quaternion.Euler(effectData.rotationOffset);
                }
            }
        }
        
        /// <summary>
        /// Configure individual particle system
        /// </summary>
        private void ConfigureParticleSystem(ParticleSystem ps, VFXEffectData effectData)
        {
            if (ps == null) return;
            
            var main = ps.main;
            
            // Apply quality scaling
            float qualityScale = effectData.qualityScales[Mathf.Clamp(currentQualityLevel, 0, effectData.qualityScales.Length - 1)];
            main.maxParticles = Mathf.RoundToInt(effectData.maxParticles * qualityScale);
            
            // Performance optimizations
            if (effectData.useSimpleShader)
            {
                // Could swap to simpler material here
            }
            
            // Update particle count tracking
            currentActiveParticles += main.maxParticles;
        }
        
        /// <summary>
        /// Play VFX instance
        /// </summary>
        private void PlayVFXInstance(VFXInstance vfxInstance)
        {
            // Play all particle systems
            foreach (var ps in vfxInstance.particleSystems)
            {
                if (ps != null)
                {
                    ps.Play();
                }
            }
            
            // Set up auto-destruction for non-looping effects
            if (!vfxInstance.isLooping)
            {
                StartCoroutine(AutoDestroyVFX(vfxInstance, vfxInstance.duration));
            }
        }
        
        /// <summary>
        /// Auto-destroy VFX after duration
        /// </summary>
        private IEnumerator AutoDestroyVFX(VFXInstance vfxInstance, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (vfxInstance != null && activeEffects.Contains(vfxInstance))
            {
                StopVFXInstance(vfxInstance);
            }
        }
        
        /// <summary>
        /// Return VFX instance to pool
        /// </summary>
        private void ReturnToPool(VFXInstance vfxInstance)
        {
            if (!vfxPools.TryGetValue(vfxInstance.effectId, out var pool))
                return;
                
            // Reset particle systems
            foreach (var ps in vfxInstance.particleSystems)
            {
                if (ps != null)
                {
                    ps.Stop(true);
                    ps.Clear();
                }
            }
            
            // Reset transform
            vfxInstance.gameObject.transform.SetParent(GetEffectContainer(vfxInstance.category));
            vfxInstance.gameObject.transform.localPosition = Vector3.zero;
            vfxInstance.gameObject.transform.localRotation = Quaternion.identity;
            vfxInstance.gameObject.SetActive(false);
            
            // Return to pool
            pool.availableInstances.Enqueue(vfxInstance.gameObject);
            pool.activeInstances.Remove(vfxInstance);
        }
        
        #endregion
        
        #region Performance Management
        
        /// <summary>
        /// Check if new effect can be played within performance limits
        /// </summary>
        private bool CanPlayNewEffect(VFXEffectData effectData)
        {
            // Check active effect limit
            if (currentActiveEffects >= maxActiveParticleSystems)
            {
                // Try to clean up finished effects
                CleanupFinishedEffects();
                
                if (currentActiveEffects >= maxActiveParticleSystems)
                {
                    // Check priority - can we replace a lower priority effect?
                    return TryReplaceEffect(effectData);
                }
            }
            
            // Check particle limit
            float qualityScale = effectData.qualityScales[Mathf.Clamp(currentQualityLevel, 0, effectData.qualityScales.Length - 1)];
            int particlesNeeded = Mathf.RoundToInt(effectData.maxParticles * qualityScale);
            
            if (currentActiveParticles + particlesNeeded > maxActiveParticleSystems * maxParticlesPerSystem)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Try to replace lower priority effect with new higher priority effect
        /// </summary>
        private bool TryReplaceEffect(VFXEffectData newEffectData)
        {
            VFXInstance lowestPriorityEffect = null;
            VFXPriority lowestPriority = VFXPriority.Critical;
            
            foreach (var effect in activeEffects)
            {
                if (effect.effectData.priority > lowestPriority)
                {
                    lowestPriority = effect.effectData.priority;
                    lowestPriorityEffect = effect;
                }
            }
            
            // Replace if new effect has higher priority
            if (lowestPriorityEffect != null && newEffectData.priority < lowestPriority)
            {
                StopVFXInstance(lowestPriorityEffect);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Update active effects and clean up finished ones
        /// </summary>
        private void UpdateActiveEffects()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                
                if (!effect.IsAlive)
                {
                    StopVFXInstance(effect);
                    continue;
                }
                
                // Update position for following effects
                if (effect.target != null && effect.effectData.attachToTarget)
                {
                    effect.gameObject.transform.position = effect.target.position + effect.targetOffset;
                }
                
                // Update distance scaling
                if (effect.effectData.scaleWithDistance && mainCamera != null)
                {
                    UpdateDistanceScaling(effect);
                }
            }
        }
        
        /// <summary>
        /// Clean up finished effects
        /// </summary>
        private void CleanupFinishedEffects()
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                
                if (!effect.IsAlive)
                {
                    StopVFXInstance(effect);
                }
            }
        }
        
        /// <summary>
        /// Monitor performance and adjust quality if needed
        /// </summary>
        private void MonitorPerformance()
        {
            // Track frame time
            frameTimeHistory.Enqueue(Time.deltaTime);
            if (frameTimeHistory.Count > 30) // Keep 30 frame history
            {
                frameTimeHistory.Dequeue();
            }
            
            // Update particle count
            UpdateParticleCount();
            
            // Log performance stats
            float avgFrameTime = 0f;
            foreach (float frameTime in frameTimeHistory)
            {
                avgFrameTime += frameTime;
            }
            avgFrameTime /= frameTimeHistory.Count;
            
            float currentFPS = 1f / avgFrameTime;
            
            Debug.Log($"[VFXManager] Performance: {currentActiveEffects} effects, {currentActiveParticles} particles, {currentFPS:F1} FPS");
        }
        
        /// <summary>
        /// Update current active particle count
        /// </summary>
        private void UpdateParticleCount()
        {
            currentActiveParticles = 0;
            
            foreach (var effect in activeEffects)
            {
                foreach (var ps in effect.particleSystems)
                {
                    if (ps != null && ps.isPlaying)
                    {
                        currentActiveParticles += ps.particleCount;
                    }
                }
            }
        }
        
        /// <summary>
        /// Perform distance-based culling check
        /// </summary>
        private void PerformCullingCheck()
        {
            if (mainCamera == null) return;
            
            Vector3 cameraPos = cameraTransform.position;
            
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                
                if (effect.gameObject == null) continue;
                
                float distance = Vector3.Distance(cameraPos, effect.gameObject.transform.position);
                
                // Cull effects beyond maximum distance
                if (distance > maxVFXDistance)
                {
                    StopVFXInstance(effect);
                    continue;
                }
                
                // Cull effects beyond their specific culling distance
                if (distance > effect.effectData.cullingDistance)
                {
                    effect.gameObject.SetActive(false);
                }
                else if (!effect.gameObject.activeInHierarchy)
                {
                    effect.gameObject.SetActive(true);
                }
            }
        }
        
        #endregion
        
        #region Quality Management
        
        /// <summary>
        /// Update adaptive quality based on performance
        /// </summary>
        private void UpdateAdaptiveQuality()
        {
            if (frameTimeHistory.Count < 10) return;
            
            float avgFrameTime = 0f;
            foreach (float frameTime in frameTimeHistory)
            {
                avgFrameTime += frameTime;
            }
            avgFrameTime /= frameTimeHistory.Count;
            
            float currentFPS = 1f / avgFrameTime;
            
            // Adjust quality based on FPS
            if (currentFPS < targetFramerate * 0.8f && currentQualityLevel > 0)
            {
                // Reduce quality
                SetVFXQualityLevel(currentQualityLevel - 1);
            }
            else if (currentFPS > targetFramerate * 1.1f && currentQualityLevel < 3)
            {
                // Increase quality
                SetVFXQualityLevel(currentQualityLevel + 1);
            }
        }
        
        /// <summary>
        /// Set VFX quality level
        /// </summary>
        /// <param name="qualityLevel">Quality level (0-3)</param>
        public void SetVFXQualityLevel(int qualityLevel)
        {
            qualityLevel = Mathf.Clamp(qualityLevel, 0, 3);
            
            if (qualityLevel == currentQualityLevel) return;
            
            currentQualityLevel = qualityLevel;
            vfxQualityLevel = qualityLevel;
            
            // Apply quality changes to active effects
            ApplyQualityToActiveEffects();
            
            OnQualityLevelChanged?.Invoke(qualityLevel);
            
            Debug.Log($"[VFXManager] VFX quality level changed to {qualityLevel}");
        }
        
        /// <summary>
        /// Apply quality scaling to VFX instance
        /// </summary>
        private void ApplyQualityScaling(VFXInstance vfxInstance, VFXEffectData effectData)
        {
            if (currentQualityLevel >= effectData.qualityEnabled.Length) return;
            
            // Check if effect is enabled at current quality level
            if (!effectData.qualityEnabled[currentQualityLevel])
            {
                vfxInstance.gameObject.SetActive(false);
                return;
            }
            
            float qualityScale = effectData.qualityScales[Mathf.Clamp(currentQualityLevel, 0, effectData.qualityScales.Length - 1)];
            
            // Apply scaling to particle systems
            foreach (var ps in vfxInstance.particleSystems)
            {
                if (ps != null)
                {
                    var main = ps.main;
                    main.maxParticles = Mathf.RoundToInt(effectData.maxParticles * qualityScale);
                    
                    // Scale emission rate
                    var emission = ps.emission;
                    if (emission.enabled)
                    {
                        var rateOverTime = emission.rateOverTime;
                        rateOverTime.constant *= qualityScale;
                        emission.rateOverTime = rateOverTime;
                    }
                }
            }
            
            // Scale object if needed
            if (qualityScale != 1f)
            {
                vfxInstance.gameObject.transform.localScale *= qualityScale;
            }
        }
        
        /// <summary>
        /// Apply quality changes to all active effects
        /// </summary>
        private void ApplyQualityToActiveEffects()
        {
            foreach (var effect in activeEffects)
            {
                ApplyQualityScaling(effect, effect.effectData);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get effect container for category
        /// </summary>
        private Transform GetEffectContainer(VFXCategory category)
        {
            switch (category)
            {
                case VFXCategory.Ability: return abilityEffectsContainer;
                case VFXCategory.Combat: return combatEffectsContainer;
                case VFXCategory.Environment: return environmentEffectsContainer;
                case VFXCategory.UI: return uiEffectsContainer;
                default: return transform;
            }
        }
        
        /// <summary>
        /// Play audio for VFX effect
        /// </summary>
        private void PlayVFXAudio(VFXEffectData effectData, Vector3 position)
        {
            if (MOBA.Audio.AudioManager.Instance != null)
            {
                if (effectData.syncWithAudio)
                {
                    StartCoroutine(PlayDelayedAudio(effectData.audioClipId, position, effectData.audioDelay));
                }
                else
                {
                    MOBA.Audio.AudioManager.Instance.PlaySpatialAudio(effectData.audioClipId, position);
                }
            }
        }
        
        /// <summary>
        /// Play delayed audio for synchronized effects
        /// </summary>
        private IEnumerator PlayDelayedAudio(string audioClipId, Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (MOBA.Audio.AudioManager.Instance != null)
            {
                MOBA.Audio.AudioManager.Instance.PlaySpatialAudio(audioClipId, position);
            }
        }
        
        /// <summary>
        /// Scale VFX based on damage amount
        /// </summary>
        private void ScaleVFXByDamage(VFXInstance vfxInstance, float damageAmount)
        {
            // Scale effect based on damage (example scaling)
            float scale = Mathf.Clamp(damageAmount / 100f, 0.5f, 2f);
            vfxInstance.gameObject.transform.localScale *= scale;
            
            // Could also modify particle count, emission rate, etc.
            foreach (var ps in vfxInstance.particleSystems)
            {
                if (ps != null)
                {
                    var main = ps.main;
                    main.startLifetime = main.startLifetime.constant * scale;
                    
                    var emission = ps.emission;
                    var rateOverTime = emission.rateOverTime;
                    rateOverTime.constant *= scale;
                    emission.rateOverTime = rateOverTime;
                }
            }
        }
        
        /// <summary>
        /// Update distance scaling for VFX
        /// </summary>
        private void UpdateDistanceScaling(VFXInstance vfxInstance)
        {
            float distance = Vector3.Distance(cameraTransform.position, vfxInstance.gameObject.transform.position);
            float scale = Mathf.Clamp01(1f - (distance / maxVFXDistance));
            
            vfxInstance.gameObject.transform.localScale = Vector3.one * scale;
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Enable/disable adaptive quality
        /// </summary>
        /// <param name="enabled">Enable adaptive quality</param>
        public void SetAdaptiveQuality(bool enabled)
        {
            adaptiveQualityEnabled = enabled;
        }
        
        /// <summary>
        /// Get VFX performance statistics
        /// </summary>
        /// <returns>Performance stats</returns>
        public VFXPerformanceStats GetPerformanceStats()
        {
            return new VFXPerformanceStats
            {
                activeEffects = currentActiveEffects,
                activeParticles = currentActiveParticles,
                pooledEffects = vfxPools.Count,
                qualityLevel = currentQualityLevel,
                memoryUsageMB = (float)System.GC.GetTotalMemory(false) / (1024f * 1024f)
            };
        }
        
        /// <summary>
        /// Check if VFX effect exists
        /// </summary>
        /// <param name="effectId">VFX effect identifier</param>
        /// <returns>True if effect exists</returns>
        public bool HasVFXEffect(string effectId)
        {
            return effectLookup.ContainsKey(effectId);
        }
        
        /// <summary>
        /// Get current VFX quality level
        /// </summary>
        /// <returns>Current quality level (0-3)</returns>
        public int GetQualityLevel()
        {
            return currentQualityLevel;
        }
        
        /// <summary>
        /// Stop all VFX effects
        /// </summary>
        public void StopAllVFX()
        {
            var effectsToStop = new List<VFXInstance>(activeEffects);
            foreach (var effect in effectsToStop)
            {
                StopVFXInstance(effect);
            }
        }
        
        /// <summary>
        /// Stop VFX effects by category
        /// </summary>
        /// <param name="category">VFX category to stop</param>
        public void StopCategory(VFXCategory category)
        {
            var effectsToStop = activeEffects.FindAll(e => e.category == category);
            foreach (var effect in effectsToStop)
            {
                StopVFXInstance(effect);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// VFX system performance statistics
    /// </summary>
    [System.Serializable]
    public struct VFXPerformanceStats
    {
        public int activeEffects;
        public int activeParticles;
        public int pooledEffects;
        public int qualityLevel;
        public float memoryUsageMB;
    }
}
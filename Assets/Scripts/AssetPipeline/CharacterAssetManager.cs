using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;
using System.Collections;

namespace MOBA.Assets
{
    /// <summary>
    /// Character asset manager for runtime character loading and management
    /// Handles asset streaming, LOD management, and performance optimization
    /// Reference: Game Engine Architecture Chapter 14, Unity In Action Chapter 9
    /// </summary>
    public class CharacterAssetManager : MonoBehaviour
    {
        #region Configuration
        
        [Header("Asset Management")]
        [SerializeField, Tooltip("Character asset specifications")]
        private CharacterAssetSpecification assetSpecs;
        
        [SerializeField, Tooltip("Enable asset streaming for large characters")]
        private bool enableAssetStreaming = true;
        
        [SerializeField, Tooltip("Maximum characters to keep loaded")]
        private int maxLoadedCharacters = 20;
        
        #pragma warning disable 0414 // Field assigned but never used - reserved for LOD system
        [SerializeField, Tooltip("Distance for automatic LOD switching")]
        private float lodSwitchDistance = 50f;
        #pragma warning restore 0414
        
        [Header("Performance Optimization")]
        #pragma warning disable 0414 // Field assigned but never used - reserved for GPU instancing
        [SerializeField, Tooltip("Enable GPU instancing for similar characters")]
        private bool enableGPUInstancing = true;
        #pragma warning restore 0414
        
        #pragma warning disable 0414 // Field assigned but never used - reserved for occlusion culling
        [SerializeField, Tooltip("Enable occlusion culling")]
        private bool enableOcclusionCulling = true;
        #pragma warning restore 0414
        
        [SerializeField, Tooltip("Maximum draw calls per frame")]
        private int maxDrawCallsPerFrame = 50;
        
        [Header("Animation System")]
        [SerializeField, Tooltip("Use playable graph for animation blending")]
        private bool usePlayableGraph = true;
        
        [SerializeField, Tooltip("Animation update mode")]
        private AnimatorUpdateMode animatorUpdateMode = AnimatorUpdateMode.Normal;
        
        [SerializeField, Tooltip("Animation culling mode")]
        private AnimatorCullingMode animatorCullingMode = AnimatorCullingMode.CullUpdateTransforms;
        
        #endregion
        
        #region State Data
        
        private Dictionary<string, GameObject> loadedCharacterPrefabs = new Dictionary<string, GameObject>();
        private Dictionary<GameObject, CharacterAssetData> activeCharacters = new Dictionary<GameObject, CharacterAssetData>();
        private Queue<GameObject> characterLoadQueue = new Queue<GameObject>();
        
        // Performance tracking
        private int currentDrawCalls = 0;
        private float lastPerformanceCheck = 0f;
        private const float performanceCheckInterval = 1f;
        
        // LOD management
        private Camera playerCamera;
        private List<LODCharacter> lodCharacters = new List<LODCharacter>();
        
        #endregion
        
        #region Structures
        
        /// <summary>
        /// Character asset data for runtime management
        /// </summary>
        [System.Serializable]
        public class CharacterAssetData
        {
            public string characterId;
            public GameObject prefab;
            public GameObject instance;
            public Animator animator;
            public PlayableGraph playableGraph;
            public LODGroup lodGroup;
            public float lastUsedTime;
            public bool isStreaming;
            public AssetValidationResult validationResult;
            
            public CharacterAssetData(string id, GameObject prefabRef, GameObject instanceRef)
            {
                characterId = id;
                prefab = prefabRef;
                instance = instanceRef;
                animator = instanceRef.GetComponent<Animator>();
                lodGroup = instanceRef.GetComponent<LODGroup>();
                lastUsedTime = Time.time;
                isStreaming = false;
            }
        }
        
        /// <summary>
        /// LOD character data for distance-based optimization
        /// </summary>
        [System.Serializable]
        public class LODCharacter
        {
            public GameObject character;
            public LODGroup lodGroup;
            public float distanceToCamera;
            public int currentLODLevel;
            
            public LODCharacter(GameObject characterObj)
            {
                character = characterObj;
                lodGroup = characterObj.GetComponent<LODGroup>();
                distanceToCamera = 0f;
                currentLODLevel = 0;
            }
        }
        
        /// <summary>
        /// Character loading request
        /// </summary>
        [System.Serializable]
        public class CharacterLoadRequest
        {
            public string characterId;
            public Vector3 spawnPosition;
            public Quaternion spawnRotation;
            public System.Action<GameObject> onLoadComplete;
            public bool highPriority;
            
            public CharacterLoadRequest(string id, Vector3 position, Quaternion rotation, System.Action<GameObject> callback, bool priority = false)
            {
                characterId = id;
                spawnPosition = position;
                spawnRotation = rotation;
                onLoadComplete = callback;
                highPriority = priority;
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Find player camera for LOD calculations
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }
            
            // Initialize asset specifications if not set
            if (assetSpecs == null)
            {
                Debug.LogWarning("[CharacterAssetManager] No asset specifications set - using defaults");
            }
        }
        
        private void Start()
        {
            // Pre-load common character assets
            PreloadCommonAssets();
            
            Debug.Log("[CharacterAssetManager] Initialized with streaming: " + enableAssetStreaming);
        }
        
        private void Update()
        {
            // Process character loading queue
            ProcessLoadingQueue();
            
            // Update LOD system
            if (playerCamera != null)
            {
                UpdateLODSystem();
            }
            
            // Performance monitoring
            if (Time.time - lastPerformanceCheck > performanceCheckInterval)
            {
                MonitorPerformance();
                lastPerformanceCheck = Time.time;
            }
            
            // Cleanup unused assets
            CleanupUnusedAssets();
        }
        
        private void OnDestroy()
        {
            // Cleanup playable graphs
            foreach (var characterData in activeCharacters.Values)
            {
                if (characterData.playableGraph.IsValid())
                {
                    characterData.playableGraph.Destroy();
                }
            }
        }
        
        #endregion
        
        #region Asset Loading
        
        /// <summary>
        /// Load character asynchronously
        /// </summary>
        /// <param name="characterId">Character identifier</param>
        /// <param name="position">Spawn position</param>
        /// <param name="rotation">Spawn rotation</param>
        /// <param name="onComplete">Callback when loading completes</param>
        public void LoadCharacterAsync(string characterId, Vector3 position, Quaternion rotation, System.Action<GameObject> onComplete)
        {
            var request = new CharacterLoadRequest(characterId, position, rotation, onComplete);
            StartCoroutine(LoadCharacterCoroutine(request));
        }
        
        /// <summary>
        /// Load character with high priority
        /// </summary>
        public void LoadCharacterHighPriority(string characterId, Vector3 position, Quaternion rotation, System.Action<GameObject> onComplete)
        {
            var request = new CharacterLoadRequest(characterId, position, rotation, onComplete, true);
            StartCoroutine(LoadCharacterCoroutine(request));
        }
        
        /// <summary>
        /// Character loading coroutine
        /// </summary>
        private IEnumerator LoadCharacterCoroutine(CharacterLoadRequest request)
        {
            // Check if character is already loaded
            if (loadedCharacterPrefabs.ContainsKey(request.characterId))
            {
                var instance = CreateCharacterInstance(request);
                request.onLoadComplete?.Invoke(instance);
                yield break;
            }
            
            // Load character prefab (simulate async loading)
            yield return StartCoroutine(LoadCharacterPrefab(request.characterId));
            
            // Create instance
            var characterInstance = CreateCharacterInstance(request);
            
            // Complete callback
            request.onLoadComplete?.Invoke(characterInstance);
        }
        
        /// <summary>
        /// Load character prefab from resources
        /// </summary>
        private IEnumerator LoadCharacterPrefab(string characterId)
        {
            // Simulate async loading delay
            yield return new WaitForSeconds(0.1f);
            
            // Load from Resources (in production, this would use Addressables)
            var prefab = Resources.Load<GameObject>($"Characters/{characterId}");
            
            if (prefab != null)
            {
                // Validate asset
                if (assetSpecs != null)
                {
                    var validation = assetSpecs.ValidateCharacterAsset(prefab);
                    if (!validation.isValid)
                    {
                        Debug.LogWarning($"[CharacterAssetManager] Character {characterId} failed validation: {string.Join(", ", validation.validationMessages)}");
                    }
                }
                
                loadedCharacterPrefabs[characterId] = prefab;
                Debug.Log($"[CharacterAssetManager] Loaded character prefab: {characterId}");
            }
            else
            {
                Debug.LogError($"[CharacterAssetManager] Failed to load character: {characterId}");
            }
        }
        
        /// <summary>
        /// Create character instance from loaded prefab
        /// </summary>
        private GameObject CreateCharacterInstance(CharacterLoadRequest request)
        {
            if (!loadedCharacterPrefabs.ContainsKey(request.characterId))
            {
                Debug.LogError($"[CharacterAssetManager] Character prefab not loaded: {request.characterId}");
                return null;
            }
            
            var prefab = loadedCharacterPrefabs[request.characterId];
            var instance = Instantiate(prefab, request.spawnPosition, request.spawnRotation);
            
            // Set up character asset data
            var assetData = new CharacterAssetData(request.characterId, prefab, instance);
            activeCharacters[instance] = assetData;
            
            // Configure animator
            ConfigureCharacterAnimator(assetData);
            
            // Set up LOD if available
            ConfigureCharacterLOD(assetData);
            
            // Set up playable graph if enabled
            if (usePlayableGraph && assetData.animator != null)
            {
                SetupPlayableGraph(assetData);
            }
            
            Debug.Log($"[CharacterAssetManager] Created character instance: {request.characterId}");
            return instance;
        }
        
        #endregion
        
        #region Character Configuration
        
        /// <summary>
        /// Configure character animator settings
        /// </summary>
        private void ConfigureCharacterAnimator(CharacterAssetData assetData)
        {
            if (assetData.animator == null)
                return;
                
            assetData.animator.updateMode = animatorUpdateMode;
            assetData.animator.cullingMode = animatorCullingMode;
            
            // Optimize animator for performance
            assetData.animator.fireEvents = false; // Disable events for NPCs
            assetData.animator.applyRootMotion = false; // Use physics-based movement
        }
        
        /// <summary>
        /// Configure character LOD settings
        /// </summary>
        private void ConfigureCharacterLOD(CharacterAssetData assetData)
        {
            if (assetData.lodGroup == null)
                return;
                
            // Add to LOD management
            var lodCharacter = new LODCharacter(assetData.instance);
            lodCharacters.Add(lodCharacter);
            
            // Configure LOD distances
            if (assetSpecs != null && assetSpecs.lodDistances.Length > 0)
            {
                var lods = assetData.lodGroup.GetLODs();
                for (int i = 0; i < lods.Length && i < assetSpecs.lodDistances.Length; i++)
                {
                    lods[i].screenRelativeTransitionHeight = CalculateLODTransitionHeight(assetSpecs.lodDistances[i]);
                }
                assetData.lodGroup.SetLODs(lods);
            }
        }
        
        /// <summary>
        /// Calculate LOD transition height from distance
        /// </summary>
        private float CalculateLODTransitionHeight(float distance)
        {
            // Convert distance to screen relative height
            // This is a simplified calculation - in production you'd use more sophisticated metrics
            float cameraFOV = playerCamera != null ? playerCamera.fieldOfView : 60f;
            return Mathf.Tan(cameraFOV * 0.5f * Mathf.Deg2Rad) / distance;
        }
        
        /// <summary>
        /// Set up playable graph for advanced animation blending
        /// </summary>
        private void SetupPlayableGraph(CharacterAssetData assetData)
        {
            if (assetData.animator == null)
                return;
                
            // Create playable graph
            assetData.playableGraph = PlayableGraph.Create($"CharacterGraph_{assetData.characterId}");
            assetData.playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            
            // Create animation playable output
            var output = AnimationPlayableOutput.Create(assetData.playableGraph, "Animation", assetData.animator);
            
            // You would add more complex animation logic here:
            // - Animation layers
            // - Blend trees
            // - State machines
            // - IK handling
            
            assetData.playableGraph.Play();
            
            Debug.Log($"[CharacterAssetManager] Set up playable graph for {assetData.characterId}");
        }
        
        #endregion
        
        #region LOD Management
        
        /// <summary>
        /// Update LOD system based on camera distance
        /// </summary>
        private void UpdateLODSystem()
        {
            Vector3 cameraPosition = playerCamera.transform.position;
            
            foreach (var lodCharacter in lodCharacters)
            {
                if (lodCharacter.character == null)
                    continue;
                    
                // Calculate distance to camera
                lodCharacter.distanceToCamera = Vector3.Distance(cameraPosition, lodCharacter.character.transform.position);
                
                // Update LOD based on distance
                if (lodCharacter.lodGroup != null)
                {
                    UpdateCharacterLOD(lodCharacter);
                }
            }
            
            // Remove null references
            lodCharacters.RemoveAll(lod => lod.character == null);
        }
        
        /// <summary>
        /// Update individual character LOD
        /// </summary>
        private void UpdateCharacterLOD(LODCharacter lodCharacter)
        {
            if (assetSpecs == null || assetSpecs.lodDistances.Length == 0)
                return;
                
            int newLODLevel = 0;
            for (int i = 0; i < assetSpecs.lodDistances.Length; i++)
            {
                if (lodCharacter.distanceToCamera > assetSpecs.lodDistances[i])
                {
                    newLODLevel = i + 1;
                }
            }
            
            // Clamp to available LOD levels
            var lods = lodCharacter.lodGroup.GetLODs();
            newLODLevel = Mathf.Clamp(newLODLevel, 0, lods.Length - 1);
            
            // Update LOD if changed
            if (newLODLevel != lodCharacter.currentLODLevel)
            {
                lodCharacter.currentLODLevel = newLODLevel;
                lodCharacter.lodGroup.ForceLOD(newLODLevel);
            }
        }
        
        #endregion
        
        #region Performance Management
        
        /// <summary>
        /// Process character loading queue
        /// </summary>
        private void ProcessLoadingQueue()
        {
            // Limit processing to maintain frame rate
            int processedThisFrame = 0;
            const int maxProcessPerFrame = 2;
            
            while (characterLoadQueue.Count > 0 && processedThisFrame < maxProcessPerFrame)
            {
                var character = characterLoadQueue.Dequeue();
                if (character != null)
                {
                    // Process character setup
                    ProcessCharacterSetup(character);
                }
                processedThisFrame++;
            }
        }
        
        /// <summary>
        /// Process character setup
        /// </summary>
        private void ProcessCharacterSetup(GameObject character)
        {
            // Apply performance optimizations
            OptimizeCharacterPerformance(character);
            
            // Update active character count
            if (activeCharacters.ContainsKey(character))
            {
                activeCharacters[character].lastUsedTime = Time.time;
            }
        }
        
        /// <summary>
        /// Optimize character for performance
        /// </summary>
        private void OptimizeCharacterPerformance(GameObject character)
        {
            // Disable shadows for distant characters
            var renderers = character.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (Vector3.Distance(character.transform.position, playerCamera.transform.position) > 30f)
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            
            // Optimize particle systems
            var particles = character.GetComponentsInChildren<ParticleSystem>();
            foreach (var particle in particles)
            {
                var main = particle.main;
                main.maxParticles = Mathf.Min(main.maxParticles, 50); // Limit particles
            }
        }
        
        /// <summary>
        /// Monitor performance metrics
        /// </summary>
        private void MonitorPerformance()
        {
            // Count current draw calls (simplified)
            currentDrawCalls = activeCharacters.Count * 2; // Rough estimate
            
            // Log performance warnings
            if (currentDrawCalls > maxDrawCallsPerFrame)
            {
                Debug.LogWarning($"[CharacterAssetManager] High draw calls: {currentDrawCalls}/{maxDrawCallsPerFrame}");
            }
            
            // Memory pressure check
            long memoryUsage = System.GC.GetTotalMemory(false);
            float memoryMB = memoryUsage / (1024f * 1024f);
            
            if (memoryMB > assetSpecs?.memoryBudgetMB * activeCharacters.Count * 2f)
            {
                Debug.LogWarning($"[CharacterAssetManager] High memory usage: {memoryMB:F1}MB");
                ForceAssetCleanup();
            }
        }
        
        /// <summary>
        /// Clean up unused assets
        /// </summary>
        private void CleanupUnusedAssets()
        {
            if (activeCharacters.Count <= maxLoadedCharacters)
                return;
                
            // Find oldest unused characters
            var charactersToRemove = new List<GameObject>();
            float cutoffTime = Time.time - 30f; // 30 seconds
            
            foreach (var kvp in activeCharacters)
            {
                if (kvp.Value.lastUsedTime < cutoffTime)
                {
                    charactersToRemove.Add(kvp.Key);
                }
            }
            
            // Remove old characters
            foreach (var character in charactersToRemove)
            {
                UnloadCharacter(character);
            }
        }
        
        /// <summary>
        /// Force cleanup of assets to free memory
        /// </summary>
        private void ForceAssetCleanup()
        {
            // Destroy oldest 25% of characters
            var sortedCharacters = new List<KeyValuePair<GameObject, CharacterAssetData>>(activeCharacters);
            sortedCharacters.Sort((a, b) => a.Value.lastUsedTime.CompareTo(b.Value.lastUsedTime));
            
            int toRemove = Mathf.Max(1, sortedCharacters.Count / 4);
            for (int i = 0; i < toRemove; i++)
            {
                UnloadCharacter(sortedCharacters[i].Key);
            }
            
            // Force garbage collection
            System.GC.Collect();
            
            Debug.Log($"[CharacterAssetManager] Force cleanup removed {toRemove} characters");
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Unload specific character
        /// </summary>
        /// <param name="character">Character to unload</param>
        public void UnloadCharacter(GameObject character)
        {
            if (activeCharacters.TryGetValue(character, out var assetData))
            {
                // Cleanup playable graph
                if (assetData.playableGraph.IsValid())
                {
                    assetData.playableGraph.Destroy();
                }
                
                // Remove from LOD management
                lodCharacters.RemoveAll(lod => lod.character == character);
                
                // Remove from active characters
                activeCharacters.Remove(character);
                
                // Destroy GameObject
                if (character != null)
                {
                    Destroy(character);
                }
                
                Debug.Log($"[CharacterAssetManager] Unloaded character: {assetData.characterId}");
            }
        }
        
        /// <summary>
        /// Get character asset data
        /// </summary>
        /// <param name="character">Character GameObject</param>
        /// <returns>Asset data or null if not found</returns>
        public CharacterAssetData GetCharacterAssetData(GameObject character)
        {
            activeCharacters.TryGetValue(character, out var assetData);
            return assetData;
        }
        
        /// <summary>
        /// Get performance statistics
        /// </summary>
        /// <returns>Performance stats</returns>
        public AssetPerformanceStats GetPerformanceStats()
        {
            return new AssetPerformanceStats
            {
                activeCharacters = activeCharacters.Count,
                loadedPrefabs = loadedCharacterPrefabs.Count,
                currentDrawCalls = currentDrawCalls,
                memoryUsageMB = System.GC.GetTotalMemory(false) / (1024f * 1024f),
                lodCharacters = lodCharacters.Count
            };
        }
        
        /// <summary>
        /// Pre-load common character assets
        /// </summary>
        private void PreloadCommonAssets()
        {
            // This would pre-load the most commonly used characters
            // For now, this is just a placeholder
            Debug.Log("[CharacterAssetManager] Pre-loading common assets...");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Performance statistics for asset management
    /// </summary>
    [System.Serializable]
    public struct AssetPerformanceStats
    {
        public int activeCharacters;
        public int loadedPrefabs;
        public int currentDrawCalls;
        public float memoryUsageMB;
        public int lodCharacters;
    }
}
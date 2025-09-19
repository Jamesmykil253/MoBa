using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

namespace MOBA.Audio
{
    /// <summary>
    /// Comprehensive audio manager for MOBA game audio systems
    /// Handles music, SFX, spatial audio, and dynamic mixing
    /// Reference: Game Audio Implementation Chapter 8, Unity Audio Best Practices
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<AudioManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("AudioManager");
                        instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        #endregion
        
        #region Configuration
        
        [Header("Audio System Configuration")]
        [SerializeField, Tooltip("Master audio mixer")]
        private AudioMixer masterMixer;
        
        [SerializeField, Tooltip("Music audio mixer group")]
        private AudioMixerGroup musicGroup;
        
        [SerializeField, Tooltip("SFX audio mixer group")]
        private AudioMixerGroup sfxGroup;
        
        [SerializeField, Tooltip("UI audio mixer group")]
        private AudioMixerGroup uiGroup;
        
        [SerializeField, Tooltip("Ambient audio mixer group")]
        private AudioMixerGroup ambientGroup;
        
        [SerializeField, Tooltip("Voice audio mixer group")]
        private AudioMixerGroup voiceGroup;
        
        [Header("Audio Source Pooling")]
        [SerializeField, Tooltip("Maximum audio sources for SFX")]
        private int maxSFXSources = 20;
        
        [SerializeField, Tooltip("Maximum audio sources for spatial audio")]
        private int maxSpatialSources = 15;
        
        [SerializeField, Tooltip("Audio source prefab")]
        private GameObject audioSourcePrefab;
        
        [Header("Spatial Audio Settings")]
        [SerializeField, Tooltip("Maximum distance for spatial audio")]
        private float maxSpatialDistance = 50f;
        
        [SerializeField, Tooltip("3D audio curve")]
        private AnimationCurve spatialAudioCurve = AnimationCurve.Linear(0, 1, 1, 0);
        
        [SerializeField, Tooltip("Doppler effect level")]
        private float dopplerLevel = 1f;
        
        [Header("Music System")]
        [SerializeField, Tooltip("Crossfade duration for music transitions")]
        private float musicCrossfadeDuration = 2f;
        
        [SerializeField, Tooltip("Music volume multiplier")]
        private float musicVolumeMultiplier = 0.7f;
        
        [Header("Dynamic Mixing")]
        [SerializeField, Tooltip("Enable dynamic range compression")]
        private bool enableDynamicCompression = true;
        
        [SerializeField, Tooltip("Audio listener reference")]
        private AudioListener audioListener;
        
        [SerializeField, Tooltip("Combat audio boost factor")]
        private float combatAudioBoost = 1.2f;
        
        [SerializeField, Tooltip("Distance fade curve")]
        private AnimationCurve distanceFadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        #endregion
        
        #region Audio Data Structures
        
        /// <summary>
        /// Audio clip categories for organization
        /// </summary>
        public enum AudioCategory
        {
            Music,
            SFX,
            UI,
            Ambient,
            Voice,
            Ability,
            Combat,
            Footsteps,
            Environment
        }
        
        /// <summary>
        /// Audio priority levels
        /// </summary>
        public enum AudioPriority
        {
            Low = 256,
            Normal = 128,
            High = 64,
            Critical = 0
        }
        
        /// <summary>
        /// Audio playback mode
        /// </summary>
        public enum PlaybackMode
        {
            OneShot,        // Play once and stop
            Loop,           // Loop continuously
            LoopWithIntro,  // Play intro once, then loop body
            Layered,        // Play multiple layers simultaneously
            Sequence        // Play clips in sequence
        }
        
        /// <summary>
        /// Audio clip data for runtime management
        /// </summary>
        [System.Serializable]
        public class AudioClipData
        {
            public string clipId;
            public AudioClip clip;
            public AudioCategory category;
            public AudioPriority priority;
            public PlaybackMode playbackMode;
            
            [Header("Playback Settings")]
            [Range(0f, 1f)] public float volume = 1f;
            [Range(-3f, 3f)] public float pitch = 1f;
            [Range(0f, 1f)] public float spatialBlend = 0f;
            public bool randomizePitch = false;
            [Range(0f, 0.3f)] public float pitchVariation = 0.1f;
            
            [Header("3D Audio Settings")]
            public float minDistance = 1f;
            public float maxDistance = 50f;
            public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
            
            [Header("Layered Audio")]
            public List<AudioClip> layeredClips = new List<AudioClip>();
            public List<float> layerVolumes = new List<float>();
            
            public AudioClipData(string id, AudioClip audioClip, AudioCategory cat = AudioCategory.SFX)
            {
                clipId = id;
                clip = audioClip;
                category = cat;
                priority = AudioPriority.Normal;
                playbackMode = PlaybackMode.OneShot;
            }
        }
        
        /// <summary>
        /// Runtime audio source tracking
        /// </summary>
        public class ManagedAudioSource
        {
            public AudioSource source;
            public string clipId;
            public AudioCategory category;
            public float startTime;
            public bool isLooping;
            public bool isSpatial;
            public Transform followTarget;
            public System.Action onComplete;
            
            public ManagedAudioSource(AudioSource audioSource)
            {
                source = audioSource;
                startTime = Time.time;
            }
            
            public bool IsPlaying => source != null && source.isPlaying;
            public float PlaybackTime => Time.time - startTime;
        }
        
        /// <summary>
        /// Music track data
        /// </summary>
        [System.Serializable]
        public class MusicTrack
        {
            public string trackId;
            public string trackName;
            public AudioClip introClip;
            public AudioClip loopClip;
            public AudioClip outroClip;
            [Range(0f, 1f)] public float volume = 1f;
            public bool isIntenseTrack = false;
            public List<string> tags = new List<string>();
        }
        
        #endregion
        
        #region State Data
        
        [Header("Runtime Audio Data")]
        [SerializeField] private List<AudioClipData> audioClips = new List<AudioClipData>();
        [SerializeField] private List<MusicTrack> musicTracks = new List<MusicTrack>();
        
        // Audio source pools
        private Queue<AudioSource> sfxSourcePool = new Queue<AudioSource>();
        private Queue<AudioSource> spatialSourcePool = new Queue<AudioSource>();
        private List<ManagedAudioSource> activeSources = new List<ManagedAudioSource>();
        
        // Music system
        private AudioSource musicSourceA;
        private AudioSource musicSourceB;
        private bool musicSourceAActive = true;
        private MusicTrack currentMusicTrack;
        private Coroutine musicTransitionCoroutine;
        
        // Volume settings
        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;
        private float uiVolume = 1f;
        private float ambientVolume = 1f;
        private float voiceVolume = 1f;
        
        // Dynamic mixing state
        private bool isInCombat = false;
        private float combatMixTimer = 0f;
        private const float combatMixDuration = 0.5f;
        
        // Performance tracking
        private int currentPlayingSounds = 0;
        private float lastCleanupTime = 0f;
        private const float cleanupInterval = 5f;
        
        // Audio clip lookup
        private Dictionary<string, AudioClipData> clipLookup = new Dictionary<string, AudioClipData>();
        private Dictionary<string, MusicTrack> trackLookup = new Dictionary<string, MusicTrack>();
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event fired when music track changes
        /// </summary>
        public System.Action<string> OnMusicTrackChanged;
        
        /// <summary>
        /// Event fired when volume settings change
        /// </summary>
        public System.Action<AudioCategory, float> OnVolumeChanged;
        
        /// <summary>
        /// Event fired when audio clip completes
        /// </summary>
        public System.Action<string> OnAudioClipComplete;
        
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
            
            // Find audio listener if not assigned
            if (audioListener == null)
            {
                audioListener = FindFirstObjectByType<AudioListener>();
            }
            
            InitializeAudioSystem();
        }
        
        private void Start()
        {
            LoadAudioSettings();
        }
        
        private void Update()
        {
            UpdateAudioSources();
            UpdateDynamicMixing();
            
            // Periodic cleanup
            if (Time.time - lastCleanupTime > cleanupInterval)
            {
                CleanupInactiveSources();
                lastCleanupTime = Time.time;
            }
        }
        
        private void OnDestroy()
        {
            SaveAudioSettings();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the audio system
        /// </summary>
        private void InitializeAudioSystem()
        {
            // Build audio clip lookup
            BuildAudioClipLookup();
            
            // Initialize audio source pools
            InitializeAudioSourcePools();
            
            // Set up music sources
            InitializeMusicSources();
            
            // Apply initial mixer settings
            ApplyMixerSettings();
            
            Debug.Log("[AudioManager] Audio system initialized");
        }
        
        /// <summary>
        /// Build lookup dictionary for audio clips
        /// </summary>
        private void BuildAudioClipLookup()
        {
            clipLookup.Clear();
            foreach (var clipData in audioClips)
            {
                if (!string.IsNullOrEmpty(clipData.clipId))
                {
                    clipLookup[clipData.clipId] = clipData;
                }
            }
            
            trackLookup.Clear();
            foreach (var track in musicTracks)
            {
                if (!string.IsNullOrEmpty(track.trackId))
                {
                    trackLookup[track.trackId] = track;
                }
            }
            
            Debug.Log($"[AudioManager] Built lookup tables: {clipLookup.Count} audio clips, {trackLookup.Count} music tracks");
        }
        
        /// <summary>
        /// Initialize audio source pools
        /// </summary>
        private void InitializeAudioSourcePools()
        {
            // Create SFX source pool
            for (int i = 0; i < maxSFXSources; i++)
            {
                var sfxSource = CreatePooledAudioSource("SFX_" + i, sfxGroup);
                sfxSourcePool.Enqueue(sfxSource);
            }
            
            // Create spatial source pool
            for (int i = 0; i < maxSpatialSources; i++)
            {
                var spatialSource = CreatePooledAudioSource("Spatial_" + i, sfxGroup);
                spatialSource.spatialBlend = 1f; // Full 3D
                spatialSourcePool.Enqueue(spatialSource);
            }
            
            Debug.Log($"[AudioManager] Initialized audio source pools: {maxSFXSources} SFX, {maxSpatialSources} spatial");
        }
        
        /// <summary>
        /// Create a pooled audio source
        /// </summary>
        private AudioSource CreatePooledAudioSource(string name, AudioMixerGroup mixerGroup)
        {
            GameObject go;
            
            if (audioSourcePrefab != null)
            {
                go = Instantiate(audioSourcePrefab, transform);
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(transform);
            }
            
            var source = go.GetComponent<AudioSource>();
            if (source == null)
            {
                source = go.AddComponent<AudioSource>();
            }
            
            // Configure audio source
            source.outputAudioMixerGroup = mixerGroup;
            source.playOnAwake = false;
            source.loop = false;
            source.volume = 1f;
            source.pitch = 1f;
            
            go.name = name;
            go.SetActive(false);
            
            return source;
        }
        
        /// <summary>
        /// Initialize music audio sources
        /// </summary>
        private void InitializeMusicSources()
        {
            // Create primary music source
            var musicGoA = new GameObject("MusicSource_A");
            musicGoA.transform.SetParent(transform);
            musicSourceA = musicGoA.AddComponent<AudioSource>();
            musicSourceA.outputAudioMixerGroup = musicGroup;
            musicSourceA.loop = true;
            musicSourceA.volume = 0f;
            musicSourceA.playOnAwake = false;
            
            // Create secondary music source for crossfading
            var musicGoB = new GameObject("MusicSource_B");
            musicGoB.transform.SetParent(transform);
            musicSourceB = musicGoB.AddComponent<AudioSource>();
            musicSourceB.outputAudioMixerGroup = musicGroup;
            musicSourceB.loop = true;
            musicSourceB.volume = 0f;
            musicSourceB.playOnAwake = false;
            
            Debug.Log("[AudioManager] Music sources initialized");
        }
        
        #endregion
        
        #region Audio Playback
        
        /// <summary>
        /// Play an audio clip by ID
        /// </summary>
        /// <param name="clipId">Audio clip identifier</param>
        /// <param name="volume">Volume override (0-1)</param>
        /// <param name="position">3D position for spatial audio</param>
        /// <param name="onComplete">Callback when playback completes</param>
        /// <returns>Managed audio source for control</returns>
        public ManagedAudioSource PlaySFX(string clipId, float volume = 1f, Vector3? position = null, System.Action onComplete = null)
        {
            if (!clipLookup.TryGetValue(clipId, out var clipData))
            {
                Debug.LogWarning($"[AudioManager] Audio clip not found: {clipId}");
                return null;
            }
            
            // Get appropriate audio source
            AudioSource audioSource = null;
            bool isSpatial = position.HasValue;
            
            if (isSpatial && spatialSourcePool.Count > 0)
            {
                audioSource = spatialSourcePool.Dequeue();
            }
            else if (!isSpatial && sfxSourcePool.Count > 0)
            {
                audioSource = sfxSourcePool.Dequeue();
            }
            
            if (audioSource == null)
            {
                Debug.LogWarning($"[AudioManager] No available audio sources for {clipId}");
                return null;
            }
            
            // Configure audio source
            ConfigureAudioSource(audioSource, clipData, volume, position);
            
            // Create managed source
            var managedSource = new ManagedAudioSource(audioSource)
            {
                clipId = clipId,
                category = clipData.category,
                isSpatial = isSpatial,
                isLooping = clipData.playbackMode == PlaybackMode.Loop,
                onComplete = onComplete
            };
            
            // Play audio
            audioSource.gameObject.SetActive(true);
            audioSource.Play();
            
            // Track active source
            activeSources.Add(managedSource);
            currentPlayingSounds++;
            
            Debug.Log($"[AudioManager] Playing SFX: {clipId} (Total playing: {currentPlayingSounds})");
            return managedSource;
        }
        
        /// <summary>
        /// Play spatial audio at a specific position
        /// </summary>
        /// <param name="clipId">Audio clip identifier</param>
        /// <param name="position">3D world position</param>
        /// <param name="volume">Volume override</param>
        /// <param name="followTarget">Transform to follow for moving sounds</param>
        /// <returns>Managed audio source</returns>
        public ManagedAudioSource PlaySpatialAudio(string clipId, Vector3 position, float volume = 1f, Transform followTarget = null)
        {
            var managedSource = PlaySFX(clipId, volume, position);
            
            if (managedSource != null)
            {
                managedSource.followTarget = followTarget;
                
                // Set 3D audio properties
                var source = managedSource.source;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, spatialAudioCurve);
                source.dopplerLevel = dopplerLevel;
            }
            
            return managedSource;
        }
        
        /// <summary>
        /// Play UI sound effect
        /// </summary>
        /// <param name="clipId">Audio clip identifier</param>
        /// <param name="volume">Volume override</param>
        public void PlayUI(string clipId, float volume = 1f)
        {
            if (!clipLookup.TryGetValue(clipId, out var clipData))
            {
                Debug.LogWarning($"[AudioManager] UI audio clip not found: {clipId}");
                return;
            }
            
            // Play as one-shot for UI sounds
            if (clipData.clip != null)
            {
                var source = GetAvailableAudioSource(false);
                if (source != null)
                {
                    source.outputAudioMixerGroup = uiGroup;
                    source.volume = volume * clipData.volume * uiVolume;
                    source.pitch = clipData.randomizePitch ? 
                        clipData.pitch + Random.Range(-clipData.pitchVariation, clipData.pitchVariation) : 
                        clipData.pitch;
                    
                    source.PlayOneShot(clipData.clip);
                }
            }
        }
        
        /// <summary>
        /// Stop audio by clip ID
        /// </summary>
        /// <param name="clipId">Audio clip identifier</param>
        public void StopAudio(string clipId)
        {
            var sourcesToStop = activeSources.FindAll(s => s.clipId == clipId);
            
            foreach (var managedSource in sourcesToStop)
            {
                if (managedSource.source != null)
                {
                    managedSource.source.Stop();
                    ReturnAudioSourceToPool(managedSource);
                }
            }
        }
        
        /// <summary>
        /// Stop all audio in a category
        /// </summary>
        /// <param name="category">Audio category to stop</param>
        public void StopCategory(AudioCategory category)
        {
            var sourcesToStop = activeSources.FindAll(s => s.category == category);
            
            foreach (var managedSource in sourcesToStop)
            {
                if (managedSource.source != null)
                {
                    managedSource.source.Stop();
                    ReturnAudioSourceToPool(managedSource);
                }
            }
        }
        
        #endregion
        
        #region Music System
        
        /// <summary>
        /// Play music track with crossfade
        /// </summary>
        /// <param name="trackId">Music track identifier</param>
        /// <param name="crossfade">Enable crossfading</param>
        public void PlayMusic(string trackId, bool crossfade = true)
        {
            if (!trackLookup.TryGetValue(trackId, out var track))
            {
                Debug.LogWarning($"[AudioManager] Music track not found: {trackId}");
                return;
            }
            
            // Stop existing music transition
            if (musicTransitionCoroutine != null)
            {
                StopCoroutine(musicTransitionCoroutine);
            }
            
            currentMusicTrack = track;
            
            if (crossfade)
            {
                musicTransitionCoroutine = StartCoroutine(CrossfadeMusicCoroutine(track));
            }
            else
            {
                PlayMusicImmediate(track);
            }
            
            OnMusicTrackChanged?.Invoke(trackId);
            Debug.Log($"[AudioManager] Playing music: {track.trackName}");
        }
        
        /// <summary>
        /// Play music immediately without crossfade
        /// </summary>
        private void PlayMusicImmediate(MusicTrack track)
        {
            var activeSource = musicSourceAActive ? musicSourceA : musicSourceB;
            var inactiveSource = musicSourceAActive ? musicSourceB : musicSourceA;
            
            // Stop inactive source
            inactiveSource.Stop();
            inactiveSource.volume = 0f;
            
            // Set up active source
            activeSource.clip = track.loopClip ?? track.introClip;
            activeSource.volume = track.volume * musicVolume * musicVolumeMultiplier;
            activeSource.loop = track.loopClip != null;
            activeSource.Play();
        }
        
        /// <summary>
        /// Crossfade between music tracks
        /// </summary>
        private IEnumerator CrossfadeMusicCoroutine(MusicTrack newTrack)
        {
            var fadeOutSource = musicSourceAActive ? musicSourceA : musicSourceB;
            var fadeInSource = musicSourceAActive ? musicSourceB : musicSourceA;
            
            // Set up new track
            fadeInSource.clip = newTrack.loopClip ?? newTrack.introClip;
            fadeInSource.loop = newTrack.loopClip != null;
            fadeInSource.volume = 0f;
            fadeInSource.Play();
            
            // Crossfade
            float elapsed = 0f;
            float startVolumeOut = fadeOutSource.volume;
            float targetVolumeIn = newTrack.volume * musicVolume * musicVolumeMultiplier;
            
            while (elapsed < musicCrossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / musicCrossfadeDuration;
                
                fadeOutSource.volume = Mathf.Lerp(startVolumeOut, 0f, t);
                fadeInSource.volume = Mathf.Lerp(0f, targetVolumeIn, t);
                
                yield return null;
            }
            
            // Complete transition
            fadeOutSource.Stop();
            fadeOutSource.volume = 0f;
            fadeInSource.volume = targetVolumeIn;
            
            // Switch active source
            musicSourceAActive = !musicSourceAActive;
            
            musicTransitionCoroutine = null;
        }
        
        /// <summary>
        /// Stop current music
        /// </summary>
        /// <param name="fadeOut">Enable fade out</param>
        public void StopMusic(bool fadeOut = true)
        {
            if (musicTransitionCoroutine != null)
            {
                StopCoroutine(musicTransitionCoroutine);
            }
            
            if (fadeOut)
            {
                musicTransitionCoroutine = StartCoroutine(FadeOutMusicCoroutine());
            }
            else
            {
                musicSourceA.Stop();
                musicSourceB.Stop();
                musicSourceA.volume = 0f;
                musicSourceB.volume = 0f;
            }
            
            currentMusicTrack = null;
            OnMusicTrackChanged?.Invoke(null);
        }
        
        /// <summary>
        /// Fade out current music
        /// </summary>
        private IEnumerator FadeOutMusicCoroutine()
        {
            var activeSource = musicSourceAActive ? musicSourceA : musicSourceB;
            float startVolume = activeSource.volume;
            float elapsed = 0f;
            
            while (elapsed < musicCrossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / musicCrossfadeDuration;
                
                activeSource.volume = Mathf.Lerp(startVolume, 0f, t);
                
                yield return null;
            }
            
            activeSource.Stop();
            activeSource.volume = 0f;
            
            musicTransitionCoroutine = null;
        }
        
        #endregion
        
        #region Audio Source Management
        
        /// <summary>
        /// Configure audio source with clip data
        /// </summary>
        private void ConfigureAudioSource(AudioSource source, AudioClipData clipData, float volumeOverride, Vector3? position)
        {
            source.clip = clipData.clip;
            source.volume = volumeOverride * clipData.volume * GetCategoryVolume(clipData.category);
            source.pitch = clipData.randomizePitch ? 
                clipData.pitch + Random.Range(-clipData.pitchVariation, clipData.pitchVariation) : 
                clipData.pitch;
            source.loop = clipData.playbackMode == PlaybackMode.Loop;
            source.priority = (int)clipData.priority;
            
            // 3D audio settings
            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.spatialBlend = clipData.spatialBlend;
                source.minDistance = clipData.minDistance;
                source.maxDistance = clipData.maxDistance;
                source.rolloffMode = clipData.rolloffMode;
            }
            else
            {
                source.spatialBlend = 0f; // 2D audio
            }
            
            // Set appropriate mixer group
            source.outputAudioMixerGroup = GetMixerGroup(clipData.category);
        }
        
        /// <summary>
        /// Get available audio source from pool
        /// </summary>
        private AudioSource GetAvailableAudioSource(bool spatial)
        {
            if (spatial && spatialSourcePool.Count > 0)
            {
                return spatialSourcePool.Dequeue();
            }
            else if (!spatial && sfxSourcePool.Count > 0)
            {
                return sfxSourcePool.Dequeue();
            }
            
            // Try to find an inactive source
            foreach (var managedSource in activeSources)
            {
                if (!managedSource.IsPlaying)
                {
                    ReturnAudioSourceToPool(managedSource);
                    return spatial && spatialSourcePool.Count > 0 ? spatialSourcePool.Dequeue() : 
                           !spatial && sfxSourcePool.Count > 0 ? sfxSourcePool.Dequeue() : null;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Return audio source to appropriate pool
        /// </summary>
        private void ReturnAudioSourceToPool(ManagedAudioSource managedSource)
        {
            if (managedSource.source == null) return;
            
            // Call completion callback
            managedSource.onComplete?.Invoke();
            
            // Reset audio source
            managedSource.source.Stop();
            managedSource.source.clip = null;
            managedSource.source.gameObject.SetActive(false);
            
            // Return to pool
            if (managedSource.isSpatial)
            {
                spatialSourcePool.Enqueue(managedSource.source);
            }
            else
            {
                sfxSourcePool.Enqueue(managedSource.source);
            }
            
            // Remove from active list
            activeSources.Remove(managedSource);
            currentPlayingSounds--;
            
            // Fire completion event
            OnAudioClipComplete?.Invoke(managedSource.clipId);
        }
        
        /// <summary>
        /// Update active audio sources
        /// </summary>
        private void UpdateAudioSources()
        {
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                var managedSource = activeSources[i];
                
                // Check if finished playing
                if (!managedSource.IsPlaying && !managedSource.isLooping)
                {
                    ReturnAudioSourceToPool(managedSource);
                    continue;
                }
                
                // Update position for following sounds
                if (managedSource.followTarget != null && managedSource.source != null)
                {
                    managedSource.source.transform.position = managedSource.followTarget.position;
                }
                
                // Update spatial audio based on distance
                if (managedSource.isSpatial && audioListener != null && managedSource.source != null)
                {
                    float distance = Vector3.Distance(audioListener.transform.position, managedSource.source.transform.position);
                    UpdateSpatialAudioSource(managedSource.source, distance);
                }
            }
        }
        
        /// <summary>
        /// Update spatial audio source based on distance
        /// </summary>
        private void UpdateSpatialAudioSource(AudioSource source, float distance)
        {
            if (distance > maxSpatialDistance)
            {
                source.volume = 0f;
                return;
            }
            
            // Apply distance-based volume falloff
            float normalizedDistance = distance / maxSpatialDistance;
            float volumeMultiplier = distanceFadeCurve.Evaluate(normalizedDistance);
            
            // Apply volume (preserve original volume setting)
            // source.volume *= volumeMultiplier; // This would compound, need to store original volume
        }
        
        /// <summary>
        /// Clean up inactive audio sources
        /// </summary>
        private void CleanupInactiveSources()
        {
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                var managedSource = activeSources[i];
                
                if (managedSource.source == null || (!managedSource.IsPlaying && !managedSource.isLooping))
                {
                    ReturnAudioSourceToPool(managedSource);
                }
            }
        }
        
        #endregion
        
        #region Volume Control
        
        /// <summary>
        /// Set master volume
        /// </summary>
        /// <param name="volume">Volume level (0-1)</param>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            masterMixer?.SetFloat("MasterVolume", VolumeToDecibels(masterVolume));
            OnVolumeChanged?.Invoke(AudioCategory.Music, masterVolume); // Use music as proxy for master
        }
        
        /// <summary>
        /// Set category volume
        /// </summary>
        /// <param name="category">Audio category</param>
        /// <param name="volume">Volume level (0-1)</param>
        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            volume = Mathf.Clamp01(volume);
            
            switch (category)
            {
                case AudioCategory.Music:
                    musicVolume = volume;
                    masterMixer?.SetFloat("MusicVolume", VolumeToDecibels(volume));
                    UpdateMusicVolume();
                    break;
                case AudioCategory.SFX:
                    sfxVolume = volume;
                    masterMixer?.SetFloat("SFXVolume", VolumeToDecibels(volume));
                    break;
                case AudioCategory.UI:
                    uiVolume = volume;
                    masterMixer?.SetFloat("UIVolume", VolumeToDecibels(volume));
                    break;
                case AudioCategory.Ambient:
                    ambientVolume = volume;
                    masterMixer?.SetFloat("AmbientVolume", VolumeToDecibels(volume));
                    break;
                case AudioCategory.Voice:
                    voiceVolume = volume;
                    masterMixer?.SetFloat("VoiceVolume", VolumeToDecibels(volume));
                    break;
            }
            
            OnVolumeChanged?.Invoke(category, volume);
        }
        
        /// <summary>
        /// Convert linear volume to decibels
        /// </summary>
        private float VolumeToDecibels(float volume)
        {
            return volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
        }
        
        /// <summary>
        /// Get category volume
        /// </summary>
        private float GetCategoryVolume(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Music: return musicVolume;
                case AudioCategory.SFX: return sfxVolume;
                case AudioCategory.UI: return uiVolume;
                case AudioCategory.Ambient: return ambientVolume;
                case AudioCategory.Voice: return voiceVolume;
                default: return sfxVolume;
            }
        }
        
        /// <summary>
        /// Get mixer group for category
        /// </summary>
        private AudioMixerGroup GetMixerGroup(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Music: return musicGroup;
                case AudioCategory.SFX: return sfxGroup;
                case AudioCategory.UI: return uiGroup;
                case AudioCategory.Ambient: return ambientGroup;
                case AudioCategory.Voice: return voiceGroup;
                default: return sfxGroup;
            }
        }
        
        /// <summary>
        /// Update music volume for active tracks
        /// </summary>
        private void UpdateMusicVolume()
        {
            if (currentMusicTrack != null)
            {
                float targetVolume = currentMusicTrack.volume * musicVolume * musicVolumeMultiplier;
                
                if (musicSourceA.isPlaying)
                {
                    musicSourceA.volume = targetVolume;
                }
                
                if (musicSourceB.isPlaying)
                {
                    musicSourceB.volume = targetVolume;
                }
            }
        }
        
        #endregion
        
        #region Dynamic Mixing
        
        /// <summary>
        /// Update dynamic mixing based on game state
        /// </summary>
        private void UpdateDynamicMixing()
        {
            // Combat audio mixing
            if (isInCombat)
            {
                combatMixTimer += Time.deltaTime;
                
                if (combatMixTimer < combatMixDuration)
                {
                    float t = combatMixTimer / combatMixDuration;
                    ApplyCombatMixing(t);
                }
            }
            else if (combatMixTimer > 0f)
            {
                combatMixTimer -= Time.deltaTime;
                
                if (combatMixTimer <= 0f)
                {
                    combatMixTimer = 0f;
                    ApplyCombatMixing(0f);
                }
                else
                {
                    float t = combatMixTimer / combatMixDuration;
                    ApplyCombatMixing(t);
                }
            }
        }
        
        /// <summary>
        /// Apply combat audio mixing
        /// </summary>
        private void ApplyCombatMixing(float intensity)
        {
            if (masterMixer == null) return;
            
            // Boost combat-related audio
            float combatBoost = Mathf.Lerp(1f, combatAudioBoost, intensity);
            masterMixer.SetFloat("CombatBoost", VolumeToDecibels(combatBoost));
            
            // Apply dynamic compression
            if (enableDynamicCompression)
            {
                float compressionThreshold = Mathf.Lerp(-10f, -6f, intensity);
                masterMixer.SetFloat("CompressionThreshold", compressionThreshold);
            }
        }
        
        /// <summary>
        /// Set combat state for dynamic mixing
        /// </summary>
        /// <param name="inCombat">Is player in combat</param>
        public void SetCombatState(bool inCombat)
        {
            isInCombat = inCombat;
            
            if (inCombat)
            {
                combatMixTimer = 0f;
            }
        }
        
        /// <summary>
        /// Apply mixer settings
        /// </summary>
        private void ApplyMixerSettings()
        {
            if (masterMixer == null) return;
            
            masterMixer.SetFloat("MasterVolume", VolumeToDecibels(masterVolume));
            masterMixer.SetFloat("MusicVolume", VolumeToDecibels(musicVolume));
            masterMixer.SetFloat("SFXVolume", VolumeToDecibels(sfxVolume));
            masterMixer.SetFloat("UIVolume", VolumeToDecibels(uiVolume));
            masterMixer.SetFloat("AmbientVolume", VolumeToDecibels(ambientVolume));
            masterMixer.SetFloat("VoiceVolume", VolumeToDecibels(voiceVolume));
        }
        
        #endregion
        
        #region Settings Persistence
        
        /// <summary>
        /// Load audio settings from PlayerPrefs
        /// </summary>
        private void LoadAudioSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("Audio_MusicVolume", 1f);
            sfxVolume = PlayerPrefs.GetFloat("Audio_SFXVolume", 1f);
            uiVolume = PlayerPrefs.GetFloat("Audio_UIVolume", 1f);
            ambientVolume = PlayerPrefs.GetFloat("Audio_AmbientVolume", 1f);
            voiceVolume = PlayerPrefs.GetFloat("Audio_VoiceVolume", 1f);
            
            ApplyMixerSettings();
            
            Debug.Log("[AudioManager] Audio settings loaded");
        }
        
        /// <summary>
        /// Save audio settings to PlayerPrefs
        /// </summary>
        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("Audio_MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("Audio_MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("Audio_SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("Audio_UIVolume", uiVolume);
            PlayerPrefs.SetFloat("Audio_AmbientVolume", ambientVolume);
            PlayerPrefs.SetFloat("Audio_VoiceVolume", voiceVolume);
            
            PlayerPrefs.Save();
            
            Debug.Log("[AudioManager] Audio settings saved");
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get current music track
        /// </summary>
        public string GetCurrentMusicTrack()
        {
            return currentMusicTrack?.trackId;
        }
        
        /// <summary>
        /// Get audio system performance stats
        /// </summary>
        public AudioPerformanceStats GetPerformanceStats()
        {
            return new AudioPerformanceStats
            {
                activeSources = activeSources.Count,
                availableSFXSources = sfxSourcePool.Count,
                availableSpatialSources = spatialSourcePool.Count,
                currentPlayingSounds = currentPlayingSounds,
                musicSourcesActive = (musicSourceA.isPlaying ? 1 : 0) + (musicSourceB.isPlaying ? 1 : 0)
            };
        }
        
        /// <summary>
        /// Check if audio clip exists
        /// </summary>
        /// <param name="clipId">Audio clip identifier</param>
        /// <returns>True if clip exists</returns>
        public bool HasAudioClip(string clipId)
        {
            return clipLookup.ContainsKey(clipId);
        }
        
        /// <summary>
        /// Get volume for category
        /// </summary>
        /// <param name="category">Audio category</param>
        /// <returns>Current volume level</returns>
        public float GetVolume(AudioCategory category)
        {
            return GetCategoryVolume(category);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Audio system performance statistics
    /// </summary>
    [System.Serializable]
    public struct AudioPerformanceStats
    {
        public int activeSources;
        public int availableSFXSources;
        public int availableSpatialSources;
        public int currentPlayingSounds;
        public int musicSourcesActive;
    }
}
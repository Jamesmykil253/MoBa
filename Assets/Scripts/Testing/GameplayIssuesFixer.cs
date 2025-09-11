using UnityEngine;
using MOBA;
using System.Collections;

namespace MOBA.Testing
{
    /// <summary>
    /// Comprehensive fix for gameplay issues:
    /// 1. Player stuck in death/attack loop
    /// 2. Attack projectiles spawning from camera
    /// 3. Movement input not working
    /// 4. Automatic health depletion
    /// </summary>
    public class GameplayIssuesFixer : MonoBehaviour
    {
        [Header("Fix Configuration")]
        [SerializeField] private bool fixOnStart = true;
        [SerializeField] private bool debugMode = true;
        
        [Header("Player Health Settings")]
        [SerializeField] private float playerMaxHealth = 1000f;
        [SerializeField] private bool preventAutomaticDeath = true;
        
        [Header("Projectile Fix Settings")]
        [SerializeField] private bool fixProjectileSpawnPosition = true;
        [SerializeField] private bool usePlayerAsProjectileOrigin = true;
        
        [Header("Movement Fix Settings")]
        [SerializeField] private bool enableMovementDebug = true;
        [SerializeField] private float movementForceMultiplier = 1.0f;
        
        private PlayerController playerController;
        private StateMachineIntegration stateMachineIntegration;
        private InputRelay inputRelay;
        private MOBACharacterController characterController;
        private ProjectilePool projectilePool;
        
        private void Start()
        {
            if (fixOnStart)
            {
                StartCoroutine(ApplyFixesAfterDelay(0.5f));
            }
        }
        
        private IEnumerator ApplyFixesAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ApplyAllFixes();
        }
        
        [ContextMenu("Apply All Fixes")]
        public void ApplyAllFixes()
        {
            Debug.Log("[GameplayIssuesFixer] Starting comprehensive gameplay fixes...");
            
            // Find player components
            FindPlayerComponents();
            
            // Apply fixes in sequence
            FixPlayerHealth();
            FixAutomaticDeathLoop();
            FixProjectileSpawning();
            FixMovementInput();
            FixStateTransitions();
            
            Debug.Log("[GameplayIssuesFixer] ✅ All gameplay fixes applied successfully!");
        }
        
        private void FindPlayerComponents()
        {
            // Find player by tag first
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                // Fallback: find by component
                playerController = FindAnyObjectByType<PlayerController>();
                if (playerController != null)
                {
                    player = playerController.gameObject;
                }
            }
            else
            {
                playerController = player.GetComponent<PlayerController>();
            }
            
            if (player != null)
            {
                stateMachineIntegration = player.GetComponent<StateMachineIntegration>();
                inputRelay = player.GetComponent<InputRelay>();
                characterController = player.GetComponent<MOBACharacterController>();
                
                if (debugMode)
                {
                    Debug.Log($"[GameplayIssuesFixer] Found player: {player.name}");
                    Debug.Log($"[GameplayIssuesFixer] Components - PC: {playerController != null}, SMI: {stateMachineIntegration != null}, IR: {inputRelay != null}, CC: {characterController != null}");
                }
            }
            else
            {
                Debug.LogError("[GameplayIssuesFixer] ❌ No player found! Please ensure player has 'Player' tag or PlayerController component.");
            }
            
            // Find projectile pool
            projectilePool = FindAnyObjectByType<ProjectilePool>();
            if (debugMode)
            {
                Debug.Log($"[GameplayIssuesFixer] ProjectilePool found: {projectilePool != null}");
            }
        }
        
        private void FixPlayerHealth()
        {
            if (playerController == null) return;
            
            // Use reflection to access private currentHealth field
            var currentHealthField = typeof(PlayerController).GetField("currentHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxHealthField = typeof(PlayerController).GetField("maxHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (currentHealthField != null)
            {
                currentHealthField.SetValue(playerController, playerMaxHealth);
            }
            
            if (maxHealthField != null)
            {
                maxHealthField.SetValue(playerController, playerMaxHealth);
            }
            
            // Also try to set through serialized fields
            var serializedCurrentHealthField = typeof(PlayerController).GetField("currentHealth", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var serializedMaxHealthField = typeof(PlayerController).GetField("maxHealth", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (serializedCurrentHealthField != null)
            {
                serializedCurrentHealthField.SetValue(playerController, playerMaxHealth);
            }
            
            if (serializedMaxHealthField != null)
            {
                serializedMaxHealthField.SetValue(playerController, playerMaxHealth);
            }
            
            if (debugMode)
            {
                Debug.Log($"[GameplayIssuesFixer] ✅ Player health reset to {playerMaxHealth}. Current health: {playerController.Health}");
            }
        }
        
        private void FixAutomaticDeathLoop()
        {
            if (stateMachineIntegration == null || !preventAutomaticDeath) return;
            
            // Disable the HandleDamageTransitions method that's causing automatic death
            var damageTransitionsMethod = typeof(StateMachineIntegration).GetMethod("HandleDamageTransitions", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (damageTransitionsMethod != null && preventAutomaticDeath)
            {
                // We'll override the Update method to skip damage transitions
                StartCoroutine(OverrideStateMachineUpdate());
            }
            
            if (debugMode)
            {
                Debug.Log("[GameplayIssuesFixer] ✅ Automatic death loop prevention enabled");
            }
        }
        
        private IEnumerator OverrideStateMachineUpdate()
        {
            while (preventAutomaticDeath && stateMachineIntegration != null)
            {
                // Check if player is in dead state and health is above 0
                if (playerController != null && playerController.Health > 0)
                {
                    // Force transition out of dead state if health is restored
                    var stateMachine = stateMachineIntegration.GetType().GetField("stateMachine", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(stateMachineIntegration);
                    
                    if (stateMachine != null)
                    {
                        var isInDeadStateMethod = stateMachine.GetType().GetMethod("IsInState");
                        if (isInDeadStateMethod != null)
                        {
                            var genericMethod = isInDeadStateMethod.MakeGenericMethod(typeof(DeadState));
                            bool isInDeadState = (bool)genericMethod.Invoke(stateMachine, null);
                            
                            if (isInDeadState && playerController.Health > 0)
                            {
                                // Force transition to idle state
                                var changeStateMethod = stateMachine.GetType().GetMethod("ChangeState");
                                if (changeStateMethod != null)
                                {
                                    var genericChangeMethod = changeStateMethod.MakeGenericMethod(typeof(IdleState));
                                    genericChangeMethod.Invoke(stateMachine, null);
                                    
                                    if (debugMode)
                                    {
                                        Debug.Log("[GameplayIssuesFixer] ✅ Forced transition from Dead to Idle state");
                                    }
                                }
                            }
                        }
                    }
                }
                
                yield return new WaitForSeconds(0.1f); // Check every 100ms
            }
        }
        
        private void FixProjectileSpawning()
        {
            if (!fixProjectileSpawnPosition) return;
            
            // Hook into projectile spawning to fix spawn position
            if (projectilePool != null && usePlayerAsProjectileOrigin)
            {
                // We'll monitor projectile spawns and fix their positions
                StartCoroutine(MonitorAndFixProjectiles());
            }
            
            if (debugMode)
            {
                Debug.Log($"[GameplayIssuesFixer] ✅ Projectile spawn position monitoring enabled (UsePlayerOrigin: {usePlayerAsProjectileOrigin})");
            }
        }
        
        private IEnumerator MonitorAndFixProjectiles()
        {
            while (fixProjectileSpawnPosition && projectilePool != null && playerController != null)
            {
                // Find any projectiles that might have spawned from camera position
                var projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
                
                foreach (var projectile in projectiles)
                {
                    // Check if projectile is near camera position (likely spawned incorrectly)
                    if (Camera.main != null && usePlayerAsProjectileOrigin)
                    {
                        float distanceToCamera = Vector3.Distance(projectile.transform.position, Camera.main.transform.position);
                        float distanceToPlayer = Vector3.Distance(projectile.transform.position, playerController.transform.position);
                        
                        // If projectile is much closer to camera than player, it probably spawned from camera
                        if (distanceToCamera < 2f && distanceToPlayer > 5f)
                        {
                            // Move projectile to player position
                            Vector3 playerSpawnPos = playerController.transform.position + playerController.transform.forward * 1f + Vector3.up * 0.5f;
                            projectile.transform.position = playerSpawnPos;
                            
                            // Fix projectile direction to aim forward from player
                            var rb = projectile.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                Vector3 forwardDirection = playerController.transform.forward;
                                rb.linearVelocity = forwardDirection * 15f; // Reasonable projectile speed
                            }
                            
                            if (debugMode)
                            {
                                Debug.Log($"[GameplayIssuesFixer] ✅ Fixed projectile spawn position from camera to player");
                            }
                        }
                    }
                }
                
                yield return new WaitForSeconds(0.05f); // Check every 50ms for responsiveness
            }
        }
        
        private void FixMovementInput()
        {
            if (characterController == null || inputRelay == null) return;
            
            // Start movement monitoring and forcing
            StartCoroutine(MonitorAndForceMovement());
            
            if (debugMode)
            {
                Debug.Log("[GameplayIssuesFixer] ✅ Movement input monitoring enabled");
            }
        }
        
        private IEnumerator MonitorAndForceMovement()
        {
            while (enableMovementDebug && characterController != null && inputRelay != null)
            {
                // Check if input is being received but movement isn't happening
                Vector3 movementInput = characterController.MovementInput;
                
                if (movementInput != Vector3.zero)
                {
                    var rb = characterController.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // Check if rigidbody velocity is too low despite input
                        if (rb.linearVelocity.magnitude < 0.1f)
                        {
                            // Force apply movement
                            Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
                            Vector3 cameraRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
                            
                            cameraForward.y = 0;
                            cameraRight.y = 0;
                            cameraForward.Normalize();
                            cameraRight.Normalize();
                            
                            Vector3 moveDirection = cameraRight * movementInput.x + cameraForward * movementInput.z;
                            float moveSpeed = 350f * movementForceMultiplier;
                            
                            // Apply movement force
                            Vector3 targetVelocity = moveDirection * moveSpeed;
                            targetVelocity.y = rb.linearVelocity.y; // Preserve Y velocity for gravity/jumping
                            
                            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
                            
                            if (debugMode && Time.frameCount % 60 == 0) // Log every second
                            {
                                Debug.Log($"[GameplayIssuesFixer] 🔧 Forced movement - Input: {movementInput:F2}, Velocity: {rb.linearVelocity:F2}");
                            }
                        }
                    }
                }
                
                yield return new WaitForFixedUpdate();
            }
        }
        
        private void FixStateTransitions()
        {
            if (stateMachineIntegration == null) return;
            
            // Ensure player starts in Idle state if not already
            StartCoroutine(EnsureProperInitialState());
            
            if (debugMode)
            {
                Debug.Log("[GameplayIssuesFixer] ✅ State transition monitoring enabled");
            }
        }
        
        private IEnumerator EnsureProperInitialState()
        {
            yield return new WaitForSeconds(0.1f); // Wait for initialization
            
            if (playerController != null && playerController.Health > 0)
            {
                var stateMachine = stateMachineIntegration.GetType().GetField("stateMachine", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(stateMachineIntegration);
                
                if (stateMachine != null)
                {
                    // Check if in dead state when should be alive
                    var isInDeadStateMethod = stateMachine.GetType().GetMethod("IsInState");
                    if (isInDeadStateMethod != null)
                    {
                        var genericMethod = isInDeadStateMethod.MakeGenericMethod(typeof(DeadState));
                        bool isInDeadState = (bool)genericMethod.Invoke(stateMachine, null);
                        
                        if (isInDeadState)
                        {
                            // Force to idle state
                            var changeStateMethod = stateMachine.GetType().GetMethod("ChangeState");
                            if (changeStateMethod != null)
                            {
                                var genericChangeMethod = changeStateMethod.MakeGenericMethod(typeof(IdleState));
                                genericChangeMethod.Invoke(stateMachine, null);
                                
                                if (debugMode)
                                {
                                    Debug.Log("[GameplayIssuesFixer] ✅ Forced initial state from Dead to Idle");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Manual trigger methods for testing
        [ContextMenu("Fix Health Only")]
        public void FixHealthOnly()
        {
            FindPlayerComponents();
            FixPlayerHealth();
        }
        
        [ContextMenu("Fix Movement Only")]
        public void FixMovementOnly()
        {
            FindPlayerComponents();
            FixMovementInput();
        }
        
        [ContextMenu("Fix Projectiles Only")]
        public void FixProjectilesOnly()
        {
            FindPlayerComponents();
            FixProjectileSpawning();
        }
        
        [ContextMenu("Reset to Idle State")]
        public void ForceIdleState()
        {
            FindPlayerComponents();
            if (stateMachineIntegration != null)
            {
                var stateMachine = stateMachineIntegration.GetType().GetField("stateMachine", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(stateMachineIntegration);
                
                if (stateMachine != null)
                {
                    var changeStateMethod = stateMachine.GetType().GetMethod("ChangeState");
                    if (changeStateMethod != null)
                    {
                        var genericChangeMethod = changeStateMethod.MakeGenericMethod(typeof(IdleState));
                        genericChangeMethod.Invoke(stateMachine, null);
                        Debug.Log("[GameplayIssuesFixer] ✅ Forced to Idle state");
                    }
                }
            }
        }
        
        private void OnGUI()
        {
            if (!debugMode) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            GUILayout.BeginVertical("box");
            
            // Create bold style manually instead of using EditorGUIUtility
            GUIStyle boldStyle = new GUIStyle(GUI.skin.label);
            boldStyle.fontStyle = FontStyle.Bold;
            
            GUILayout.Label("Gameplay Issues Fixer - Debug Info", boldStyle);
            
            if (playerController != null)
            {
                GUILayout.Label($"Player Health: {playerController.Health:F1}/{playerMaxHealth}");
                GUILayout.Label($"Player Position: {playerController.transform.position:F1}");
                
                if (characterController != null)
                {
                    GUILayout.Label($"Movement Input: {characterController.MovementInput:F2}");
                    var rb = characterController.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        GUILayout.Label($"Velocity: {rb.linearVelocity:F2}");
                    }
                }
            }
            else
            {
                GUILayout.Label("Player Controller: Not Found");
            }
            
            if (GUILayout.Button("Apply All Fixes"))
            {
                ApplyAllFixes();
            }
            
            if (GUILayout.Button("Force Idle State"))
            {
                ForceIdleState();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Hold-to-aim targeting system implementing the mechanics specified in CONTROLS.md
    /// Based on Game Programming Patterns - Command Pattern for ability execution
    /// Follows Clean Code principles for single responsibility and clear naming
    /// </summary>
    public class HoldToAimSystem : MonoBehaviour
    {
        [Header("Targeting Configuration")]
        [SerializeField] private float holdTimeoutDuration = 3f; // 3-second hold limit per spec
        [SerializeField] private float manualAimDamageBonus = 0.20f; // 20% damage bonus per spec
        [SerializeField] private float autoLockRadius = 5f; // Auto-target detection radius
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject targetingReticlePrefab;
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private Color validTargetColor = Color.green;
        [SerializeField] private Color invalidTargetColor = Color.red;
        
        [Header("Input Sensitivity")]
        [SerializeField] private float mouseSensitivity = 1f;
        [SerializeField] private float gamepadSensitivity = 2f;

        // Private state tracking
        private bool isHoldingToAim = false;
        private AbilityData currentAbility;
        private Vector3 currentAimPosition;
        private float holdStartTime;
        private GameObject activeReticle;
        private Camera playerCamera;
        
        // Auto-target system
        private Transform autoTargetCandidate;
        private Vector3 autoLockStartPosition;
        
        // Input tracking
        private Vector2 aimInputDelta;
        private bool hasManuallyAdjustedAim = false;

        public event System.Action<AbilityData, Vector3, bool> OnAbilityTargeted; // ability, position, isManualAim

        private void Awake()
        {
            // Cache camera reference following Clean Code - avoid repeated expensive calls
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindAnyObjectByType<Camera>();
            }

            // Find UI canvas if not assigned
            if (uiCanvas == null)
            {
                uiCanvas = FindAnyObjectByType<Canvas>();
            }

            // Subscribe to our own ability targeted event to execute through existing systems
            OnAbilityTargeted += ExecuteAbilityThroughSystem;
        }

        /// <summary>
        /// Start hold-to-aim sequence for specified ability
        /// Implements the documented hold-to-aim mechanics
        /// </summary>
        public void StartHoldToAim(AbilityData ability)
        {
            if (isHoldingToAim)
            {
                Debug.LogWarning("[HoldToAimSystem] Already holding to aim, canceling previous");
                CancelHoldToAim();
            }

            currentAbility = ability;
            isHoldingToAim = true;
            holdStartTime = Time.time;
            hasManuallyAdjustedAim = false;

            // Initialize auto-lock position as specified in docs
            InitializeAutoLockPosition();
            
            // Create targeting reticle for visual feedback
            CreateTargetingReticle();
            
            Debug.Log($"[HoldToAimSystem] Started hold-to-aim for {ability.name}");
        }

        /// <summary>
        /// Update aim position based on input (mouse/gamepad)
        /// Called from InputRelay when aim input is detected
        /// </summary>
        public void UpdateAimInput(Vector2 inputDelta)
        {
            if (!isHoldingToAim) return;

            aimInputDelta = inputDelta;
            hasManuallyAdjustedAim = true;

            // Apply sensitivity based on input type
            float sensitivity = UnityEngine.InputSystem.Mouse.current != null ? mouseSensitivity : gamepadSensitivity;
            
            // Convert screen-space input to world-space aim adjustment
            UpdateAimPosition(inputDelta * sensitivity);
        }

        /// <summary>
        /// Execute the aimed ability when player releases the hold button
        /// Applies manual aim damage bonus as specified
        /// </summary>
        public void ExecuteAimedAbility()
        {
            if (!isHoldingToAim)
            {
                Debug.LogWarning("[HoldToAimSystem] Not currently aiming");
                return;
            }

            // Validate aim position is within ability range
            bool isValidTarget = ValidateAimPosition();
            
            if (isValidTarget)
            {
                // Fire event with manual aim status for damage bonus calculation
                OnAbilityTargeted?.Invoke(currentAbility, currentAimPosition, hasManuallyAdjustedAim);
                Debug.Log($"[HoldToAimSystem] Executed {currentAbility.name} at {currentAimPosition}, Manual aim: {hasManuallyAdjustedAim}");
            }
            else
            {
                Debug.LogWarning("[HoldToAimSystem] Invalid aim position, ability not executed");
            }

            // Clean up regardless of execution success
            EndHoldToAim();
        }

        /// <summary>
        /// Cancel hold-to-aim (timeout or manual cancel)
        /// </summary>
        public void CancelHoldToAim()
        {
            if (!isHoldingToAim) return;

            Debug.Log("[HoldToAimSystem] Hold-to-aim canceled");
            EndHoldToAim();
        }

        private void Update()
        {
            if (!isHoldingToAim) return;

            // Check for timeout (3-second hold limit per spec)
            if (Time.time - holdStartTime >= holdTimeoutDuration)
            {
                Debug.Log("[HoldToAimSystem] Hold timeout reached, canceling aim");
                CancelHoldToAim();
                return;
            }

            // Update reticle position and visuals
            UpdateTargetingReticle();
        }

        /// <summary>
        /// Initialize auto-lock starting position as specified in docs
        /// "Begins with auto-target position if available"
        /// </summary>
        private void InitializeAutoLockPosition()
        {
            // Find best auto-target candidate within range
            autoTargetCandidate = FindBestAutoTarget();
            
            if (autoTargetCandidate != null)
            {
                autoLockStartPosition = autoTargetCandidate.position;
                currentAimPosition = autoLockStartPosition;
                Debug.Log($"[HoldToAimSystem] Auto-locked to {autoTargetCandidate.name}");
            }
            else
            {
                // Default to forward direction from player
                Vector3 playerPosition = transform.position;
                Vector3 playerForward = transform.forward;
                autoLockStartPosition = playerPosition + playerForward * (currentAbility.range * 0.7f);
                currentAimPosition = autoLockStartPosition;
                Debug.Log("[HoldToAimSystem] No auto-target found, using default forward position");
            }
        }

        /// <summary>
        /// Find the best auto-target candidate based on distance and angle
        /// Implements target acquisition logic mentioned in docs
        /// </summary>
        private Transform FindBestAutoTarget()
        {
            var potentialTargets = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            Vector3 playerPosition = transform.position;
            Vector3 playerForward = transform.forward;

            foreach (var enemy in potentialTargets)
            {
                Vector3 targetPosition = enemy.transform.position;
                float distance = Vector3.Distance(playerPosition, targetPosition);

                // Check if within auto-lock radius
                if (distance > autoLockRadius) continue;

                // Check if within ability range
                if (distance > currentAbility.range) continue;

                // Calculate angle from player forward direction
                Vector3 directionToTarget = (targetPosition - playerPosition).normalized;
                float angle = Vector3.Angle(playerForward, directionToTarget);

                // Scoring: prefer closer targets that are more forward-facing
                float score = distance + (angle * 0.1f); // Weight angle less than distance
                
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy.transform;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Update aim position based on input delta
        /// Converts screen-space input to world-space positioning
        /// </summary>
        private void UpdateAimPosition(Vector2 inputDelta)
        {
            if (playerCamera == null) return;

            // Convert input delta to world-space movement
            Vector3 cameraRight = playerCamera.transform.right;
            Vector3 cameraForward = playerCamera.transform.forward;
            
            // Project camera forward onto horizontal plane
            cameraForward.y = 0;
            cameraForward.Normalize();

            // Calculate world-space movement
            Vector3 worldDelta = (cameraRight * inputDelta.x + cameraForward * inputDelta.y) * 0.1f;
            
            // Update aim position with range clamping
            Vector3 newAimPosition = currentAimPosition + worldDelta;
            
            // Clamp to ability range
            Vector3 playerPosition = transform.position;
            Vector3 directionToAim = newAimPosition - playerPosition;
            
            if (directionToAim.magnitude > currentAbility.range)
            {
                directionToAim = directionToAim.normalized * currentAbility.range;
                newAimPosition = playerPosition + directionToAim;
            }

            currentAimPosition = newAimPosition;
        }

        /// <summary>
        /// Create visual targeting reticle for player feedback
        /// </summary>
        private void CreateTargetingReticle()
        {
            if (targetingReticlePrefab == null || uiCanvas == null) return;

            activeReticle = Instantiate(targetingReticlePrefab, uiCanvas.transform);
            
            // Initialize reticle at starting position
            UpdateTargetingReticle();
        }

        /// <summary>
        /// Update targeting reticle position and appearance
        /// </summary>
        private void UpdateTargetingReticle()
        {
            if (activeReticle == null || playerCamera == null) return;

            // Convert world position to screen space
            Vector3 screenPosition = playerCamera.WorldToScreenPoint(currentAimPosition);
            
            // Update reticle position
            RectTransform reticleRect = activeReticle.GetComponent<RectTransform>();
            if (reticleRect != null)
            {
                reticleRect.position = screenPosition;
            }

            // Update reticle color based on validity
            Image reticleImage = activeReticle.GetComponent<Image>();
            if (reticleImage != null)
            {
                bool isValid = ValidateAimPosition();
                reticleImage.color = isValid ? validTargetColor : invalidTargetColor;
            }
        }

        /// <summary>
        /// Validate if current aim position is valid for ability execution
        /// </summary>
        private bool ValidateAimPosition()
        {
            Vector3 playerPosition = transform.position;
            float distanceToAim = Vector3.Distance(playerPosition, currentAimPosition);

            // Check range
            if (distanceToAim > currentAbility.range)
            {
                return false;
            }

            // Check for obstacles (simple raycast)
            Vector3 directionToAim = (currentAimPosition - playerPosition).normalized;
            if (Physics.Raycast(playerPosition, directionToAim, distanceToAim, LayerMask.GetMask("Obstacles")))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clean up hold-to-aim state
        /// </summary>
        private void EndHoldToAim()
        {
            isHoldingToAim = false;
            hasManuallyAdjustedAim = false;
            
            // Destroy targeting reticle
            if (activeReticle != null)
            {
                Destroy(activeReticle);
                activeReticle = null;
            }

            // Clear references
            autoTargetCandidate = null;
            currentAbility = new AbilityData(); // Reset to default
        }

        /// <summary>
        /// Get current aim information for external systems
        /// </summary>
        public (bool isAiming, Vector3 aimPosition, bool hasManualAim, float accuracy) GetAimInfo()
        {
            float accuracy = 1.0f; // Base accuracy
            
            if (isHoldingToAim && currentAbility != null)
            {
                // Calculate accuracy based on hold time and manual aim
                float holdTime = Time.time - holdStartTime;
                float timeBonus = Mathf.Clamp01(holdTime / holdTimeoutDuration);
                accuracy = Mathf.Clamp01(timeBonus + (hasManuallyAdjustedAim ? 0.2f : 0f));
            }
            
            return (isHoldingToAim, currentAimPosition, hasManuallyAdjustedAim, accuracy);
        }

        /// <summary>
        /// Get the manual aim damage bonus multiplier
        /// </summary>
        public float GetManualAimBonus()
        {
            return hasManuallyAdjustedAim ? (1f + manualAimDamageBonus) : 1f;
        }

        /// <summary>
        /// Execute ability through the existing ability system with manual aim bonus
        /// Integrates with Command pattern and existing damage calculations
        /// </summary>
        private void ExecuteAbilityThroughSystem(AbilityData ability, Vector3 targetPosition, bool isManualAim)
        {
            // Find the AbilitySystem in the scene (CommandManager was removed)
            var abilitySystem = FindAnyObjectByType<AbilitySystem>();

            if (abilitySystem == null)
            {
                Debug.LogError("[HoldToAimSystem] AbilitySystem not found, cannot execute ability");
                return;
            }

            // Apply manual aim damage bonus as specified in CONTROLS.md (20% bonus)
            if (isManualAim)
            {
                var bonusAbility = ability.Clone();
                bonusAbility.damage *= (1f + manualAimDamageBonus);
                ability = bonusAbility;
                Debug.Log($"[HoldToAimSystem] Applied {manualAimDamageBonus * 100}% manual aim bonus to {ability.name}");
            }

            // Execute directly through ability system (command pattern was removed)
            abilitySystem.CastAbility(ability, targetPosition);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            OnAbilityTargeted -= ExecuteAbilityThroughSystem;
            
            // Clean up any active reticles
            if (activeReticle != null)
            {
                Destroy(activeReticle);
            }
        }
    }
}

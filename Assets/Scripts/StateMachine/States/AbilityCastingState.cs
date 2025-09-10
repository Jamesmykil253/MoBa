using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Ability Casting state - character is casting an ability
    /// Handles ability targeting, casting time, and projectile spawning
    /// </summary>
    public class AbilityCastingState : CharacterStateBase
    {
        private float castStartTime;
        private float castDuration;
        private bool isTargeting;
        private Vector3 targetPosition;
        private AbilityData currentAbility;

        public AbilityCastingState(MOBACharacterController controller)
        {
            this.controller = controller;
        }

        protected override void OnEnter()
        {
            castStartTime = Time.time;
            isTargeting = true;
            targetPosition = controller.transform.position + controller.transform.forward * 5f;

            // Determine cast duration based on ability type
            // This would be set when the state is entered
            castDuration = 1.5f; // Default cast time

            // Set casting animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsCasting", true);
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsStunned", false);
                animator.SetTrigger("CastAbility");
            }

            // Notify camera of ability casting
            var cameraController = Object.FindAnyObjectByType<MOBACameraController>();
            if (cameraController != null)
            {
                // Camera can show targeting reticle or zoom in
            }

            Debug.Log("Entered Ability Casting State");
        }

        protected override void OnUpdate()
        {
            float castProgress = (Time.time - castStartTime) / castDuration;

            if (isTargeting)
            {
                // Update targeting
                UpdateTargeting();

                // Check for cast confirmation
                if (Input.GetMouseButtonDown(0) || castProgress > 0.5f)
                {
                    ConfirmCast();
                }
            }
            else
            {
                // Casting in progress
                if (castProgress >= 1.0f)
                {
                    ExecuteAbility();
                }
            }
        }

        protected override void OnExit()
        {
            isTargeting = false;

            // Reset casting animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsCasting", false);
            }

            Debug.Log("Exited Ability Casting State");
        }

        private void UpdateTargeting()
        {
            // Update target position based on mouse/aim input
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    targetPosition = hit.point;
                }
            }

            // Visualize targeting
            Debug.DrawLine(controller.transform.position, targetPosition, Color.blue);
        }

        private void ConfirmCast()
        {
            isTargeting = false;
            castStartTime = Time.time; // Reset timer for actual cast

            // Play cast confirmation effect
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetTrigger("CastConfirm");
            }

            Debug.Log("Ability cast confirmed");
        }

        private void ExecuteAbility()
        {
            // Spawn ability effect/projectile
            SpawnAbilityProjectile();

            // Apply ability effects
            ApplyAbilityEffects();

            // Play cast complete effect
            PlayCastCompleteEffect();

            Debug.Log("Ability executed");
        }

        private void SpawnAbilityProjectile()
        {
            // Use object pool for projectile spawning
            var pool = Object.FindAnyObjectByType<ProjectilePool>();
            if (pool != null)
            {
                Vector2 direction = ((Vector2)targetPosition - (Vector2)controller.transform.position).normalized;

                // Try to use flyweight for ability projectiles
                var availableFlyweights = pool.GetAvailableFlyweightNames();
                string flyweightName = "HomingProjectile"; // Use homing for abilities

                if (availableFlyweights.Contains(flyweightName))
                {
                    pool.SpawnProjectileWithFlyweight(controller.transform.position, direction, flyweightName);
                }
                else
                {
                    pool.SpawnProjectile(controller.transform.position, direction, 15f, 100f, 3f);
                }
            }
            else
            {
                // Fallback: create basic projectile
                GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectile.transform.position = controller.transform.position;
                projectile.transform.localScale = Vector3.one * 0.5f;

                var rb = projectile.AddComponent<Rigidbody>();
                Vector3 direction = (targetPosition - controller.transform.position).normalized;
                rb.linearVelocity = direction * 15f;

                Object.Destroy(projectile, 3f);
            }
        }

        private void ApplyAbilityEffects()
        {
            // Apply area effects, buffs, etc.
            // This would integrate with the ability system

            // Example: Apply knockback in area
            Collider[] affectedColliders = Physics.OverlapSphere(targetPosition, 3f);
            foreach (var collider in affectedColliders)
            {
                if (collider.gameObject != controller.gameObject)
                {
                    var damageable = collider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(75f);

                        // Apply knockback
                        if (collider.TryGetComponent(out Rigidbody rb))
                        {
                            Vector3 knockbackDir = (collider.transform.position - targetPosition).normalized;
                            rb.AddForce(knockbackDir * 10f, ForceMode.Impulse);
                        }
                    }
                }
            }
        }

        private void PlayCastCompleteEffect()
        {
            // Create cast complete effect
            GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.transform.position = targetPosition;
            effect.transform.localScale = Vector3.one * 2f;

            var renderer = effect.GetComponent<Renderer>();
            renderer.material.color = Color.cyan;

            Object.Destroy(effect, 1f);

            // Play cast sound
            // This would integrate with audio system
        }

        public override string GetStateName()
        {
            return isTargeting ? "Ability Targeting" : "Ability Casting";
        }
    }
}
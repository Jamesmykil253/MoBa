using UnityEngine;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Attacking state - character is performing a basic attack
    /// Handles attack animation, damage application, and combo system
    /// </summary>
    public class AttackingState : CharacterStateBase
    {
        private float attackStartTime;
        private float attackDuration = 0.8f; // Duration of attack animation
        private bool attackLanded;
        private int comboCount;
        private const int MAX_COMBO = 3;

        public AttackingState(MOBACharacterController controller)
        {
            this.controller = controller;
        }

        protected override void OnEnter()
        {
            attackStartTime = Time.time;
            attackLanded = false;
            comboCount++;

            // Set attacking animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsAttacking", true);
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsCasting", false);
                animator.SetBool("IsStunned", false);
                animator.SetInteger("AttackCombo", comboCount);
                animator.SetTrigger("Attack");
            }

            // Notify camera of attack
            var cameraController = Object.FindAnyObjectByType<MOBACameraController>();
            if (cameraController != null)
            {
                // Camera can add screen shake or focus on target
            }

            Debug.Log($"Entered Attacking State (Combo: {comboCount})");
        }

        protected override void OnUpdate()
        {
            float attackProgress = (Time.time - attackStartTime) / attackDuration;

            // Handle attack timing
            if (attackProgress < 0.3f)
            {
                // Wind-up phase
            }
            else if (attackProgress < 0.6f && !attackLanded)
            {
                // Active attack phase - check for hits
                CheckForAttackHits();
            }
            else if (attackProgress >= 1.0f)
            {
                // Attack finished
                OnAttackComplete();
            }

            // Check for combo input
            CheckForComboInput();
        }

        protected override void OnExit()
        {
            // Reset attacking animation
            if (controller.TryGetComponent(out Animator animator))
            {
                animator.SetBool("IsAttacking", false);
            }

            Debug.Log("Exited Attacking State");
        }

        private void CheckForAttackHits()
        {
            if (attackLanded) return;

            // Perform attack hit detection
            Vector3 attackPosition = controller.transform.position + controller.transform.forward * 2f;
            Vector3 attackSize = new Vector3(3f, 2f, 2f);

            // Use overlap box for attack detection
            Collider[] hitColliders = Physics.OverlapBox(attackPosition, attackSize / 2, controller.transform.rotation);

            foreach (var collider in hitColliders)
            {
                if (collider.gameObject != controller.gameObject)
                {
                    var damageable = collider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        // Calculate attack damage
                        float attackDamage = CalculateAttackDamage();

                        // Apply damage
                        damageable.TakeDamage(attackDamage);

                        // Apply knockback
                        ApplyKnockback(collider.gameObject, attackDamage);

                        attackLanded = true;

                        // Play hit effect
                        PlayHitEffect(collider.transform.position);

                        Debug.Log($"Attack hit: {collider.gameObject.name} for {attackDamage} damage");
                        break;
                    }
                }
            }
        }

        private void CheckForComboInput()
        {
            // Check for combo continuation input
            if (comboCount < MAX_COMBO && Input.GetMouseButtonDown(0))
            {
                // Combo input detected - will transition to new attack state
                Debug.Log("Combo input detected");
            }
        }

        private void OnAttackComplete()
        {
            // Reset combo if too much time has passed
            if (stateTimer > 2f)
            {
                comboCount = 0;
            }

            // Transition back to appropriate state
            // This will be handled by the state machine based on current conditions
        }

        private float CalculateAttackDamage()
        {
            // Base attack damage with combo multiplier
            float baseDamage = 50f;
            float comboMultiplier = 1f + (comboCount - 1) * 0.2f; // 20% bonus per combo

            return baseDamage * comboMultiplier;
        }

        private void ApplyKnockback(GameObject target, float damage)
        {
            if (target.TryGetComponent(out Rigidbody targetRb))
            {
                Vector3 knockbackDirection = (target.transform.position - controller.transform.position).normalized;
                float knockbackForce = Mathf.Min(damage / 10f, 5f); // Scale knockback with damage

                targetRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            }
        }

        private void PlayHitEffect(Vector3 position)
        {
            // Create hit effect (would use object pool in production)
            GameObject hitEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitEffect.transform.position = position;
            hitEffect.transform.localScale = Vector3.one * 0.5f;

            var renderer = hitEffect.GetComponent<Renderer>();
            renderer.material.color = Color.red;

            Object.Destroy(hitEffect, 0.5f);

            // Play hit sound
            // This would integrate with audio system
        }

        public override string GetStateName()
        {
            return $"Attacking (Combo: {comboCount})";
        }
    }
}
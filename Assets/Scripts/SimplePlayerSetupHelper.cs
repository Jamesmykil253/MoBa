using UnityEngine;
using MOBA.Debugging;
using MOBA.Abilities;

namespace MOBA
{
    /// <summary>
    /// Simple helper to setup player objects
    /// </summary>
    public class SimplePlayerSetupHelper : MonoBehaviour
    {
        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General, string actor = null)
        {
            return new GameDebugContext(
                GameDebugCategory.Player,
                GameDebugSystemTag.Player,
                mechanic,
                subsystem: nameof(SimplePlayerSetupHelper),
                actor: actor);
        }
        [ContextMenu("Setup All Players In Scene")]
        public void SetupAllPlayersInScene()
        {
            var players = FindObjectsByType<SimplePlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            int setupCount = 0;
            
            foreach (var player in players)
            {
                SetupPlayerObject(player.gameObject);
                setupCount++;
            }
            
            GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization),
                "Player objects setup invoked.",
                ("Count", setupCount));
        }

        private void SetupPlayerObject(GameObject playerObj)
        {
            // Ensure Rigidbody exists
            if (playerObj.GetComponent<Rigidbody>() == null)
            {
                playerObj.AddComponent<Rigidbody>();
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization, playerObj.name),
                    "Added Rigidbody component.");
            }

            // Ensure Collider exists
            if (playerObj.GetComponent<Collider>() == null)
            {
                playerObj.AddComponent<CapsuleCollider>();
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization, playerObj.name),
                    "Added CapsuleCollider component.");
            }

            // Ensure Enhanced ability stack exists
            var enhanced = playerObj.GetComponent<EnhancedAbilitySystem>();
            if (enhanced == null)
            {
                enhanced = playerObj.AddComponent<EnhancedAbilitySystem>();
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization, playerObj.name),
                    "Added EnhancedAbilitySystem component.");
            }

            var legacyAbility = playerObj.GetComponent<SimpleAbilitySystem>();
            if (legacyAbility == null)
            {
                legacyAbility = playerObj.AddComponent<SimpleAbilitySystem>();
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization, playerObj.name),
                    "Added SimpleAbilitySystem facade component.");
            }

            legacyAbility.SynchroniseAbilities();

            GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization, playerObj.name),
                "Player setup complete.");
        }
    }
}

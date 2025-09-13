using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Simple helper to setup player objects
    /// </summary>
    public class SimplePlayerSetupHelper : MonoBehaviour
    {
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
            
            Debug.Log($"[SimplePlayerSetupHelper] Setup {setupCount} player objects");
        }

        private void SetupPlayerObject(GameObject playerObj)
        {
            // Ensure Rigidbody exists
            if (playerObj.GetComponent<Rigidbody>() == null)
            {
                playerObj.AddComponent<Rigidbody>();
                Debug.Log($"Added Rigidbody to {playerObj.name}");
            }

            // Ensure Collider exists
            if (playerObj.GetComponent<Collider>() == null)
            {
                playerObj.AddComponent<CapsuleCollider>();
                Debug.Log($"Added CapsuleCollider to {playerObj.name}");
            }

            // Ensure SimpleAbilitySystem exists
            if (playerObj.GetComponent<SimpleAbilitySystem>() == null)
            {
                playerObj.AddComponent<SimpleAbilitySystem>();
                Debug.Log($"Added SimpleAbilitySystem to {playerObj.name}");
            }

            Debug.Log($"[SimplePlayerSetupHelper] Setup complete for {playerObj.name}");
        }
    }
}

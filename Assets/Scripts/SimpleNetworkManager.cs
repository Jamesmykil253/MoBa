using UnityEngine;
using Unity.Netcode;

namespace MOBA.Networking
{
    /// <summary>
    /// Simple network manager for basic multiplayer
    /// </summary>
    public class SimpleNetworkManager : MonoBehaviour
    {
        [Header("Network Settings")]
        public bool isHost = false;
        public int maxPlayers = 10;
        
        void Start()
        {
            if (isHost)
            {
                StartHost();
            }
            else
            {
                StartClient();
            }
        }
        
        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("Started as Host");
        }
        
        public void StartClient()
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Started as Client");
        }
        
        public void StartServer()
        {
            NetworkManager.Singleton.StartServer();
            Debug.Log("Started as Server");
        }
        
        public void Disconnect()
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Disconnected");
        }
        
        private string GetNetworkMode()
        {
            if (NetworkManager.Singleton.IsHost) return "Host";
            if (NetworkManager.Singleton.IsServer) return "Server";
            return "Client";
        }
        
        void OnGUI()
        {
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Host")) StartHost();
                if (GUILayout.Button("Client")) StartClient();
                if (GUILayout.Button("Server")) StartServer();
            }
            else
            {
                if (GUILayout.Button("Disconnect")) Disconnect();
                
                GUILayout.Label($"Mode: {GetNetworkMode()}");
                GUILayout.Label($"Connected Players: {NetworkManager.Singleton.ConnectedClientsList.Count}");
            }
        }
    }
}

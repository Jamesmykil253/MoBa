using UnityEngine;
using Unity.Netcode;

namespace MOBA.Networking
{
    /// <summary>
    /// Base interface for network manager components
    /// Provides common functionality and access to the main network manager
    /// </summary>
    public interface INetworkManagerComponent
    {
        bool IsInitialized { get; }
        void Initialize(ProductionNetworkManager networkManager);
        void Shutdown();
    }

    /// <summary>
    /// Base class for network manager components
    /// Provides shared functionality and access to Unity Netcode
    /// </summary>
    public abstract class NetworkManagerComponent : MonoBehaviour, INetworkManagerComponent
    {
        protected ProductionNetworkManager networkManager;
        protected NetworkManager netcode;
        
        public bool IsInitialized { get; private set; }

        public virtual void Initialize(ProductionNetworkManager networkManager)
        {
            this.networkManager = networkManager;
            this.netcode = NetworkManager.Singleton;
            IsInitialized = true;
            OnInitialized();
        }

        public virtual void Shutdown()
        {
            OnShutdown();
            IsInitialized = false;
        }

        protected virtual void OnInitialized() { }
        protected virtual void OnShutdown() { }

        protected virtual void OnDestroy()
        {
            if (IsInitialized)
            {
                Shutdown();
            }
        }
    }
}
using UnityEngine;
using Unity.Netcode;
using MOBA.Debugging;
using MOBA.Networking;

namespace MOBA.GameManagement
{
    /// <summary>
    /// Manages game events, event coordination, and system-wide event handling.
    /// Centralizes event dispatching and provides event lifecycle management.
    /// </summary>
    public class GameEventManager : GameManagerComponent
    {
        #region Configuration
        
        [Header("Event Settings")]
        [SerializeField, Tooltip("Enable detailed event logging")]
        private bool logEvents = true;
        
        [SerializeField, Tooltip("Maximum event queue size")]
        private int maxEventQueueSize = 100;
        
        [SerializeField, Tooltip("Process events per frame")]
        private int eventsPerFrame = 5;
        
        #endregion
        
        #region Event Definitions
        
        /// <summary>
        /// Game event types for categorization and filtering
        /// </summary>
        public enum GameEventType
        {
            GameStart,
            GameEnd,
            ScoreChanged,
            PlayerJoined,
            PlayerLeft,
            TimeUpdate,
            SystemError,
            Configuration,
            Network,
            UI,
            Spawn,
            Service
        }
        
        /// <summary>
        /// Game event data structure
        /// </summary>
        [System.Serializable]
        public struct GameEvent
        {
            public GameEventType eventType;
            public string eventName;
            public float timestamp;
            public object eventData;
            public System.Action<object> callback;
            
            public GameEvent(GameEventType type, string name, object data = null, System.Action<object> callback = null)
            {
                this.eventType = type;
                this.eventName = name;
                this.timestamp = Time.time;
                this.eventData = data;
                this.callback = callback;
            }
        }
        
        #endregion
        
        #region Event Queue
        
        private System.Collections.Generic.Queue<GameEvent> eventQueue;
        private System.Collections.Generic.Dictionary<GameEventType, System.Action<GameEvent>> eventHandlers;
        private System.Collections.Generic.Dictionary<string, System.Action<GameEvent>> namedEventHandlers;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current number of queued events
        /// </summary>
        public int QueuedEventCount => eventQueue?.Count ?? 0;
        
        /// <summary>
        /// Whether event queue is full
        /// </summary>
        public bool IsQueueFull => QueuedEventCount >= maxEventQueueSize;
        
        /// <summary>
        /// Number of registered event type handlers
        /// </summary>
        public int RegisteredTypeHandlerCount => eventHandlers?.Count ?? 0;
        
        /// <summary>
        /// Number of registered named event handlers
        /// </summary>
        public int RegisteredNamedHandlerCount => namedEventHandlers?.Count ?? 0;
        
        #endregion
        
        #region Global Events
        
        /// <summary>
        /// Global event raised for all game events. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<GameEvent> OnGameEvent;
        
        /// <summary>
        /// Raised when game starts. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action OnGameStarted;
        
        /// <summary>
        /// Raised when game ends. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int> OnGameEnded; // winningTeam
        
        /// <summary>
        /// Raised when player joins. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<ulong> OnPlayerJoined; // clientId
        
        /// <summary>
        /// Raised when player leaves. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<ulong> OnPlayerLeft; // clientId
        
        /// <summary>
        /// Raised when system error occurs. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<string> OnSystemError; // errorMessage
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(SimpleGameManager gameManager)
        {
            base.Initialize(gameManager);
            
            InitializeEventSystem();
            SetupEventHandlers();
            RegisterForExternalEvents();
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Events),
                    "Event manager initialized.",
                    ("MaxQueueSize", maxEventQueueSize),
                    ("EventsPerFrame", eventsPerFrame));
            }
        }
        
        public override void Shutdown()
        {
            UnregisterFromExternalEvents();
            CleanupEventHandlers();
            ClearEventQueue();
            
            // Clear global events
            OnGameEvent = null;
            OnGameStarted = null;
            OnGameEnded = null;
            OnPlayerJoined = null;
            OnPlayerLeft = null;
            OnSystemError = null;
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Events),
                    "Event manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        public override void UpdateComponent()
        {
            ProcessEventQueue();
        }
        
        #endregion
        
        #region Event System Setup
        
        /// <summary>
        /// Initialize event system components
        /// </summary>
        private void InitializeEventSystem()
        {
            eventQueue = new System.Collections.Generic.Queue<GameEvent>();
            eventHandlers = new System.Collections.Generic.Dictionary<GameEventType, System.Action<GameEvent>>();
            namedEventHandlers = new System.Collections.Generic.Dictionary<string, System.Action<GameEvent>>();
        }
        
        /// <summary>
        /// Setup internal event handlers
        /// </summary>
        private void SetupEventHandlers()
        {
            // Register for specific event types
            RegisterEventHandler(GameEventType.GameStart, HandleGameStartEvent);
            RegisterEventHandler(GameEventType.GameEnd, HandleGameEndEvent);
            RegisterEventHandler(GameEventType.PlayerJoined, HandlePlayerJoinedEvent);
            RegisterEventHandler(GameEventType.PlayerLeft, HandlePlayerLeftEvent);
            RegisterEventHandler(GameEventType.SystemError, HandleSystemErrorEvent);
        }
        
        /// <summary>
        /// Register for external events from other components
        /// </summary>
        private void RegisterForExternalEvents()
        {
            if (simpleGameManager == null)
            {
                return;
            }
            
            // Register for service events
            var serviceManager = simpleGameManager.GetServiceManager();
            if (serviceManager != null)
            {
                serviceManager.OnScoreChanged += HandleScoreChanged;
            }
            
            // Register for network events
            var networkManager = simpleGameManager.GetNetworkManager();
            if (networkManager != null)
            {
                networkManager.OnNetworkTimeChanged += HandleNetworkTimeChanged;
                networkManager.OnNetworkGameStateChanged += HandleNetworkGameStateChanged;
            }
            
            // Register for ProductionNetworkManager events
            var productionNetworkManager = ProductionNetworkManager.Instance;
            if (productionNetworkManager != null)
            {
                productionNetworkManager.OnPlayerConnected += HandlePlayerConnected;
                productionNetworkManager.OnPlayerDisconnected += HandlePlayerDisconnected;
            }
        }
        
        /// <summary>
        /// Unregister from external events
        /// </summary>
        private void UnregisterFromExternalEvents()
        {
            if (simpleGameManager == null)
            {
                return;
            }
            
            // Unregister from service events
            var serviceManager = simpleGameManager.GetServiceManager();
            if (serviceManager != null)
            {
                serviceManager.OnScoreChanged -= HandleScoreChanged;
            }
            
            // Unregister from network events
            var networkManager = simpleGameManager.GetNetworkManager();
            if (networkManager != null)
            {
                networkManager.OnNetworkTimeChanged -= HandleNetworkTimeChanged;
                networkManager.OnNetworkGameStateChanged -= HandleNetworkGameStateChanged;
            }
            
            // Unregister from ProductionNetworkManager events
            var productionNetworkManager = ProductionNetworkManager.Instance;
            if (productionNetworkManager != null)
            {
                productionNetworkManager.OnPlayerConnected -= HandlePlayerConnected;
                productionNetworkManager.OnPlayerDisconnected -= HandlePlayerDisconnected;
            }
        }
        
        /// <summary>
        /// Cleanup event handlers
        /// </summary>
        private void CleanupEventHandlers()
        {
            eventHandlers?.Clear();
            namedEventHandlers?.Clear();
        }
        
        #endregion
        
        #region Event Registration
        
        /// <summary>
        /// Register event handler for specific event type
        /// </summary>
        /// <param name="eventType">Event type to handle</param>
        /// <param name="handler">Handler callback</param>
        public void RegisterEventHandler(GameEventType eventType, System.Action<GameEvent> handler)
        {
            if (handler == null)
            {
                return;
            }
            
            if (eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType] += handler;
            }
            else
            {
                eventHandlers[eventType] = handler;
            }
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Events),
                    "Event handler registered.",
                    ("EventType", eventType),
                    ("Handler", handler.Method.Name));
            }
        }
        
        /// <summary>
        /// Unregister event handler for specific event type
        /// </summary>
        /// <param name="eventType">Event type to stop handling</param>
        /// <param name="handler">Handler callback to remove</param>
        public void UnregisterEventHandler(GameEventType eventType, System.Action<GameEvent> handler)
        {
            if (handler == null || !eventHandlers.ContainsKey(eventType))
            {
                return;
            }
            
            eventHandlers[eventType] -= handler;
            
            if (eventHandlers[eventType] == null)
            {
                eventHandlers.Remove(eventType);
            }
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Events),
                    "Event handler unregistered.",
                    ("EventType", eventType),
                    ("Handler", handler.Method.Name));
            }
        }
        
        /// <summary>
        /// Register named event handler
        /// </summary>
        /// <param name="eventName">Event name to handle</param>
        /// <param name="handler">Handler callback</param>
        public void RegisterNamedEventHandler(string eventName, System.Action<GameEvent> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null)
            {
                return;
            }
            
            if (namedEventHandlers.ContainsKey(eventName))
            {
                namedEventHandlers[eventName] += handler;
            }
            else
            {
                namedEventHandlers[eventName] = handler;
            }
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Events),
                    "Named event handler registered.",
                    ("EventName", eventName),
                    ("Handler", handler.Method.Name));
            }
        }
        
        /// <summary>
        /// Unregister named event handler
        /// </summary>
        /// <param name="eventName">Event name to stop handling</param>
        /// <param name="handler">Handler callback to remove</param>
        public void UnregisterNamedEventHandler(string eventName, System.Action<GameEvent> handler)
        {
            if (string.IsNullOrEmpty(eventName) || handler == null || !namedEventHandlers.ContainsKey(eventName))
            {
                return;
            }
            
            namedEventHandlers[eventName] -= handler;
            
            if (namedEventHandlers[eventName] == null)
            {
                namedEventHandlers.Remove(eventName);
            }
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Events),
                    "Named event handler unregistered.",
                    ("EventName", eventName),
                    ("Handler", handler.Method.Name));
            }
        }
        
        #endregion
        
        #region Event Dispatching
        
        /// <summary>
        /// Queue event for processing
        /// </summary>
        /// <param name="gameEvent">Event to queue</param>
        public void QueueEvent(GameEvent gameEvent)
        {
            if (IsQueueFull)
            {
                if (logEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Events),
                        "Event queue is full, dropping event.",
                        ("EventType", gameEvent.eventType),
                        ("EventName", gameEvent.eventName));
                }
                return;
            }
            
            eventQueue.Enqueue(gameEvent);
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Events),
                    "Event queued.",
                    ("EventType", gameEvent.eventType),
                    ("EventName", gameEvent.eventName),
                    ("QueueSize", eventQueue.Count));
            }
        }
        
        /// <summary>
        /// Queue event with parameters
        /// </summary>
        /// <param name="eventType">Type of event</param>
        /// <param name="eventName">Name of event</param>
        /// <param name="eventData">Optional event data</param>
        /// <param name="callback">Optional callback</param>
        public void QueueEvent(GameEventType eventType, string eventName, object eventData = null, System.Action<object> callback = null)
        {
            var gameEvent = new GameEvent(eventType, eventName, eventData, callback);
            QueueEvent(gameEvent);
        }
        
        /// <summary>
        /// Process immediate event (bypass queue)
        /// </summary>
        /// <param name="gameEvent">Event to process immediately</param>
        public void ProcessImmediateEvent(GameEvent gameEvent)
        {
            ProcessEvent(gameEvent);
        }
        
        /// <summary>
        /// Process event queue
        /// </summary>
        private void ProcessEventQueue()
        {
            int processed = 0;
            
            while (eventQueue.Count > 0 && processed < eventsPerFrame)
            {
                var gameEvent = eventQueue.Dequeue();
                ProcessEvent(gameEvent);
                processed++;
            }
        }
        
        /// <summary>
        /// Process individual event
        /// </summary>
        /// <param name="gameEvent">Event to process</param>
        private void ProcessEvent(GameEvent gameEvent)
        {
            // Invoke global event
            OnGameEvent?.Invoke(gameEvent);
            
            // Invoke type-specific handlers
            if (eventHandlers.ContainsKey(gameEvent.eventType))
            {
                eventHandlers[gameEvent.eventType]?.Invoke(gameEvent);
            }
            
            // Invoke named handlers
            if (!string.IsNullOrEmpty(gameEvent.eventName) && namedEventHandlers.ContainsKey(gameEvent.eventName))
            {
                namedEventHandlers[gameEvent.eventName]?.Invoke(gameEvent);
            }
            
            // Invoke callback if provided
            gameEvent.callback?.Invoke(gameEvent.eventData);
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Events),
                    "Event processed.",
                    ("EventType", gameEvent.eventType),
                    ("EventName", gameEvent.eventName),
                    ("Timestamp", gameEvent.timestamp));
            }
        }
        
        #endregion
        
        #region Event Queue Management
        
        /// <summary>
        /// Clear all queued events
        /// </summary>
        public void ClearEventQueue()
        {
            int clearedCount = eventQueue?.Count ?? 0;
            eventQueue?.Clear();
            
            if (logEvents && clearedCount > 0)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Events),
                    "Event queue cleared.",
                    ("ClearedCount", clearedCount));
            }
        }
        
        /// <summary>
        /// Get event queue statistics
        /// </summary>
        /// <returns>Queue statistics string</returns>
        public string GetQueueStatistics()
        {
            return $"Queued: {QueuedEventCount}/{maxEventQueueSize}, " +
                   $"Type Handlers: {RegisteredTypeHandlerCount}, " +
                   $"Named Handlers: {RegisteredNamedHandlerCount}";
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle game start event
        /// </summary>
        /// <param name="gameEvent">Game event data</param>
        private void HandleGameStartEvent(GameEvent gameEvent)
        {
            OnGameStarted?.Invoke();
        }
        
        /// <summary>
        /// Handle game end event
        /// </summary>
        /// <param name="gameEvent">Game event data</param>
        private void HandleGameEndEvent(GameEvent gameEvent)
        {
            if (gameEvent.eventData is int winningTeam)
            {
                OnGameEnded?.Invoke(winningTeam);
            }
        }
        
        /// <summary>
        /// Handle player joined event
        /// </summary>
        /// <param name="gameEvent">Game event data</param>
        private void HandlePlayerJoinedEvent(GameEvent gameEvent)
        {
            if (gameEvent.eventData is ulong clientId)
            {
                OnPlayerJoined?.Invoke(clientId);
            }
        }
        
        /// <summary>
        /// Handle player left event
        /// </summary>
        /// <param name="gameEvent">Game event data</param>
        private void HandlePlayerLeftEvent(GameEvent gameEvent)
        {
            if (gameEvent.eventData is ulong clientId)
            {
                OnPlayerLeft?.Invoke(clientId);
            }
        }
        
        /// <summary>
        /// Handle system error event
        /// </summary>
        /// <param name="gameEvent">Game event data</param>
        private void HandleSystemErrorEvent(GameEvent gameEvent)
        {
            if (gameEvent.eventData is string errorMessage)
            {
                OnSystemError?.Invoke(errorMessage);
            }
        }
        
        #endregion
        
        #region External Event Handlers
        
        /// <summary>
        /// Handle score changed from service manager
        /// </summary>
        /// <param name="team">Team that scored</param>
        /// <param name="score">New score</param>
        private void HandleScoreChanged(int team, int score)
        {
            var scoreData = new { team, score };
            QueueEvent(GameEventType.ScoreChanged, "ScoreChanged", scoreData);
        }
        
        /// <summary>
        /// Handle network time changed
        /// </summary>
        /// <param name="previous">Previous time</param>
        /// <param name="current">Current time</param>
        private void HandleNetworkTimeChanged(float previous, float current)
        {
            var timeData = new { previous, current };
            QueueEvent(GameEventType.TimeUpdate, "NetworkTimeChanged", timeData);
        }
        
        /// <summary>
        /// Handle network game state changed
        /// </summary>
        /// <param name="previous">Previous state</param>
        /// <param name="current">Current state</param>
        private void HandleNetworkGameStateChanged(bool previous, bool current)
        {
            var stateData = new { previous, current };
            GameEventType eventType = current ? GameEventType.GameStart : GameEventType.GameEnd;
            QueueEvent(eventType, "NetworkGameStateChanged", stateData);
        }
        
        /// <summary>
        /// Handle player connected
        /// </summary>
        /// <param name="clientId">Connected client ID</param>
        private void HandlePlayerConnected(ulong clientId)
        {
            QueueEvent(GameEventType.PlayerJoined, "PlayerConnected", clientId);
        }
        
        /// <summary>
        /// Handle player disconnected
        /// </summary>
        /// <param name="clientId">Disconnected client ID</param>
        private void HandlePlayerDisconnected(ulong clientId)
        {
            QueueEvent(GameEventType.PlayerLeft, "PlayerDisconnected", clientId);
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Set maximum event queue size
        /// </summary>
        /// <param name="maxSize">Maximum queue size</param>
        public void SetMaxEventQueueSize(int maxSize)
        {
            maxEventQueueSize = Mathf.Max(1, maxSize);
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Max event queue size updated.",
                    ("MaxSize", maxEventQueueSize));
            }
        }
        
        /// <summary>
        /// Set events processed per frame
        /// </summary>
        /// <param name="eventsPerFrame">Events to process per frame</param>
        public void SetEventsPerFrame(int eventsPerFrame)
        {
            this.eventsPerFrame = Mathf.Max(1, eventsPerFrame);
            
            if (logEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Events per frame updated.",
                    ("EventsPerFrame", this.eventsPerFrame));
            }
        }
        
        /// <summary>
        /// Set event logging enabled
        /// </summary>
        /// <param name="enabled">Whether to log events</param>
        public void SetEventLogging(bool enabled)
        {
            logEvents = enabled;
        }
        
        #endregion
    }
}
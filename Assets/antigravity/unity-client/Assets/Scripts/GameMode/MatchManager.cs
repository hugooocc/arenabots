using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using Antigravity.Player;
using Antigravity.CameraSystem;

namespace Antigravity.GameMode
{
    public class MatchManager : MonoBehaviour
    {
        private UIDocument countdownUIDocument;
        private SinglePlayerManager singlePlayerManager;
        
        private Label countdownLabel;
        private VisualElement countdownContainer;

        // Stored so we can unsubscribe it later
        private System.Action onNetworkConnectedHandler;

        public enum CameraState { FOLLOW, SPECTATE, OVERVIEW }
        private CameraState currentViewState = CameraState.FOLLOW;
        private Transform cameraFollowTarget;

        private void UpdateCameraLogic()
        {
            if (Camera.main == null) return;
            var cameraFollow = Camera.main.GetComponent<CameraFollow>();
            
            switch (currentViewState)
            {
                case CameraState.FOLLOW:
                    // Local player usually handled by CameraFollow "target" already set
                    break;

                case CameraState.SPECTATE:
                    if (cameraFollowTarget == null) FindNewSpectatorTarget();
                    if (cameraFollow != null) cameraFollow.target = cameraFollowTarget;
                    break;

                case CameraState.OVERVIEW:
                    if (cameraFollow != null) cameraFollow.target = null;
                    Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(0, 0, -10), Time.deltaTime);
                    break;
            }
        }

        private void FindNewSpectatorTarget()
        {
            var players = Object.FindObjectsByType<Antigravity.Player.PlayerHealth>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.currentHealth > 0)
                {
                    cameraFollowTarget = p.transform;
                    Debug.Log($"[Spectator] Siguiendo ahora a {p.gameObject.name}");
                    return;
                }
            }
            // No one alive? Go to Overview
            currentViewState = CameraState.OVERVIEW;
        }

        public void SetCameraState(CameraState state, Transform target = null)
        {
            currentViewState = state;
            cameraFollowTarget = target;
        }

        private void OnGUI()
        {
            // Persistent debug overlay using legacy GUI for guaranteed visibility
            if (Antigravity.Auth.GameSession.CurrentGameId != "singleplayer")
            {
                var allMovements = FindObjectsByType<Antigravity.Player.PlayerMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                int activeLocalCount = 0;
                foreach(var m in allMovements) {
                    // Solo contamos el local real (el que no tiene NetworkPlayer)
                    if (m.enabled && m.GetComponent<Antigravity.Network.NetworkPlayer>() == null) activeLocalCount++;
                }

                var remotePlayers = FindObjectsByType<Antigravity.Network.NetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                string remotePositions = "";
                foreach (var rp in remotePlayers) {
                    remotePositions += $"\n  - {rp.username} ({rp.userId}): {rp.transform.position}";
                }

                PlayerMovement localPlayerObj = null;
                foreach(var m in allMovements) {
                    if (m.enabled && m.GetComponent<Antigravity.Network.NetworkPlayer>() == null) {
                        localPlayerObj = m;
                        break;
                    }
                }
                string localPosStr = localPlayerObj != null ? localPlayerObj.transform.position.ToString() : "N/A";

                string debugInfo = $"[ARENA BOTS MULTI-DEBUG]\n" +
                                   $"Local Players: {activeLocalCount} ({localPosStr})\n" +
                                   $"Remote Players: {remotePlayers.Length}{remotePositions}\n" +
                                   $"User ID: {Antigravity.Auth.GameSession.UserId}\n" +
                                   $"Errors: {string.Join(" | ", errorHistory)}";

                GUI.color = Color.yellow;
                GUI.Label(new Rect(10, 10, 600, 100), debugInfo);
            }
        }

    private void Update()
    {
        // Camera logic
        UpdateCameraLogic();

        // Periodic check to log state to console
        if (Time.frameCount % 300 == 0 && Antigravity.Auth.GameSession.CurrentGameId != "singleplayer") {
            var localPlayers = FindObjectsByType<Antigravity.Player.PlayerMovement>(FindObjectsSortMode.None);
            var remotePlayers = FindObjectsByType<Antigravity.Network.NetworkPlayer>(FindObjectsSortMode.None);
            Debug.Log($"[DIAGNOSTIC] Clients in Scene: Local={localPlayers.Length}, Remote={remotePlayers.Length}");
        }

        // CONTROL DE ESPECTADOR
        if (Antigravity.Auth.GameSession.CurrentGameId != "singleplayer")
        {
            CheckSpectatorMode();
        }
    }

    private void CheckSpectatorMode()
    {
        // Buscamos al jugador local
        var localPlayer = FindLocalPlayer();
        if (localPlayer != null && localPlayer.TryGetComponent<PlayerHealth>(out var health))
        {
            if (health.currentHealth <= 0)
            {
                // Si el local está muerto, buscamos un aliado vivo
                var camera = FindAnyObjectByType<Antigravity.CameraSystem.CameraFollow>();
                if (camera != null)
                {
                    var remotes = FindObjectsByType<Antigravity.Network.NetworkPlayer>(FindObjectsSortMode.None);
                    foreach (var r in remotes)
                    {
                        // En multijugador, si hay remotes, seguimos al primero que encontremos
                        if (camera.target != r.transform)
                        {
                            Debug.Log($"[MatchManager] Espectador: Cambiando cámara a {r.username}");
                            camera.target = r.transform;
                            break;
                        }
                    }
                }
            }
        }
    }

    private PlayerMovement FindLocalPlayer()
    {
        var allpm = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var pm in allpm)
        {
            if (pm.GetComponent<Antigravity.Network.NetworkPlayer>() == null) return pm;
        }
        return null;
    }

        private System.Collections.Generic.List<string> errorHistory = new System.Collections.Generic.List<string>();

        private void Start()
        {
            // PRO-TIP: Keep WebSocket alive even if window loses focus
            Application.runInBackground = true;

            // CAPTURE ERRORS FOR DIAGNOSIS
            Application.logMessageReceived += (condition, stackTrace, type) => {
                if (type == LogType.Error || type == LogType.Exception) {
                    if (errorHistory.Count < 5) errorHistory.Add(condition);
                }
            };

            // ¡Detectamos los componentes automáticamente para que no tengas que arrastrar nada!
            countdownUIDocument = GetComponent<UIDocument>();
            singlePlayerManager = UnityEngine.Object.FindAnyObjectByType<SinglePlayerManager>();

            if (countdownUIDocument != null)
            {
                var root = countdownUIDocument.rootVisualElement;
                if (root != null)
                {
                    countdownLabel = root.Q<Label>("countdown-label");
                    countdownContainer = root.Q<VisualElement>("countdown-container");
                }
            }

            // In single player, we start immediately. In multiplayer, we wait for the server.
            if (Antigravity.Auth.GameSession.CurrentGameId == "singleplayer")
            {
                StartCoroutine(StartMatchCountdown());
            }
            else
            {
                // Silenciar el spawner local en multijugador para que no haya duplicados
                if (singlePlayerManager != null) {
                    singlePlayerManager.enabled = false;
                    Debug.Log("[MatchManager] SinglePlayerManager desactivado en modo multijugador.");
                }

                if (countdownLabel != null) 
                {
                    countdownLabel.style.fontSize = 60; // Texto más pequeño para el mensaje de espera
                    countdownLabel.text = "ESPERANDO JUGADORES...";
                    if (countdownContainer != null) countdownContainer.style.display = DisplayStyle.Flex;
                }

                // 1. MULTIPLAYER INITIALIZATION (MUST BE FIRST)
                Debug.Log("[MatchManager] Initializing Multiplayer Spawner...");
                GameObject spawnerGo = new GameObject("MultiplayerSpawner");
                var spawner = spawnerGo.AddComponent<Antigravity.Network.MultiplayerSpawner>();
                spawner.remotePlayerPrefab = Resources.Load<GameObject>("Prefabs/RemotePlayer");
                
                if (spawner.remotePlayerPrefab == null) {
                    Debug.LogError("[MatchManager] CRITICAL: RemotePlayer prefab NOT FOUND in Resources/Prefabs/RemotePlayer");
                } else {
                    Debug.Log("[MatchManager] RemotePlayer prefab loaded successfully.");
                }

                // 2. HANDSHAKE: tell the server we are in the Arena.
                // IMPORTANT: we MUST wait for the WebSocket to be open before sending.
                var nm = Antigravity.Shooting.NetworkManager.Instance;
                if (nm != null)
                {
                    nm.OnMessageReceived += HandleNetworkMessage;

                    if (nm.IsConnected)
                    {
                        SendPlayerReady();
                    }
                    else
                    {
                        onNetworkConnectedHandler = () => {
                            SendPlayerReady();
                            nm.OnConnected -= onNetworkConnectedHandler;
                        };
                        nm.OnConnected += onNetworkConnectedHandler;
                    }
                }

                // 3. IDENTIFY LOCAL PLAYER (HARDENED)
                PlayerMovement[] allMovements = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
                PlayerMovement localPlayer = null;
                int localCount = 0;

                foreach (var pm in allMovements)
                {
                    // Un jugador local de verdad es aquel que tiene PlayerMovement PERO NO NetworkPlayer
                    if (pm.GetComponent<Antigravity.Network.NetworkPlayer>() == null)
                    {
                        if (localPlayer == null) 
                        {
                            localPlayer = pm;
                        }
                        else 
                        {
                            // ¡DUPLICADO DETECTADO! Lo borramos para evitar interferencias
                            Debug.LogWarning("[MatchManager] ¡BORRANDO JUGADOR DUPLICADO! " + pm.gameObject.name);
                            DestroyImmediate(pm.gameObject);
                            continue;
                        }
                        localCount++;
                    }
                }

                if (localPlayer != null)
                {
                    localPlayer.gameObject.name = "LOCAL_PLAYER_" + Antigravity.Auth.GameSession.Username;
                    localPlayer.transform.position = new Vector3(-3f, 0, 0); // Start far left
                    
                    // Asegurarnos de que no tenga ya un sync (por si acaso)
                    if (localPlayer.GetComponent<Antigravity.Network.PlayerNetworkSync>() == null)
                    {
                        localPlayer.gameObject.AddComponent<Antigravity.Network.PlayerNetworkSync>();
                    }
                    Debug.Log($"[DIAGNOSTIC] Local player identified and synced: {localPlayer.gameObject.name} at {localPlayer.transform.position}");
                }
                else {
                    Debug.LogError("[MatchManager] CRITICAL: Local PlayerMovement NOT FOUND in scene! Movements available: " + allMovements.Length);
                }
                
                // Keep players disabled while waiting for connection
                Antigravity.Player.PlayerMovement[] players = FindObjectsByType<Antigravity.Player.PlayerMovement>(FindObjectsSortMode.None);
                foreach(var p in players) p.canMove = false;
            }
        }

        private void OnDestroy()
        {
            var nm = Antigravity.Shooting.NetworkManager.Instance;
            if (nm != null)
            {
                nm.OnMessageReceived -= HandleNetworkMessage;
                if (onNetworkConnectedHandler != null)
                    nm.OnConnected -= onNetworkConnectedHandler;
            }
        }

        private void SendPlayerReady()
        {
            var nm = Antigravity.Shooting.NetworkManager.Instance;
            if (nm == null) return;
            string username = Antigravity.Auth.GameSession.Username ?? "Jugador";
            string msg = $"{{\"tipo\":\"player_ready\",\"username\":\"{username}\"}}";
            Debug.Log("[MatchManager] Sending player_ready: " + msg);
            nm.SendMessage(msg);
        }

        private void HandleNetworkMessage(string rawMessage)
        {
            try {
                if (rawMessage.Contains("\"tipo\":\"game_tick\""))
                {
                    // Update global time from server for HUD interpolation
                    var tickData = JsonUtility.FromJson<ServerTickData>(rawMessage);
                    Antigravity.UI.InGameUIManager ui = Object.FindFirstObjectByType<Antigravity.UI.InGameUIManager>();
                    if (ui != null) ui.SyncServerTime(tickData.time);
                }
                else if (rawMessage.Contains("\"tipo\":\"jugador_muerto\""))
                {
                    var msg = JsonUtility.FromJson<Antigravity.Network.MoveMessage>(rawMessage);
                    if (msg.userId == Antigravity.Auth.GameSession.UserId) {
                        Debug.Log("[MatchManager] He muerto. Pasando a Modo Espectador.");
                        SetCameraState(CameraState.SPECTATE);
                    }
                }
                else if (rawMessage.Contains("\"tipo\":\"game_over\""))
                {
                    SetCameraState(CameraState.OVERVIEW);
                }
                else if (rawMessage.Contains("\"tipo\":\"start_countdown\""))
                {
                    StartCoroutine(StartMatchCountdown());
                }
            } catch (System.Exception e) {
                Debug.LogError("[MatchManager] Error parsing message: " + e.Message);
            }
        }

        [System.Serializable]
        private class ServerTickData {
            public string tipo;
            public int tick;
            public float time;
        }

        IEnumerator StartMatchCountdown()
        {
            // Find all players and disable their movement initially
            Antigravity.Player.PlayerMovement[] players = FindObjectsByType<Antigravity.Player.PlayerMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach(var p in players) if(p.enabled) p.canMove = false;

            // Show container
            if (countdownContainer != null) countdownContainer.style.display = DisplayStyle.Flex;

            // Countdown loop
            for (int i = 5; i > 0; i--)
            {
                if (countdownLabel != null) 
                {
                    countdownLabel.style.fontSize = 150; // Grande para los números
                    countdownLabel.text = i.ToString();
                }
                yield return new WaitForSeconds(1f);
            }

            if (countdownLabel != null)
            {
                countdownLabel.style.fontSize = 100; // Un poco más grande que la espera, pero menos que los números
                countdownLabel.text = "¡A LUCHAR!";
                
                // Active game timer in HUD
                Antigravity.UI.InGameUIManager ui = Object.FindFirstObjectByType<Antigravity.UI.InGameUIManager>();
                if (ui != null) ui.SetGameActive(true);

                yield return new WaitForSeconds(1f);
                if (countdownContainer != null) countdownContainer.style.display = DisplayStyle.None;
            }

            // Enable movement back ONLY for local player
            players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach(var p in players) 
            {
                // Solo activamos el movimiento si NO es un jugador de red
                if (p.GetComponent<Antigravity.Network.NetworkPlayer>() == null) {
                    p.canMove = true;
                }
            }

            // Notify server that we are ready (this triggers waves in MP)
            if (Antigravity.Auth.GameSession.CurrentGameId != "singleplayer")
            {
                if (Antigravity.Shooting.NetworkManager.Instance != null)
                {
                    Antigravity.Shooting.NetworkManager.Instance.SendMessage("{\"tipo\":\"countdown_finished\"}");
                }
            }

            // Start spawning bots if in singleplayer mode
            if (singlePlayerManager != null)
            {
                singlePlayerManager.StartMatch();
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using Antigravity.Player;

namespace Antigravity.GameMode
{
    public class MatchManager : MonoBehaviour
    {
        private UIDocument countdownUIDocument;
        private SinglePlayerManager singlePlayerManager;
        
        private Label countdownLabel;
        private VisualElement countdownContainer;

        private void Start()
        {
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
                if (countdownLabel != null) 
                {
                    countdownLabel.style.fontSize = 60; // Texto más pequeño para el mensaje de espera
                    countdownLabel.text = "ESPERANDO JUGADORES...";
                    if (countdownContainer != null) countdownContainer.style.display = DisplayStyle.Flex;
                }

                if (Antigravity.Shooting.NetworkManager.Instance != null)
                {
                    Antigravity.Shooting.NetworkManager.Instance.OnMessageReceived += HandleNetworkMessage;
                }

                // MULTIPLAYER INITIALIZATION
                Debug.Log("[MatchManager] Initializing Multiplayer Spawner...");
                GameObject spawnerGo = new GameObject("MultiplayerSpawner");
                var spawner = spawnerGo.AddComponent<Antigravity.Network.MultiplayerSpawner>();
                spawner.remotePlayerPrefab = Resources.Load<GameObject>("Prefabs/RemotePlayer");
                
                if (spawner.remotePlayerPrefab == null) {
                    Debug.LogError("[MatchManager] CRITICAL: RemotePlayer prefab NOT FOUND in Resources/Prefabs/RemotePlayer");
                } else {
                    Debug.Log("[MatchManager] RemotePlayer prefab loaded successfully.");
                }

                // Attach Sync to local player
                PlayerMovement localPlayer = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
                if (localPlayer != null)
                {
                    localPlayer.gameObject.AddComponent<Antigravity.Network.PlayerNetworkSync>();
                    Debug.Log("[MatchManager] Initialized multiplayer sync for LOCAL player.");
                }
                else {
                    Debug.LogWarning("[MatchManager] Local PlayerMovement NOT FOUND in scene!");
                }

                // HANDSHAKE: Tell the server we are ready to receive other players
                if (Antigravity.Shooting.NetworkManager.Instance != null) {
                    string username = Antigravity.Auth.GameSession.Username ?? "Jugador";
                    string playerReadyMsg = $"{{\"tipo\":\"player_ready\",\"username\":\"{username}\"}}";
                    Debug.Log("[MatchManager] Sending 'player_ready' to server: " + playerReadyMsg);
                    Antigravity.Shooting.NetworkManager.Instance.SendMessage(playerReadyMsg);
                }
                
                // Keep players disabled while waiting for connection
                PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
                foreach(var p in players) p.canMove = false;
            }
        }

        private void OnDestroy()
        {
            if (Antigravity.Shooting.NetworkManager.Instance != null)
            {
                Antigravity.Shooting.NetworkManager.Instance.OnMessageReceived -= HandleNetworkMessage;
            }
        }

        private void HandleNetworkMessage(string rawMessage)
        {
            try {
                Antigravity.Shooting.WSMessage msg = JsonUtility.FromJson<Antigravity.Shooting.WSMessage>(rawMessage);
                if (msg.tipo == "start_countdown")
                {
                    StartCoroutine(StartMatchCountdown());
                }
            } catch (System.Exception e) {
                Debug.LogError("[MatchManager] Error parsing message: " + e.Message);
            }
        }

        IEnumerator StartMatchCountdown()
        {
            // Find all players and disable their movement initially
            PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
            foreach(var p in players) p.canMove = false;

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

            // Enable movement back
            players = FindObjectsOfType<PlayerMovement>();
            foreach(var p in players) p.canMove = true;

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

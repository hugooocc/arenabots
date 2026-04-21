using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

using UnityEngine.SceneManagement;

namespace Antigravity.Shooting
{
    [Serializable]
    public class WSMessage
    {
        public string tipo;
        public string gameId;
    }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public string serverUrl => Antigravity.Config.AntigravityConfig.Instance != null 
            ? Antigravity.Config.AntigravityConfig.Instance.WsBaseUrl 
            : "ws://localhost:3000";
        private WebSocket websocket;

        public bool IsConnected => websocket != null && websocket.State == WebSocketState.Open;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnMessageReceived;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Arena")
            {
                if (!string.IsNullOrEmpty(Antigravity.Auth.GameSession.CurrentGameId) && 
                    !string.IsNullOrEmpty(Antigravity.Auth.GameSession.Token))
                {
                    // FIX: Don't reconnect if already connected to a game!
                    if (websocket == null || websocket.State != WebSocketState.Open) {
                        ConnectToGame(Antigravity.Auth.GameSession.CurrentGameId, Antigravity.Auth.GameSession.Token);
                    } else {
                        Debug.Log("[NetworkManager] Already connected. Maintaining session for Arena.");
                    }
                }
                else 
                {
                    Debug.LogWarning("Entered Arena but no GameSession Game ID or Token found. Offline mode?");
                }
            }
        }

        public async void ConnectToGame(string gameId, string token)
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                await websocket.Close();
            }

            string url = $"{serverUrl}?gameId={gameId}&token={token}";
            websocket = new WebSocket(url);

            websocket.OnOpen += () =>
            {
                Debug.Log("Connected to game server: " + gameId);
                OnConnected?.Invoke();
            };

            websocket.OnMessage += (bytes) =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("Received: " + message);
                
                WSMessage data = JsonUtility.FromJson<WSMessage>(message);
                if (data.tipo == "game_started")
                {
                    Debug.Log("Transitioning to Arena...");
                    SceneManager.LoadScene("Arena");
                }

                OnMessageReceived?.Invoke(message);
            };

            await websocket.Connect();
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (websocket != null)
            {
                websocket.DispatchMessageQueue();
            }
#endif
        }

        public async void SendMessage(string message)
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                await websocket.SendText(message);
            }
        }

        private async void OnApplicationQuit()
        {
            if (websocket != null)
            {
                await websocket.Close();
            }
        }
    }
}

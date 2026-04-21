using UnityEngine;

namespace Antigravity.Config
{
    public class AntigravityConfig : MonoBehaviour
    {
        public static AntigravityConfig Instance { get; private set; }

        [Header("Backend Settings")]
        [Tooltip("The public IP or hostname of your server (e.g., 123.123.123.123)")]
        public string serverIp = "localhost";
        
        [Tooltip("The port the gateway is listening on (default: 3000)")]
        public int serverPort = 3000;

        public string HttpBaseUrl => $"http://{serverIp}:{serverPort}/api";
        public string AuthBaseUrl => $"{HttpBaseUrl}/auth";
        public string WsBaseUrl => $"ws://{serverIp}:{serverPort}";

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
    }
}

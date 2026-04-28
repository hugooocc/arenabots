using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

namespace Antigravity.Auth
{
    [Serializable]
    public class AuthResponse
    {
        public string message;
        public string token;
        public UserData user;
        public string userId; // For register response
    }

    [Serializable]
    public class UserData
    {
        public string id;
        public string username;
    }

    [Serializable]
    public class AuthRequest
    {
        public string username;
        public string password;
    }

    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        private string baseUrl => Antigravity.Config.AntigravityConfig.Instance != null 
            ? Antigravity.Config.AntigravityConfig.Instance.AuthBaseUrl 
            : "http://localhost:3000/api/auth";

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

        public void Register(string username, string password, Action<AuthResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(PostRequest("/register", username, password, onSuccess, onError));
        }

        public void Login(string username, string password, Action<AuthResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(PostRequest("/login", username, password, onSuccess, onError));
        }

        private IEnumerator PostRequest(string endpoint, string username, string password, Action<AuthResponse> onSuccess, Action<string> onError)
        {
            AuthRequest requestData = new AuthRequest { username = username, password = password };
            string json = JsonUtility.ToJson(requestData);

            using (UnityWebRequest www = new UnityWebRequest(baseUrl + endpoint, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = 10; // 10 seconds timeout

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                else
                {
                    string errorMessage = www.downloadHandler.text;
                    if (string.IsNullOrEmpty(errorMessage)) errorMessage = www.error;
                    onError?.Invoke(errorMessage);
                }
            }
        }
    }
}
 
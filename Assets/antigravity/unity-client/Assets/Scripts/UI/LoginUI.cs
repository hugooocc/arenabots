using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Antigravity.Auth;

namespace Antigravity.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class LoginUI : MonoBehaviour
    {
        private TextField usernameInput;
        private TextField passwordInput;
        private Button loginButton;
        private Button registerButton;
        private Label statusText;

        private void Start()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("No UIDocument found on LoginUI!");
                return;
            }

            var root = uiDocument.rootVisualElement;

            usernameInput = root.Q<TextField>("username-input");
            passwordInput = root.Q<TextField>("password-input");
            loginButton = root.Q<Button>("login-button");
            registerButton = root.Q<Button>("register-button");
            statusText = root.Q<Label>("status-label");

            if (loginButton != null) loginButton.clicked += OnLoginClicked;
            if (registerButton != null) registerButton.clicked += OnRegisterClicked;
        }

        private void OnDestroy()
        {
            if (loginButton != null) loginButton.clicked -= OnLoginClicked;
            if (registerButton != null) registerButton.clicked -= OnRegisterClicked;
        }

        private void OnLoginClicked()
        {
            if (statusText != null) statusText.text = "Logging in...";
            AuthManager.Instance.Login(usernameInput.value, passwordInput.value, 
                OnLoginSuccess, OnAuthError);
        }

        private void OnRegisterClicked()
        {
            if (statusText != null) statusText.text = "Registering...";
            AuthManager.Instance.Register(usernameInput.value, passwordInput.value, 
                OnRegisterSuccess, OnAuthError);
        }

        private void OnLoginSuccess(AuthResponse response)
        {
            if (statusText != null) statusText.text = "Login successful! Loading Main Menu...";
            GameSession.Token = response.token;
            GameSession.Username = response.user.username;
            GameSession.UserId = response.user.id;
            
            Debug.Log($"Welcome {GameSession.Username}. Token: {GameSession.Token}");
            
            // Cargar la escena del Menú Principal
            SceneManager.LoadScene("MainMenu");
        }

        private void OnRegisterSuccess(AuthResponse response)
        {
            if (statusText != null) statusText.text = "Registration successful! You can now login.";
        }

        private void OnAuthError(string error)
        {
            if (statusText != null) statusText.text = "Error: " + error;
            Debug.LogError("Auth Error: " + error);
        }
    }
}

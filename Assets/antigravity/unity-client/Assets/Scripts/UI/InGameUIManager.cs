using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Antigravity.Player;
using Antigravity.Enemies;
using Antigravity.Auth;
using System.Collections;

namespace Antigravity.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class InGameUIManager : MonoBehaviour
    {
        private UIDocument uiDocument;
        private int mobsKilled = 0;
        private PlayerHealth targetPlayerHealth;
        
        private float timePlayed = 0f;
        private bool isGameActive = true;

        [Header("Scene Settings")]
        public string mainMenuSceneName = "MainMenu"; // cambiado a MainMenu según solicitud

        // HUD Elements
        private VisualElement gameHudLayer;
        private Label mobsKilledLabel;
        private Label timePlayedLabel;
        private ProgressBar healthBar;

        // End Game Elements
        private VisualElement endGameLayer;
        private Label finalStatsKills;
        private Label finalStatsTime;
        private Button restartButton;
        private Button mainMenuButton;

        // Pause Elements
        private VisualElement pauseInstance;
        private VisualElement pauseMenuInner;
        private Label pauseStatsKills;
        private Label pauseStatsTime;
        private Button pauseButton;
        private Button resumeButton;
        private Button quitButton;
        private bool isPaused = false;

        private VisualElement endGameInstance;
        private VisualElement hudInstance;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            // 1. Find HUD elements
            gameHudLayer = root.Q<VisualElement>("game-hud");
            mobsKilledLabel = root.Q<Label>("mobs-killed-label");
            timePlayedLabel = root.Q<Label>("time-played-label");
            healthBar = root.Q<ProgressBar>("health-bar");

            // 2. Find End Game elements
            endGameInstance = root.Q<VisualElement>("end-game-instance");
            endGameLayer = root.Q<VisualElement>("end-game-screen");
            finalStatsKills = root.Q<Label>("final-stats-kills");
            finalStatsTime = root.Q<Label>("final-stats-time");
            restartButton = root.Q<Button>("btn-restart");
            mainMenuButton = root.Q<Button>("btn-main-menu");

            // 3. Find Pause elements
            pauseInstance = root.Q<VisualElement>("pause-instance");
            pauseMenuInner = root.Q<VisualElement>("pause-menu");
            pauseStatsKills = root.Q<Label>("pause-stats-kills");
            pauseStatsTime = root.Q<Label>("pause-stats-time");
            pauseButton = root.Q<Button>("btn-pause");
            resumeButton = root.Q<Button>("btn-resume");
            quitButton = root.Q<Button>("btn-quit");

            hudInstance = root.Q<VisualElement>("hud-instance");
            gameHudLayer = root.Q<VisualElement>("game-hud");

            // Verify elements are found to avoid null refs
            if (mobsKilledLabel == null || healthBar == null || endGameLayer == null)
            {
                Debug.LogWarning("[InGameUIManager] UI elements not found. Please ensure GameHUD and EndGameScreen are added to the UIDocument.");
                return;
            }

            // Setup Buttons
            if (restartButton != null) {
                restartButton.clicked += RestartGame;
                Debug.Log("[InGameUIManager] Restart button linked.");
            }
            if (mainMenuButton != null) {
                mainMenuButton.clicked += GoToMainMenu;
                Debug.Log("[InGameUIManager] Main Menu button linked.");
            }
            
            if (pauseButton != null) {
                pauseButton.clicked += TogglePause;
                // Fallback for picking issues: use direct pointer event
                pauseButton.RegisterCallback<PointerDownEvent>(evt => {
                    Debug.Log("[InGameUIManager] Pause button PointerDown detected.");
                });
                Debug.Log("[InGameUIManager] Pause button linked.");
            } else {
                Debug.LogError("[InGameUIManager] CRITICAL: pauseButton NOT FOUND in hierarchy!");
            }

            if (resumeButton != null) {
                resumeButton.clicked += TogglePause;
                Debug.Log("[InGameUIManager] Resume button linked.");
            }
            if (quitButton != null) {
                quitButton.clicked += GoToMainMenu;
                Debug.Log("[InGameUIManager] Quit button linked.");
            }

            // Initial Visibility: We only toggle the Instance containers
            if (endGameInstance != null) {
                endGameInstance.style.display = DisplayStyle.None;
                // Pre-set inner layers to Flex so they show up when instance is shown
                if (endGameLayer != null) endGameLayer.style.display = DisplayStyle.Flex;
            }

            if (pauseInstance != null) {
                pauseInstance.style.display = DisplayStyle.None;
                // Pre-set inner layers to Flex
                if (pauseMenuInner != null) pauseMenuInner.style.display = DisplayStyle.Flex;
            }

            if (hudInstance != null) {
                hudInstance.style.display = DisplayStyle.Flex;
                if (gameHudLayer != null) gameHudLayer.style.display = DisplayStyle.Flex;
            }

            // Subscribe to Enemy Controller Global Event
            EnemyController.OnEnemyKilled += HandleEnemyKilled;

            // Subscriptions
            FindAndSubscribePlayerHealth();

            // Networking
            if (Antigravity.Shooting.NetworkManager.Instance != null) {
                Antigravity.Shooting.NetworkManager.Instance.OnMessageReceived += HandleNetworkMessage;
            }
        }

        private void Start()
        {
            mobsKilled = 0;
            timePlayed = 0f;
            isGameActive = false; // El cronómetro empieza parado hasta que termine la cuenta atrás
            if (mobsKilledLabel != null) mobsKilledLabel.text = "Mobs Killed: 0";
            UpdateTimerDisplay();
        }

        public void SetGameActive(bool active)
        {
            isGameActive = active;
        }

        private void OnDisable()
        {
            EnemyController.OnEnemyKilled -= HandleEnemyKilled;
            if (targetPlayerHealth != null)
            {
                targetPlayerHealth.OnHealthChanged -= HandleHealthChanged;
                targetPlayerHealth.OnPlayerDeath -= ShowEndGameScreen;
            }
            
            if (restartButton != null) restartButton.clicked -= RestartGame;
            if (mainMenuButton != null) mainMenuButton.clicked -= GoToMainMenu;
        }

        public void SyncServerTime(float serverTime)
        {
            // PRO ARCHITECTURE: The server dictates the time.
            // In a real high-end system, we'd interpolate this with local time for smoothness
            // between 50ms ticks, but for now we just apply the snap for the HUD.
            timePlayed = serverTime;
            UpdateTimerDisplay();
        }

        private void Update()
        {
            // Only auto-increment timer in SinglePlayer
            bool isMultiplayer = Antigravity.Auth.GameSession.CurrentGameId != "singleplayer";
            
            if (isGameActive && !isMultiplayer)
            {
                timePlayed += Time.deltaTime;
                UpdateTimerDisplay();
            }

            // Escape key toggles pause (works even if button fails)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            // Continuously look for player if not found
            if (targetPlayerHealth == null)
            {
                FindAndSubscribePlayerHealth();
            }

            CheckLocalGameOverFailsafe();
        }

        private bool failsafeTriggered = false;
        private float diesTimeStamp = 0f;

        private void CheckLocalGameOverFailsafe()
        {
            if (failsafeTriggered || !isGameActive) return;
            if (Antigravity.Auth.GameSession.CurrentGameId == "singleplayer") return;

            // Wait until player and target are found
            if (targetPlayerHealth == null) return;
            
            bool localDead = !targetPlayerHealth.IsAlive;
            
            var remotes = FindObjectsByType<Antigravity.Network.NetworkPlayer>(FindObjectsSortMode.None);
            if (remotes.Length == 0) return; // Haven't spawned yet

            bool anyRemoteAlive = false;
            foreach (var r in remotes) {
                if (r.IsAlive) anyRemoteAlive = true;
            }

            if (localDead && !anyRemoteAlive) {
                if (diesTimeStamp == 0f) diesTimeStamp = Time.time;
                // Esperamos 2 segundos de cortesía para ver si llega el JSON nativamente
                if (Time.time - diesTimeStamp > 2f) {
                    failsafeTriggered = true;
                    Debug.Log("[FAILSAFE] All players dead locally. Forcing Game Over screen!");
                    
                    isGameActive = false;
                    if (hudInstance != null) hudInstance.style.display = DisplayStyle.None;
                    if (pauseInstance != null) pauseInstance.style.display = DisplayStyle.None;
                    
                    if (endGameInstance != null) {
                        endGameInstance.style.display = DisplayStyle.Flex;
                        if (endGameLayer != null) endGameLayer.style.display = DisplayStyle.Flex;
                    }
                    if (finalStatsKills != null && !finalStatsKills.text.Contains("RESULTADOS")) {
                        finalStatsKills.text = "RESULTADOS (Recuperación Local):\n[OPERADOR: JUGADOR] BAJAS: " + mobsKilled;
                        finalStatsKills.style.unityTextAlign = TextAnchor.MiddleCenter;
                    }

                    // Force overview camera
                    var mm = FindFirstObjectByType<Antigravity.GameMode.MatchManager>();
                    if (mm != null) mm.SetCameraState(Antigravity.GameMode.MatchManager.CameraState.OVERVIEW);
                }
            } else {
                diesTimeStamp = 0f; // Reset if someone revived (impossible but safe)
            }
        }

        private void UpdateTimerDisplay()
        {
            if (timePlayedLabel != null)
            {
                int minutes = Mathf.FloorToInt(timePlayed / 60f);
                int seconds = Mathf.FloorToInt(timePlayed % 60f);
                timePlayedLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        private void HandleNetworkMessage(string rawMessage)
        {
            try {
                if (rawMessage.Contains("\"tipo\":\"game_over\"")) {
                    isGameActive = false;
                    if (hudInstance != null) hudInstance.style.display = DisplayStyle.None;
                    if (pauseInstance != null) pauseInstance.style.display = DisplayStyle.None;
                    
                    if (endGameInstance != null) {
                        endGameInstance.style.display = DisplayStyle.Flex;
                        if (endGameLayer != null) endGameLayer.style.display = DisplayStyle.Flex;
                    }

                    var baseMsg = JsonUtility.FromJson<Antigravity.Network.GameOverMessage>(rawMessage);
                    if (baseMsg != null && baseMsg.stats != null) {
                        ShowMultiplayerStats(baseMsg.stats);
                    }
                }
            } catch (Exception e) {
                Debug.LogError("[InGameUIManager] Excepción parseando mensaje: " + e.Message);
            }
        }

        private void ShowMultiplayerStats(System.Collections.Generic.List<Antigravity.Network.PlayerStatsData> stats)
        {
            if (endGameInstance != null)
            {
                
                if (finalStatsKills != null)
                {
                    string summary = "RESULTADOS DE LA INCURSIÓN:\n";
                    try {
                        foreach (var s in stats) {
                            string displayName = !string.IsNullOrEmpty(s.username) ? s.username : "JUGADOR";
                            summary += $"\n[OPERADOR: {displayName}] BAJAS: {s.kills} | TIEMPO: {s.time}s";
                        }
                    } catch (Exception ex) {
                        summary += "\n(Error procesando datos)";
                        Debug.LogError("[ShowMultiplayerStats] Exception: " + ex.Message);
                    }
                    finalStatsKills.text = summary;
                    finalStatsKills.style.unityTextAlign = TextAnchor.MiddleCenter;
                    finalStatsKills.style.whiteSpace = WhiteSpace.Normal; // Enable word wrapping
                }
                if (finalStatsTime != null) finalStatsTime.text = ""; 
            }
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Debug.Log($"[InGameUIManager] TogglePause called. New isPaused: {isPaused}");

            // GUARD: Only freeze the game if the pause UI can actually be shown
            if (pauseInstance != null) {
                pauseInstance.style.display = isPaused ? DisplayStyle.Flex : DisplayStyle.None;
                
                if (isPaused)
                {
                    // Update current stats when pausing
                    if (pauseStatsKills != null) pauseStatsKills.text = $"BAJAS: {mobsKilled}";
                    if (pauseStatsTime != null && timePlayedLabel != null) pauseStatsTime.text = $"TIEMPO: {timePlayedLabel.text}";
                }

                Debug.Log($"[InGameUIManager] Pause instance display: {pauseInstance.style.display}");

                // Pausa física solo en single player genuino o sala "singleplayer"
                bool isRealMultiplayer = Antigravity.Auth.GameSession.CurrentGameId != "singleplayer" &&
                                         Antigravity.Shooting.NetworkManager.Instance != null && 
                                         Antigravity.Shooting.NetworkManager.Instance.isActiveAndEnabled;

                if (!isRealMultiplayer) {
                    Time.timeScale = isPaused ? 0f : 1f;
                    Debug.Log($"[InGameUIManager] Time.timeScale set to: {Time.timeScale}");
                } else {
                    Debug.Log("[InGameUIManager] Time.timeScale NOT changed because we are in Multiplayer.");
                }
            } else {
                // SAFETY: Don't freeze the game if the pause menu can't appear
                isPaused = false;
                Time.timeScale = 1f;
                Debug.LogError("[InGameUIManager] CRITICAL: pauseInstance is NULL! Attempting to re-query...");
                
                // Try to re-query the element
                if (uiDocument != null) {
                    var root = uiDocument.rootVisualElement;
                    pauseInstance = root.Q<VisualElement>("pause-instance");
                    pauseMenuInner = root.Q<VisualElement>("pause-menu");
                    pauseStatsKills = root.Q<Label>("pause-stats-kills");
                    pauseStatsTime = root.Q<Label>("pause-stats-time");
                    resumeButton = root.Q<Button>("btn-resume");
                    quitButton = root.Q<Button>("btn-quit");
                    
                    if (resumeButton != null) resumeButton.clicked += TogglePause;
                    if (quitButton != null) quitButton.clicked += GoToMainMenu;
                    
                    Debug.Log($"[InGameUIManager] Re-query result: pauseInstance={pauseInstance != null}, pauseMenu={pauseMenuInner != null}");
                }
            }
        }

        private void FindAndSubscribePlayerHealth()
        {
            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            if (player != null)
            {
                targetPlayerHealth = player;
                targetPlayerHealth.OnHealthChanged += HandleHealthChanged;
                targetPlayerHealth.OnPlayerDeath += ShowEndGameScreen;

                // Sync Initial Health
                HandleHealthChanged(targetPlayerHealth.currentHealth, targetPlayerHealth.maxHealth);
            }
        }

        private void HandleEnemyKilled()
        {
            mobsKilled++;
            if (targetPlayerHealth != null && !targetPlayerHealth.IsAlive && Antigravity.Auth.GameSession.CurrentGameId != "singleplayer") {
                // Si estoy muerto en multiplayer, dejo mi rótulo de CAÍDO quieto
                return;
            }
            if (mobsKilledLabel != null)
            {
                mobsKilledLabel.text = $"Mobs Killed: {mobsKilled}";
            }
        }

        private void HandleHealthChanged(int currentHealth, int maxHealth)
        {
            if (healthBar != null)
            {
                healthBar.highValue = maxHealth;
                healthBar.value = currentHealth;
                healthBar.title = $"{currentHealth}/{maxHealth}";
            }
        }

        private void ShowEndGameScreen()
        {
            if (Antigravity.Auth.GameSession.CurrentGameId != "singleplayer")
            {
                Debug.Log("[InGameUIManager] Player died in multiplayer. Entering spectator view instead of showing Game Over.");
                if (mobsKilledLabel != null) mobsKilledLabel.text = "ESTADO: CAÍDO (ESPECTADOR)";
                return;
            }

            isGameActive = false;
            if (hudInstance != null) hudInstance.style.display = DisplayStyle.None;
            if (gameHudLayer != null) gameHudLayer.style.display = DisplayStyle.None;
            
            if (endGameInstance != null) endGameInstance.style.display = DisplayStyle.Flex;
            if (endGameLayer != null)
            {
                endGameLayer.style.display = DisplayStyle.Flex;
                
                string timeStr = timePlayedLabel != null ? timePlayedLabel.text : "00:00";
                if (finalStatsKills != null) finalStatsKills.text = $"BOTS ELIMINADOS: {mobsKilled}";
                if (finalStatsTime != null) finalStatsTime.text = $"TIEMPO: {timeStr}";
            }
            StartCoroutine(UpdateStatsRoutine());
        }

        [System.Serializable]
        private class UpdateStatsRequest
        {
            public int mobsKilled;
            public int timeSurvived;
        }

        private IEnumerator UpdateStatsRoutine()
        {
            if (string.IsNullOrEmpty(GameSession.Token))
                yield break; // Skip if no token is present

            string baseUrl = Antigravity.Config.AntigravityConfig.Instance != null 
                ? Antigravity.Config.AntigravityConfig.Instance.HttpBaseUrl 
                : "http://localhost:3000/api";
            string json = JsonUtility.ToJson(new UpdateStatsRequest { 
                mobsKilled = mobsKilled, 
                timeSurvived = Mathf.FloorToInt(timePlayed) 
            });

            using (UnityWebRequest www = new UnityWebRequest(baseUrl + "/users/stats", "PUT"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + GameSession.Token);
                www.timeout = 10;

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Stats updated successfully!");
                }
                else
                {
                    Debug.LogError("Error updating stats: " + www.error);
                }
            }
        }

        private void RestartGame()
        {
            // Reloads current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void GoToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}

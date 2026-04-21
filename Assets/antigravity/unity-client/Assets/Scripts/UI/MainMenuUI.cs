using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System;
using Antigravity.Auth; 

[Serializable]
public class GameData
{
    public string id;
    public string name;
    public int currentPlayers;
    public int maxPlayers;
    public string status;
    public bool isPrivate;
}

[RequireComponent(typeof(UIDocument))]
public class MainMenuUI : MonoBehaviour
{
    private string baseUrl => Antigravity.Config.AntigravityConfig.Instance != null 
        ? Antigravity.Config.AntigravityConfig.Instance.HttpBaseUrl 
        : "http://localhost:3000/api";
    private string GetUserToken() { return GameSession.Token ?? "test-user-id"; }

    private TextField gameNameInput;
    private Toggle gameIsPrivateToggle;
    private SliderInt maxPlayersSlider;
    private ScrollView gamesScrollView;
    private Button createButton;
    private Button refreshButton;
    private Button joinByCodeButton;

    // Join Modal Elements
    private VisualElement joinModal;
    private TextField joinCodeInput;
    private Button confirmJoinButton;
    private Button cancelJoinButton;

    // Created Modal Elements
    private VisualElement createdModal;
    private Label createdCodeLabel;
    private Button copyEnterButton;
    private string newlyCreatedGameId;

    private void Start()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        // Query the elements
        gameNameInput = root.Q<TextField>("game-name-input");
        gameIsPrivateToggle = root.Q<Toggle>("game-is-private-toggle");
        maxPlayersSlider = root.Q<SliderInt>("max-players-slider");
        gamesScrollView = root.Q<ScrollView>("games-scroll-view");
        
        createButton = root.Q<Button>("create-button");
        refreshButton = root.Q<Button>("refresh-button");
        joinByCodeButton = root.Q<Button>("join-by-code-button");

        // Join Modal
        joinModal = root.Q<VisualElement>("join-modal");
        joinCodeInput = root.Q<TextField>("join-code-input");
        confirmJoinButton = root.Q<Button>("confirm-join-button");
        cancelJoinButton = root.Q<Button>("cancel-join-button");

        // Created Modal
        createdModal = root.Q<VisualElement>("created-modal");
        createdCodeLabel = root.Q<Label>("created-code-label");
        copyEnterButton = root.Q<Button>("copy-enter-button");

        // Set up interactions
        if (maxPlayersSlider != null)
        {
            maxPlayersSlider.RegisterValueChangedCallback(evt => {
                maxPlayersSlider.label = $"Máximo de Jugadores: {evt.newValue}";
            });
        }

        if (createButton != null) createButton.clicked += CreateGame;
        if (refreshButton != null) refreshButton.clicked += RefreshGamesList;
        
        if (joinByCodeButton != null) joinByCodeButton.clicked += () => {
            if (joinCodeInput != null) 
            {
                joinCodeInput.value = "";
                joinCodeInput.schedule.Execute(() => joinCodeInput.Focus()).StartingIn(100);
            }
            if (joinModal != null) joinModal.style.display = DisplayStyle.Flex;
        };

        if (confirmJoinButton != null) confirmJoinButton.clicked += () => JoinGameByCode(joinCodeInput.value);
        if (cancelJoinButton != null) cancelJoinButton.clicked += () => {
            if (joinModal != null) joinModal.style.display = DisplayStyle.None;
        };

        if (copyEnterButton != null) copyEnterButton.clicked += () => {
            if (createdCodeLabel != null) {
                // Copy to clipboard
                GUIUtility.systemCopyBuffer = createdCodeLabel.text;
                Debug.Log("Código copiado al portapapeles: " + createdCodeLabel.text);
            }
            if (createdModal != null) createdModal.style.display = DisplayStyle.None;
            GameSession.CurrentGameId = newlyCreatedGameId;
            SceneManager.LoadScene("Arena");
        };

        // Set up Mode Selection interactions
        var modeSelectionPanel = root.Q<VisualElement>("mode-selection-panel");
        var multiplayerPanel = root.Q<VisualElement>("multiplayer-panel");
        var statsPanel = root.Q<VisualElement>("stats-panel");

        var btnSingleplayer = root.Q<Button>("btn-singleplayer");
        var btnMultiplayer = root.Q<Button>("btn-multiplayer");
        var btnStats = root.Q<Button>("btn-stats");
        
        var btnBackMulti = root.Q<Button>("btn-back-multi");
        var btnBackStats = root.Q<Button>("btn-back-stats");

        if (btnSingleplayer != null) btnSingleplayer.clicked += () => {
            GameSession.CurrentGameId = "singleplayer";
            SceneManager.LoadScene("Arena");
        };

        if (btnMultiplayer != null) btnMultiplayer.clicked += () => {
            if (modeSelectionPanel != null) modeSelectionPanel.style.display = DisplayStyle.None;
            if (multiplayerPanel != null) multiplayerPanel.style.display = DisplayStyle.Flex;
            if (gameNameInput != null) gameNameInput.schedule.Execute(() => gameNameInput.Focus()).StartingIn(100);
            RefreshGamesList();
        };

        if (btnStats != null) btnStats.clicked += () => {
            if (modeSelectionPanel != null) modeSelectionPanel.style.display = DisplayStyle.None;
            if (statsPanel != null) statsPanel.style.display = DisplayStyle.Flex;
            
            var lblMobs = root.Q<Label>("stat-mobs-killed");
            var lblTime = root.Q<Label>("stat-time-survived");
            StartCoroutine(FetchUserStatsRoutine(lblMobs, lblTime));
        };

        if (btnBackMulti != null) btnBackMulti.clicked += () => {
            if (modeSelectionPanel != null) modeSelectionPanel.style.display = DisplayStyle.Flex;
            if (multiplayerPanel != null) multiplayerPanel.style.display = DisplayStyle.None;
        };

        if (btnBackStats != null) btnBackStats.clicked += () => {
            if (modeSelectionPanel != null) modeSelectionPanel.style.display = DisplayStyle.Flex;
            if (statsPanel != null) statsPanel.style.display = DisplayStyle.None;
        };

        RefreshGamesList();
    }

    private void OnDestroy()
    {
        if (createButton != null) createButton.clicked -= CreateGame;
        if (refreshButton != null) refreshButton.clicked -= RefreshGamesList;
    }

    public void CreateGame()
    {
        if (gameNameInput == null || string.IsNullOrWhiteSpace(gameNameInput.value)) return;
        bool isPrivate = gameIsPrivateToggle != null ? gameIsPrivateToggle.value : false;
        // Limitemos las salas a 2 jugadores por defecto para simplificar el flujo
        StartCoroutine(CreateGameRoutine(gameNameInput.value, 2, isPrivate));
    }

    IEnumerator CreateGameRoutine(string name, int maxPlayers, bool isPrivate)
    {
        string json = JsonUtility.ToJson(new CreateGameRequest { name = name, maxPlayers = maxPlayers, isPrivate = isPrivate });
        
        using (UnityWebRequest www = new UnityWebRequest(baseUrl + "/games", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + GetUserToken());

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Game created successfully!");
                CreateGameResponse response = JsonUtility.FromJson<CreateGameResponse>(www.downloadHandler.text);
                
                if (isPrivate && !string.IsNullOrEmpty(response.code)) {
                    newlyCreatedGameId = response.gameId;
                    if (createdCodeLabel != null) createdCodeLabel.text = response.code;
                    if (createdModal != null) createdModal.style.display = DisplayStyle.Flex;
                } else {
                    GameSession.CurrentGameId = response.gameId;
                    SceneManager.LoadScene("Arena");
                }
            }
            else
            {
                Debug.LogError("Error creating game: " + www.error);
            }
        }
    }

    [Serializable]
    private class CreateGameRequest {
        public string name;
        public int maxPlayers;
        public bool isPrivate;
    }
    
    [Serializable]
    private class CreateGameResponse {
        public string gameId;
        public string message;
        public string status;
        public string code;
    }

    public void RefreshGamesList()
    {
        StartCoroutine(GetGamesRoutine());
    }

    IEnumerator GetGamesRoutine()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "/games"))
        {
            www.SetRequestHeader("Authorization", "Bearer " + GetUserToken());
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = "{\"games\":" + www.downloadHandler.text + "}";
                GameListWrapper wrapper = JsonUtility.FromJson<GameListWrapper>(jsonResponse);
                UpdateUIList(wrapper.games);
            }
            else
            {
                Debug.LogError("Error fetching games: " + www.error);
            }
        }
    }

    void UpdateUIList(GameData[] games)
    {
        if (gamesScrollView == null) return;
        gamesScrollView.Clear();

        foreach (var game in games)
        {
            VisualElement item = new VisualElement();
            item.AddToClassList("game-item");

            Label nameLabel = new Label($"{game.name} ({game.currentPlayers}/{game.maxPlayers})");
            nameLabel.AddToClassList("game-item-name");
            item.Add(nameLabel);

            Button joinBtn = new Button { text = "UNIRSE" };
            joinBtn.AddToClassList("join-button");

            joinBtn.clicked += () => {
                JoinGame(game.id);
            };
            item.Add(joinBtn);

            gamesScrollView.Add(item);
        }
    }

    public void JoinGame(string gameId)
    {
        StartCoroutine(JoinGameRoutine(gameId));
    }

    IEnumerator JoinGameRoutine(string gameId)
    {
        using (UnityWebRequest www = new UnityWebRequest(baseUrl + "/games/" + gameId + "/join", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + GetUserToken());
            
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Joined game successfully! Transitioning to Arena...");
                GameSession.CurrentGameId = gameId;
                SceneManager.LoadScene("Arena");
            }
            else
            {
                Debug.LogError("Error joining game: " + www.error);
            }
        }
    }

    public void JoinGameByCode(string code)
    {
        StartCoroutine(JoinPrivateGameRoutine(code));
    }

    IEnumerator JoinPrivateGameRoutine(string code)
    {
        string json = "{\"code\":\"" + (code ?? "") + "\"}";
        using (UnityWebRequest www = new UnityWebRequest(baseUrl + "/games/join-private", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + GetUserToken());
            
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Joined private game successfully! Transitioning...");
                
                string jsonResponse = www.downloadHandler.text;
                JoinPrivateResponse response = JsonUtility.FromJson<JoinPrivateResponse>(jsonResponse);
                
                if (joinModal != null) joinModal.style.display = DisplayStyle.None;
                
                GameSession.CurrentGameId = response.game.id;
                SceneManager.LoadScene("Arena");
            }
            else
            {
                string serverMsg = www.downloadHandler != null ? www.downloadHandler.text : "(sin respuesta)";
                Debug.LogError($"Error joining private game: {www.error} | Server: {serverMsg}");
            }
        }
    }

    [Serializable]
    private class JoinPrivateResponse {
        public string message;
        public GameData game;
    }

    [Serializable]
    private class GameListWrapper
    {
        public GameData[] games;
    }

    [Serializable]
    private class UserStatsResponse {
        public string username;
        public int maxMobsKilled;
        public int maxTimeSurvived;
    }

    IEnumerator FetchUserStatsRoutine(Label mobsLabel, Label timeLabel)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "/users/me"))
        {
            www.SetRequestHeader("Authorization", "Bearer " + GetUserToken());
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                UserStatsResponse res = JsonUtility.FromJson<UserStatsResponse>(www.downloadHandler.text);
                if (mobsLabel != null) mobsLabel.text = $"Récord de Mobs Eliminados: {res.maxMobsKilled}";
                if (timeLabel != null) {
                    int mins = Mathf.FloorToInt(res.maxTimeSurvived / 60f);
                    int secs = Mathf.FloorToInt(res.maxTimeSurvived % 60f);
                    timeLabel.text = string.Format("Mayor Tiempo Sobrevivido: {0:00}:{1:00}", mins, secs);
                }
            }
            else
            {
                Debug.LogError("Error fetching stats: " + www.error);
                if (mobsLabel != null) mobsLabel.text = "Error cargando estadísticas...";
            }
        }
    }
}

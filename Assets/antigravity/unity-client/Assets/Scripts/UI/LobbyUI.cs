using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

public class LobbyUI : MonoBehaviour
{
    public Transform playersContainer;
    public GameObject playerItemPrefab;
    public Button startButton;
    public Text gameNameText;

    private string baseUrl => Antigravity.Config.AntigravityConfig.Instance != null 
        ? Antigravity.Config.AntigravityConfig.Instance.HttpBaseUrl 
        : "http://localhost:3000/api";
    private string currentGameId;
    private string userToken = "test-user-id";

    public void SetGame(string gameId, string name)
    {
        currentGameId = gameId;
        gameNameText.text = name;
        StartCoroutine(PollPlayersRoutine());
    }

    IEnumerator PollPlayersRoutine()
    {
        while (currentGameId != null)
        {
            yield return GetGameDetails();
            yield return new WaitForSeconds(3f); // Poll every 3 seconds
        }
    }

    IEnumerator GetGameDetails()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "/games/" + currentGameId)) // This endpoint might need to be added or use list
        {
            www.SetRequestHeader("Authorization", "Bearer " + userToken);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Parse details and update UI
            }
        }
    }

    public void StartGame()
    {
        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(baseUrl + "/games/" + currentGameId + "/start", "POST"))
        {
            www.SetRequestHeader("Authorization", "Bearer " + userToken);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Game started!");
                // The transition should be handled by WebSocket event ideally
            }
        }
    }
}

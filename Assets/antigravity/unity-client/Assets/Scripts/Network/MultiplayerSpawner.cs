using UnityEngine;
using System.Collections.Generic;
using Antigravity.Shooting;

namespace Antigravity.Network
{
    public class MultiplayerSpawner : MonoBehaviour
    {
        public GameObject remotePlayerPrefab;
        private Dictionary<string, NetworkPlayer> remotePlayers = new Dictionary<string, NetworkPlayer>();

        private void Awake()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived += HandleMessage;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived -= HandleMessage;
            }
        }

        private void HandleMessage(string rawMessage)
        {
            Debug.Log("[MultiplayerSpawner] Received: " + rawMessage);
            try
            {
                WSMessage baseMsg = JsonUtility.FromJson<WSMessage>(rawMessage);
                Debug.Log("[MultiplayerSpawner] Parsed type: " + baseMsg.tipo);

                if (baseMsg.tipo == "lista_jugadores")
                {
                    var list = JsonUtility.FromJson<PlayerListMessage>(rawMessage);
                    foreach (var p in list.jugadores)
                    {
                        SpawnRemotePlayer(p.userId, p.username);
                    }
                }
                else if (baseMsg.tipo == "nuevo_jugador")
                {
                    var p = JsonUtility.FromJson<PlayerData>(rawMessage);
                    SpawnRemotePlayer(p.userId, p.username);
                }
                else if (baseMsg.tipo == "jugador_desconectado")
                {
                    var p = JsonUtility.FromJson<PlayerData>(rawMessage);
                    RemoveRemotePlayer(p.userId);
                }
                else if (baseMsg.tipo == "jugador_movido")
                {
                    var move = JsonUtility.FromJson<MoveMessage>(rawMessage);
                    if (remotePlayers.ContainsKey(move.userId))
                    {
                        remotePlayers[move.userId].UpdateState(move.posicion, move.velocidad, move.mirando);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[MultiplayerSpawner] Error parsing: " + e.Message);
            }
        }

        private void SpawnRemotePlayer(string userId, string username)
        {
            if (remotePlayers.ContainsKey(userId)) {
                Debug.Log($"[MultiplayerSpawner] Player {userId} already spawned.");
                return;
            }

            // WARNING: If IDs are identical, it's probably local testing with same account.
            // We allow it but with a warning to help the user.
            bool isMe = userId == Antigravity.Auth.GameSession.UserId;
            if (isMe) {
                Debug.LogWarning($"[MultiplayerSpawner] Spawning a copy of MYSELF for testing purposes (UserId: {userId}). This shouldn't happen in production with different accounts.");
            }

            if (remotePlayerPrefab == null) {
                Debug.LogError("[MultiplayerSpawner] CRITICAL: remotePlayerPrefab is NULL! Spawning aborted.");
                return;
            }

            GameObject go = Instantiate(remotePlayerPrefab, new Vector3(remotePlayers.Count * 2f, 0, 0), Quaternion.identity);
            go.name = isMe ? "RemoteClone_OF_SELF" : "RemotePlayer_" + username;
            
            NetworkPlayer np = go.AddComponent<NetworkPlayer>();
            np.userId = userId;
            np.username = username;

            // VISUAL DIFFERENTIATION: Make remote players slightly different color
            var renderer = go.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) {
                renderer.color = new Color(0.7f, 0.7f, 1f, 1f); // Bluish tint for remote players
            }

            // NAME LABEL: Simple legacy TextMesh above head
            GameObject nameLabelGo = new GameObject("NameLabel");
            nameLabelGo.transform.SetParent(go.transform);
            nameLabelGo.transform.localPosition = new Vector3(0, 1.2f, 0);
            
            var textMesh = nameLabelGo.AddComponent<TextMesh>();
            textMesh.text = username;
            textMesh.fontSize = 24;
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;

            // ISOLATION: Remove or disable everything that could act on local input
            // (Repeated logic from previously but even more thorough)
            Transform cam = go.transform.Find("Main Camera");
            if (cam != null) cam.gameObject.SetActive(false);
            
            var localScripts = go.GetComponentsInChildren<MonoBehaviour>();
            foreach(var s in localScripts) {
                // If the script is NOT NetworkPlayer, disable it.
                if (!(s is NetworkPlayer) && !(s is SpriteRenderer) && !(s is Animator)) {
                    s.enabled = false;
                }
            }

            remotePlayers.Add(userId, np);
            Debug.Log($"[MultiplayerSpawner] Spawned remote player: {username} ({userId}) at {go.transform.position}");
        }

        private void RemoveRemotePlayer(string userId)
        {
            if (remotePlayers.ContainsKey(userId))
            {
                Destroy(remotePlayers[userId].gameObject);
                remotePlayers.Remove(userId);
                Debug.Log($"[MultiplayerSpawner] Removed remote player: {userId}");
            }
        }
    }
}

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

            GameObject go = Instantiate(remotePlayerPrefab, new Vector3(remotePlayers.Count * 3f, 0, 0), Quaternion.identity);
            go.name = "REMOTE_" + username + "_" + userId;
            
            NetworkPlayer np = go.AddComponent<NetworkPlayer>();
            np.userId = userId;
            np.username = username;

            // EMERGENCY DIAGNOSTIC: Paint remote players RED
            var renderer = go.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) {
                renderer.color = Color.red; // BRIGHT RED for easy identification
                renderer.sortingOrder = 10; // Ensure it's on top
            }

            // ISOLATION: Destroy scripts instead of just disabling them to be 100% sure
            var sync = go.GetComponent<Antigravity.Network.PlayerNetworkSync>();
            if (sync != null) DestroyImmediate(sync);
            
            var movement = go.GetComponent<Antigravity.Player.PlayerMovement>();
            if (movement != null) {
                movement.canMove = false;
                movement.enabled = false;
            }

            var shoot = go.GetComponent<Antigravity.Shooting.ShootController>();
            if (shoot != null) shoot.enabled = false;

            // Audio & Camera
            Camera c = go.GetComponentInChildren<Camera>();
            if (c != null) DestroyImmediate(c.gameObject);

            AudioListener al = go.GetComponentInChildren<AudioListener>();
            if (al != null) DestroyImmediate(al);

            remotePlayers.Add(userId, np);
            Debug.Log($"[DIAGNOSTIC] Spawned RED remote player: {username} at {go.transform.position}");
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

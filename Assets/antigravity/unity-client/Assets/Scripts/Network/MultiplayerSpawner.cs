using UnityEngine;
using System.Collections.Generic;
using Antigravity.Shooting;

namespace Antigravity.Network
{
    public class MultiplayerSpawner : MonoBehaviour
    {
        public GameObject remotePlayerPrefab;
        private Dictionary<string, NetworkPlayer> remotePlayers = new Dictionary<string, NetworkPlayer>();

        private void Start()
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
            if (remotePlayers.ContainsKey(userId)) return;

            // Don't spawn self
            if (userId == Antigravity.Auth.GameSession.UserId) return;

            GameObject go = Instantiate(remotePlayerPrefab, Vector3.zero, Quaternion.identity);
            go.name = "RemotePlayer_" + username;
            
            NetworkPlayer np = go.AddComponent<NetworkPlayer>();
            np.userId = userId;
            np.username = username;

            // Disable local components
            var movement = go.GetComponent<Antigravity.Player.PlayerMovement>();
            if (movement != null) movement.enabled = false;
            
            var shooting = go.GetComponent<Antigravity.Shooting.ShootController>();
            if (shooting != null) shooting.enabled = false;

            remotePlayers.Add(userId, np);
            Debug.Log($"[MultiplayerSpawner] Spawned remote player: {username} ({userId})");
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

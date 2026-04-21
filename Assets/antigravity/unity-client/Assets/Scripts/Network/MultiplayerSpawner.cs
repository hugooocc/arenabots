using UnityEngine;
using System.Collections.Generic;
using Antigravity.Shooting;
using Antigravity.Player;

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
            if (remotePlayers.ContainsKey(userId)) return;

            // [BEST STRATEGY] Clone the local player to ensure 100% visual parity
            GameObject local = null;
            PlayerMovement[] allMovements = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach(var pm in allMovements) {
                if (pm.GetComponent<NetworkPlayer>() == null) {
                    local = pm.gameObject;
                    break;
                }
            }

            GameObject go;
            if (local != null) {
                go = Instantiate(local);
                Debug.Log($"[MultiplayerSpawner] Cloned LOCAL player for {username}");
            } else {
                if (remotePlayerPrefab == null) return;
                go = Instantiate(remotePlayerPrefab);
                Debug.Log($"[MultiplayerSpawner] Fallback to prefab for {username}");
            }

            go.name = "REMOTE_" + username + "_" + userId;
            NetworkPlayer np = go.AddComponent<NetworkPlayer>();
            np.userId = userId;
            np.username = username;

            // Teleport to side of local
            if (local != null) {
                go.transform.position = local.transform.position + new Vector3(2f, 2f, 0);
            }

            // CLEANUP: Remove ALL components that shouldn't be on a remote doll
            // We do this by name or type to be thorough
            var scriptsToDestroy = new System.Type[] {
                typeof(Antigravity.Player.PlayerMovement),
                typeof(Antigravity.Shooting.ShootController),
                typeof(AudioListener),
                typeof(Camera),
                typeof(Antigravity.Network.PlayerNetworkSync)
            };

            foreach(var type in scriptsToDestroy) {
                var comp = go.GetComponentInChildren(type);
                if (comp != null) {
                    if (type == typeof(Camera)) {
                        // Cameras usually have their own GameObject we want to kill
                        DestroyImmediate(comp.gameObject);
                    } else {
                        DestroyImmediate(comp);
                    }
                }
            }

            // Ensure it's active and visible
            go.SetActive(true);
            var renderers = go.GetComponentsInChildren<SpriteRenderer>();
            foreach(var r in renderers) {
                r.enabled = true;
                // We DON'T paint it red anymore, user wants original visuals
                r.color = Color.white; 
            }

            remotePlayers.Add(userId, np);
            Debug.Log($"[MultiplayerSpawner] Remote player REPLICATED: {username}");
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

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

            // [FIX] PROXIMIDAD FORZADA: Teletransportar cerca del local para asegurar visibilidad
            GameObject go = Instantiate(remotePlayerPrefab);
            PlayerMovement local = Object.FindAnyObjectByType<PlayerMovement>();
            if (local != null && local.GetComponent<NetworkPlayer>() == null) {
                go.transform.position = local.transform.position + new Vector3(remotePlayers.Count + 1f, 0, 0);
            } else {
                go.transform.position = new Vector3(remotePlayers.Count * 3f, 0, 0);
            }

            go.name = "REMOTE_" + username + "_" + userId;

            NetworkPlayer np = go.AddComponent<NetworkPlayer>();
            go.AddComponent<VisibilityPointer>();
            np.userId = userId;
            np.username = username;

            // EMERGENCY DIAGNOSTIC: Paint ALL SpriteRenderers RED
            var renderers = go.GetComponentsInChildren<SpriteRenderer>();
            foreach(var r in renderers) {
                r.color = Color.red; 
                r.sortingOrder = 100; // SUPER TOP
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

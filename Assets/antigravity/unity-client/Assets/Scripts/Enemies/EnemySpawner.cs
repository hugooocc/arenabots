using System;
using System.Collections.Generic;
using UnityEngine;
using Antigravity.Shooting;

namespace Antigravity.Enemies
{
    [Serializable]
    public class BackendEnemySpawnData
    {
        public string tipo;
        public string enemigoId;
        public float x;
        public float y;
        public int hp;
    }

    [Serializable]
    public class BackendEnemyDeathData
    {
        public string tipo;
        public string enemigoId;
    }

    [Serializable]
    public class EnemyTargetData
    {
        public string id;
        public string targetId;
    }

    [Serializable]
    public class SyncEnemyTargetsData
    {
        public string tipo;
        public EnemyTargetData[] targets;
    }

    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("References")]
        public GameObject enemyPrefab; // Prefab of the Zombie-Bot
        
        // Dictionary to track active enemies by their Backend UUID
        private Dictionary<string, EnemyController> activeEnemies = new Dictionary<string, EnemyController>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived += HandleWebSocketMessage;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived -= HandleWebSocketMessage;
            }
        }

        private void HandleWebSocketMessage(string messageJson)
        {
            // Simple type parsing
            if (messageJson.Contains("\"tipo\":\"spawn_enemy\""))
            {
                var data = JsonUtility.FromJson<BackendEnemySpawnData>(messageJson);
                SpawnEnemy(data);
            }
            else if (messageJson.Contains("\"tipo\":\"enemigo_muerto\""))
            {
                var data = JsonUtility.FromJson<BackendEnemyDeathData>(messageJson);
                KillEnemy(data.enemigoId);
            }
            else if (messageJson.Contains("\"tipo\":\"sync_enemy_targets\""))
            {
                var data = JsonUtility.FromJson<SyncEnemyTargetsData>(messageJson);
                UpdateEnemyTargets(data.targets);
            }
        }

        private void UpdateEnemyTargets(EnemyTargetData[] targets)
        {
            foreach (var t in targets)
            {
                if (activeEnemies.TryGetValue(t.id, out EnemyController enemy))
                {
                    Transform targetTransform = FindPlayerTransformById(t.targetId);
                    if (targetTransform != null)
                    {
                        enemy.SetTarget(targetTransform);
                    }
                }
            }
        }

        private Transform FindPlayerTransformById(string userId)
        {
            // 1. Local
            if (Antigravity.Auth.GameSession.UserId == userId)
            {
                var local = Object.FindObjectsByType<Antigravity.Player.PlayerMovement>(FindObjectsSortMode.None);
                foreach (var p in local) {
                    if (p.GetComponent<Antigravity.Network.NetworkPlayer>() == null) return p.transform;
                }
            }

            // 2. Remotes
            var remotes = Object.FindObjectsByType<Antigravity.Network.NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var r in remotes)
            {
                if (r.userId == userId) return r.transform;
            }

            return null;
        }

        private void SpawnEnemy(BackendEnemySpawnData data)
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("EnemyPrefab is not assigned in EnemySpawner!");
                return;
            }

            Vector2 spawnPos = new Vector2(data.x, data.y);
            GameObject newEnemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            
            EnemyController controller = newEnemyObj.GetComponent<EnemyController>();
            if (controller != null)
            {
                controller.Initialize(data.enemigoId, data.hp);
                activeEnemies[data.enemigoId] = controller;
            }
            else
            {
                Debug.LogError("The EnemyPrefab is missing the EnemyController script!");
            }
        }

        private void KillEnemy(string enemyId)
        {
            if (activeEnemies.TryGetValue(enemyId, out EnemyController enemy))
            {
                enemy.Die();
                activeEnemies.Remove(enemyId);
            }
        }
    }
}

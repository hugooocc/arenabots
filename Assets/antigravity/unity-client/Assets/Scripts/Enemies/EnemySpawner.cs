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
            if (messageJson.Contains("\"tipo\":\"game_tick\""))
            {
                var data = JsonUtility.FromJson<BackendGameTickData>(messageJson);
                UpdateEnemiesFromTick(data.enemies);
            }
            else if (messageJson.Contains("\"tipo\":\"full_state\""))
            {
                var data = JsonUtility.FromJson<BackendGameTickData>(messageJson);
                ReconstructFullState(data.enemies);
            }
            else if (messageJson.Contains("\"tipo\":\"spawn_enemy\""))
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

        private void ReconstructFullState(BackendEnemySnapshot[] snapshot)
        {
            Debug.Log($"[EnemySpawner] Reconstruyendo estado completo con {snapshot.Length} enemigos.");
            // Destruir lo que no esté en el snapshot (opcional para limpieza profunda)
            HashSet<string> snapshotIds = new HashSet<string>();
            foreach (var s in snapshot) snapshotIds.Add(s.id);

            List<string> toRemove = new List<string>();
            foreach (var id in activeEnemies.Keys) {
                if (!snapshotIds.Contains(id)) toRemove.Add(id);
            }
            foreach (var id in toRemove) KillEnemy(id);

            // Crear o actualizar existentes
            foreach (var s in snapshot) {
                if (!activeEnemies.ContainsKey(s.id)) {
                    SpawnEnemy(new BackendEnemySpawnData { enemigoId = s.id, x = s.x, y = s.y, hp = s.hp });
                }
                activeEnemies[s.id].UpdateNetworkState(s.x, s.y, s.hp, s.seq);
            }
        }

        private void UpdateEnemiesFromTick(BackendEnemySnapshot[] snapshot)
        {
            foreach (var s in snapshot)
            {
                if (activeEnemies.TryGetValue(s.id, out EnemyController enemy))
                {
                    enemy.UpdateNetworkState(s.x, s.y, s.hp, s.seq);
                }
            }
        }

        [Serializable]
        public class BackendEnemySnapshot {
            public string id;
            public float x;
            public float y;
            public int hp;
            public int seq;
        }

        [Serializable]
        public class BackendGameTickData {
            public string tipo;
            public int tick;
            public float time;
            public BackendEnemySnapshot[] enemies;
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
                var local = UnityEngine.Object.FindObjectsByType<Antigravity.Player.PlayerMovement>(FindObjectsSortMode.None);
                foreach (var p in local) {
                    if (p.GetComponent<Antigravity.Network.NetworkPlayer>() == null) return p.transform;
                }
            }

            // 2. Remotes
            var remotes = UnityEngine.Object.FindObjectsByType<Antigravity.Network.NetworkPlayer>(FindObjectsSortMode.None);
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

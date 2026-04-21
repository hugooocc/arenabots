using UnityEngine;
using System.Collections.Generic;

namespace Antigravity.Environment
{
    public class InfiniteWorldManager : MonoBehaviour
    {
        [Header("Settings")]
        public GameObject floorPrefab;
        public float chunkSize = 30f;
        public int viewDistance = 1; // 1 means 3x3 grid around player

        [Header("Decor Settings")]
        public GameObject[] decorPrefabs;
        public int minDecorPerChunk = 2;
        public int maxDecorPerChunk = 5;

        private Transform playerTransform;
        private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();

        private void Start()
        {
            // SEGURIDAD: El mundo infinito NUNCA debe ser hijo del jugador
            if (transform.parent != null)
            {
                transform.parent = null;
            }
            
            FindPlayer();
            UpdateChunks();
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }
            
            // Comprobar si el jugador ha cambiado de chunk
            UpdateChunks();
        }

        private void FindPlayer()
        {
            // Intentar por Tag primero
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) 
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                // Si falla, buscar por componente de movimiento
                var movement = Object.FindAnyObjectByType<Antigravity.Player.PlayerMovement>();
                if (movement != null) playerTransform = movement.transform;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[InfiniteWorldManager] Esperando a que el jugador aparezca en la escena...");
            }
        }

        private void UpdateChunks()
        {
            if (playerTransform == null || floorPrefab == null) return;
            if (chunkSize < 1f) chunkSize = 30f; // Failsafe para evitar divisiones por cero

            Vector2Int currentChunkCoord = new Vector2Int(
                Mathf.RoundToInt(playerTransform.position.x / chunkSize),
                Mathf.RoundToInt(playerTransform.position.y / chunkSize)
            );

            // Crear nuevos chunks y mantener los existentes
            HashSet<Vector2Int> visibleCoords = new HashSet<Vector2Int>();
            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                for (int y = -viewDistance; y <= viewDistance; y++)
                {
                    Vector2Int coord = currentChunkCoord + new Vector2Int(x, y);
                    visibleCoords.Add(coord);

                    if (!activeChunks.ContainsKey(coord))
                    {
                        SpawnChunk(coord);
                    }
                }
            }

            // Limpiar chunks fuera de rango
            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (var chunk in activeChunks)
            {
                if (!visibleCoords.Contains(chunk.Key))
                {
                    toRemove.Add(chunk.Key);
                }
            }

            foreach (var coord in toRemove)
            {
                Destroy(activeChunks[coord]);
                activeChunks.Remove(coord);
            }
        }

        private void SpawnChunk(Vector2Int coord)
        {
            Vector3 pos = new Vector3(coord.x * chunkSize, coord.y * chunkSize, 0);
            GameObject newChunk = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
            newChunk.name = $"Chunk_{coord.x}_{coord.y}";
            
            // Asegurarse de que el chunk no tenga paredes y tenga un ligero solapamiento para evitar líneas
            var floorSetup = newChunk.GetComponent<FloorSetup>();
            if (floorSetup != null)
            {
                // Añadimos un pequeño margen de 0.05 para que los chunks se solapen ligeramente y no se vean rendijas
                floorSetup.arenaSize = new Vector2(chunkSize + 0.05f, chunkSize + 0.05f);
            }

            activeChunks.Add(coord, newChunk);

            // Spawnear decoración aleatoria
            SpawnDecor(newChunk.transform, coord);
        }

        private void SpawnDecor(Transform chunkParent, Vector2Int coord)
        {
            if (decorPrefabs == null || decorPrefabs.Length == 0) return;

            int count = Random.Range(minDecorPerChunk, maxDecorPerChunk + 1);
            float halfSize = chunkSize / 2f;

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = decorPrefabs[Random.Range(0, decorPrefabs.Length)];
                
                // Posición aleatoria dentro del chunk
                float randomX = Random.Range(-halfSize, halfSize);
                float randomY = Random.Range(-halfSize, halfSize);
                Vector3 localPos = new Vector3(randomX, randomY, 0);
                Vector3 worldPos = chunkParent.position + localPos;

                // EVITAR SPAWN ENCIMA DEL JUGADOR AL INICIO (0,0)
                if (coord == Vector2Int.zero && worldPos.magnitude < 5f)
                {
                    continue; // Demasiado cerca del centro inicial
                }

                GameObject decor = Instantiate(prefab, chunkParent);
                decor.transform.localPosition = localPos;
                
                // Variar un poco la escala y rotación para que no se vea repetitivo
                decor.transform.localScale *= Random.Range(0.8f, 1.2f);
                decor.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
            }
        }
    }
}

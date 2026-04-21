using UnityEngine;

public class SinglePlayerManager : MonoBehaviour
{
    public GameObject botPrefab;
    public Transform[] spawnPoints;
    public int score = 0;

    private float nextSpawnTime;
    public float timeBetweenWaves = 5f;
    private bool gameStarted = false;

    private void OnEnable()
    {
        Antigravity.Enemies.EnemyController.OnEnemyKilled += OnBotKilled;
    }

    private void OnDisable()
    {
        Antigravity.Enemies.EnemyController.OnEnemyKilled -= OnBotKilled;
    }

    public void StartMatch()
    {
        gameStarted = true;
        // Spawnear el primer grupo de 3 enemigos
        for(int i=0; i<3; i++) SpawnBot();
        nextSpawnTime = Time.time + timeBetweenWaves;
    }

    public float cleanupDistance = 40f;

    private void Update()
    {
        if (!gameStarted) return;

        // Oleadas por tiempo (como en el servidor)
        if (Time.time >= nextSpawnTime)
        {
            SpawnBot();
            nextSpawnTime = Time.time + timeBetweenWaves;
        }

        // LIMPIEZA AUTOMÁTICA DE BOTS LEJANOS (Optimización Mundo Abierto)
        CleanupDistantEnemies();
    }

    private void CleanupDistantEnemies()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var enemies = GameObject.FindObjectsOfType<Antigravity.Enemies.EnemyController>();
        foreach (var enemy in enemies)
        {
            float dist = Vector2.Distance(player.transform.position, enemy.transform.position);
            if (dist > cleanupDistance)
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    public void SpawnBot()
    {
        Vector2 spawnPos;
        var player = GameObject.FindGameObjectWithTag("Player");
        Vector2 center = player != null ? (Vector2)player.transform.position : Vector2.zero;
        
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            spawnPos = spawnPoints[randomIndex].position;
        }
        else
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            // Spawneamos a unos 12-15 metros del jugador para que no aparezcan en su cara
            spawnPos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 15f;
            Debug.Log("[SinglePlayerManager] Spawn infinito en: " + spawnPos);
        }

        if (botPrefab != null)
        {
            GameObject go = Instantiate(botPrefab, spawnPos, Quaternion.identity);
            var controller = go.GetComponent<Antigravity.Enemies.EnemyController>();
            if (controller != null)
            {
                // Inicializamos con 100 de vida (o más según la puntuación para que sea difícil)
                controller.Initialize("sp_" + score + "_" + Time.time, 100 + (score * 5));
            }
        }
        else
        {
            Debug.LogError("[SinglePlayerManager] ¡Falta el Bot Prefab! Arrastra el Orco a la casilla.");
        }
    }

    public void OnBotKilled()
    {
        if (!gameStarted) return; // IGNORAR muertes antes de que empiece la partida de verdad

        score++;
        Debug.Log("Bot killed! Current score: " + score);
        SpawnBot();
    }
}

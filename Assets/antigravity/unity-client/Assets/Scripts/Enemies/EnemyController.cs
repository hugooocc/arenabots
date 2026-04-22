using UnityEngine;

namespace Antigravity.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        public string EnemyId { get; private set; }
        public int Health { get; private set; }

        public static event System.Action OnEnemyKilled;

        [Header("Settings")]
        public float moveSpeed = 3.5f;
        public float rotationSpeed = 10f;
        
        [Header("Combat Settings")]
        public int attackDamage = 15;
        public float attackCooldown = 1.5f;
        private float lastAttackTime = -999f;
        
        private bool isDead = false;
        
        private Transform targetPlayer;
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            
            // Físicas: Rigidbody2D (Dynamic, Gravity: 0, Sleeping Mode: Start Awake)
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.sleepMode = RigidbodySleepMode2D.StartAwake;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 
            rb.freezeRotation = true; 

            // Asegurar visibilidad sobre el suelo (-10)
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 5;
        }

        public void Initialize(string id, int initialHealth)
        {
            EnemyId = id;
            Health = initialHealth;
            FindPlayer();
        }

        private void FindPlayer()
        {
            // Buscamos a TODOS los que tengan PlayerHealth (Locales y de Red)
            var allHealths = UnityEngine.Object.FindObjectsByType<Antigravity.Player.PlayerHealth>(FindObjectsSortMode.None);
            
            float minDistance = float.MaxValue;
            Transform bestTarget = null;

            foreach (var hp in allHealths)
            {
                // Solo nos interesan los que están vivos
                if (hp.currentHealth > 0)
                {
                    float dist = Vector3.Distance(transform.position, hp.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestTarget = hp.transform;
                    }
                }
            }

            if (bestTarget != null)
            {
                targetPlayer = bestTarget;
            }
            else {
                targetPlayer = null;
            }
        }

        private void FixedUpdate()
        {
            // Si no tenemos target o el que tenemos ha muerto, buscamos otro
            if (targetPlayer == null || (targetPlayer.TryGetComponent<Antigravity.Player.PlayerHealth>(out var hp) && hp.currentHealth <= 0)) 
            {
                FindPlayer();
                if (targetPlayer == null)
                {
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                    return;
                }
            }

            // Movimiento IA: Calcular la dirección hacia el jugador y aplicarla a rb.linearVelocity
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            // Mantener al zombie "recto" y solo girar la imagen (Flip X)
            if (direction.x > 0.1f) transform.localScale = new Vector3(1, 1, 1);
            else if (direction.x < -0.1f) transform.localScale = new Vector3(-1, 1, 1);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryAttack(collision.gameObject);
        }

        private void OnTriggerStay2D(Collider2D collider)
        {
            TryAttack(collider.gameObject);
        }

        private void TryAttack(GameObject hitObject)
        {
            // Solo atacamos si estamos vivos y ha pasado el cooldown
            if (Health > 0 && Time.time >= lastAttackTime + attackCooldown)
            {
                if (hitObject.CompareTag("Player"))
                {
                    Debug.Log($"[EnemyController] Detectado toque físico o trigger con: {hitObject.name}");

                    // Disparar animación de ataque
                    var animator = GetComponentInChildren<Animator>();
                    if (animator != null) animator.SetTrigger("Attack");

                    // Hacer daño al jugador
                    var playerHealth = hitObject.GetComponent<Antigravity.Player.PlayerHealth>();
                    if (playerHealth != null)
                    {
                        Debug.Log($"[EnemyController] Restando {attackDamage} de vida al jugador...");
                        playerHealth.TakeDamage(attackDamage);
                        lastAttackTime = Time.time;
                    }
                    else 
                    {
                        Debug.LogWarning("[EnemyController] ERROR: El jugador tocado no tiene puesto el script PlayerHealth.cs!!");
                    }
                }
            }
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            Debug.Log($"Enemigo {EnemyId} recibió {damage} de daño. Vida: {Health}");
            
            // Feedback visual: "Flash Rojo" en el Sprite
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                StartCoroutine(FlashRed(sr));
            }

            // Muerte local: 
            // 1. Si no tiene ID (modo sandbox/manual)
            // 2. Si estamos en modo un jugador (no hay servidor que mande la muerte)
            bool isSinglePlayer = Antigravity.Auth.GameSession.CurrentGameId == "singleplayer";
            if (Health <= 0 && (string.IsNullOrEmpty(EnemyId) || isSinglePlayer))
            {
                Die();
            }
        }

        private System.Collections.IEnumerator FlashRed(SpriteRenderer sr)
        {
            Color original = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (sr != null) sr.color = original;
        }

        public int xpReward = 25; // XP otorgada al morir

        public void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log($"¡Enemigo {EnemyId} eliminado!");
            Debug.Log($"[EnemyController] ¡El Orco {gameObject.name} (ID: {EnemyId}) ha muerto! Iniciando muerte...");

            OnEnemyKilled?.Invoke();

            if (Antigravity.Player.PlayerProgression.Instance != null)
            {
                Antigravity.Player.PlayerProgression.Instance.AddExperience(xpReward);
            }

            // Reproducir animación de muerte si existe
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                Debug.Log("[EnemyController] ¡Disparando trigger de muerte en el Animator!");
                animator.SetTrigger("Die");
                
                // Desactivar físicas y scripts para que sea un "cadáver" temporal
                if (rb != null) rb.linearVelocity = Vector2.zero;
                var colliders = GetComponentsInChildren<Collider2D>();
                foreach (var c in colliders) c.enabled = false;
                this.enabled = false; // Desactiva EnemyController
                
                Destroy(gameObject, 1.5f); // Da tiempo a la animación
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

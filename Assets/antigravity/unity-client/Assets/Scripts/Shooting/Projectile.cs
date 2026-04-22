using UnityEngine;
using Antigravity.Network;

namespace Antigravity.Shooting
{
    public class Projectile : MonoBehaviour
    {
        public string id;
        public string ownerId;
        public Vector2 direction;
        public float speed = 10f;
        public long spawnTimestamp;

        private Rigidbody2D rb;

        private void Start()
        {
            // Físicas: Inyectar por código un Rigidbody2D 
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            // Body Type: Dynamic, Gravity Scale: 0 y Collision Detection: Continuous
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true; // Evitar que el láser rote al chocar

            // Collider: Inyectar por código un CircleCollider2D como Trigger
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
            }
            collider.isTrigger = true;
            collider.radius = 0.5f; // Ajustar según el tamaño del sprite
        }

        private void FixedUpdate()
        {
            if (rb != null)
            {
                // Movimiento: Usar rb.linearVelocity (específico de Unity 6)
                rb.linearVelocity = direction * speed;
            }
        }

        public void Initialize(string id, string ownerId, Vector2 direction, float speed, long timestamp)
        {
            this.id = id;
            this.ownerId = ownerId;
            this.direction = direction.normalized;
            this.speed = speed;
            this.spawnTimestamp = timestamp;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Detección: Ignorar etiquetas (tags) y buscar directamente el componente EnemyController
            // Buscamos mediante GetComponentInParent por si acaso pusiste el Collider en un "hijo" del objeto
            Antigravity.Enemies.EnemyController enemy = collision.GetComponentInParent<Antigravity.Enemies.EnemyController>();
            
            if (enemy != null)
            {
                Debug.Log($"¡Impacto detectado en enemigo: {enemy.EnemyId}!");

                // Llamar a la función local de daño para feedback visual inmediato (Client-Side Prediction)
                enemy.TakeDamage(25); 

                // Enviar un payload JSON al servidor mediante NetworkManager
                ImpactPayload hit = new ImpactPayload {
                    tipo = "impacto_proyectil",
                    partidaId = Antigravity.Auth.GameSession.CurrentGameId ?? "singleplayer",
                    enemigoId = enemy.EnemyId,
                    dano = 25
                };

                string json = JsonUtility.ToJson(hit);
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.SendMessage(json);
                }
                
                Destroy(gameObject); // El proyectil se destruye al chocar
            }
        }
    }

}

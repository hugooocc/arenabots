using UnityEngine;
using System;

namespace Antigravity.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public int maxHealth = 100;
        public int currentHealth;

        public event Action<int, int> OnHealthChanged;
        public event Action OnPlayerDeath;
        
        private Animator animator;
        private static readonly int DieHash = Animator.StringToHash("Die");

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(Mathf.Max(0, currentHealth), maxHealth);
        }

        public void TakeDamage(int damage)
        {
            if (currentHealth <= 0) return;

            currentHealth -= damage;
            Debug.Log($"[PlayerHealth] OUCH! El jugador recibió {damage} de daño. Vida: {currentHealth}/{maxHealth}");
            
            OnHealthChanged?.Invoke(Mathf.Max(0, currentHealth), maxHealth);

        if (currentHealth <= 0)
        {
            if (Antigravity.Auth.GameSession.CurrentGameId != "singleplayer")
            {
                var nm = Antigravity.Shooting.NetworkManager.Instance;
                if (nm != null)
                {
                    string json = $"{{\"tipo\":\"player_dead\",\"partidaId\":\"{Antigravity.Auth.GameSession.CurrentGameId}\"}}";
                    nm.SendMessage(json);
                    Debug.Log("[PlayerHealth] Notificando muerte al servidor...");
                }
            }
            Die();
        }
    }

        private void Die()
        {
            Debug.Log("[PlayerHealth] ¡EL JUGADOR HA SIDO DERROTADO!");
            
            if (animator != null)
            {
                animator.SetTrigger(DieHash);
            }
            
            OnPlayerDeath?.Invoke();
            
            // Nota: Podrías usar un Animation Event para desactivar el objeto al final
            // Por ahora lo dejamos activo para que se vea la animación de muerte.
            // gameObject.SetActive(false); 
        }
    }
}

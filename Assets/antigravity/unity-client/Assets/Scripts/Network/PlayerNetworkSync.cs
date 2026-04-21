using UnityEngine;
using Antigravity.Shooting;

namespace Antigravity.Network
{
    public class PlayerNetworkSync : MonoBehaviour
    {
        public float syncFrequency = 0.05f; // 20 times per second
        private float nextSyncTime = 0f;

        private Rigidbody2D rb;
        private Animator animator;
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (Time.time >= nextSyncTime)
            {
                if (NetworkManager.Instance != null && NetworkManager.Instance.isActiveAndEnabled) {
                    SendUpdate();
                }
                nextSyncTime = Time.time + syncFrequency;
            }
        }

        private void SendUpdate()
        {
            if (NetworkManager.Instance == null) return;

            Vector2 pos = transform.position;
            Vector2 vel = rb != null ? rb.linearVelocity : Vector2.zero;
            Vector2 looking = Vector2.down; // Fallback

            if (animator != null)
            {
                looking.x = animator.GetFloat(MoveXHash);
                looking.y = animator.GetFloat(MoveYHash);
            }

            var message = new MoveMessage
            {
                tipo = "movimiento",
                userId = Antigravity.Auth.GameSession.UserId,
                posicion = new Vector2Payload(pos),
                velocidad = new Vector2Payload(vel),
                mirando = new Vector2Payload(looking)
            };

            NetworkManager.Instance.SendMessage(JsonUtility.ToJson(message));
        }
    }
}

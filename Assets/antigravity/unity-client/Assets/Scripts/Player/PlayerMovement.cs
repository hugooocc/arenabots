using UnityEngine;
using UnityEngine.InputSystem;

namespace Antigravity.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        public float moveSpeed = 5f;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Animator animator;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();

            // Es crucial que el rigidbody2d tenga gravedad en 0 para un juego top-down (arena)
            if (rb.gravityScale != 0f)
            {
                rb.gravityScale = 0f;
            }

            // La interpolación es la clave para que la cámara no dé "tirones" cuando el jugador se mueve en FixedUpdate
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            
            // FÍSICAS: Bloquear la rotación Z para que el personaje no dé volteretas al chocar
            rb.freezeRotation = true;
        }

        public bool canMove = true;
        private float lastShootTime = -999f;
        public float lookOverrideDuration = 0.5f;

        // --- PREDICTION & RECONCILIATION ---
        private struct PredictionState {
            public int seq;
            public Vector2 input;
            public Vector2 pos;
        }
        private System.Collections.Generic.List<PredictionState> pendingStates = new System.Collections.Generic.List<PredictionState>();
        private int currentSeq = 0;
        private float reconciliationThreshold = 0.1f;

        private void Start()
        {
            if (Antigravity.Shooting.NetworkManager.Instance != null) {
                Antigravity.Shooting.NetworkManager.Instance.OnMessageReceived += HandleNetworkUpdate;
            }
        }

        private void OnDestroy()
        {
            if (Antigravity.Shooting.NetworkManager.Instance != null) {
                Antigravity.Shooting.NetworkManager.Instance.OnMessageReceived -= HandleNetworkUpdate;
            }
        }

        private void HandleNetworkUpdate(string msg) {
            if (!msg.Contains("\"tipo\":\"player_update\"")) return;
            
            var data = JsonUtility.FromJson<PlayerUpdateData>(msg);
            if (data.userId == Antigravity.Auth.GameSession.UserId) {
                Reconciliate(data.pos, data.seq);
            }
        }

        private void Reconciliate(Vector2Payload serverPosPayload, int seq) {
            Vector2 serverPos = new Vector2(serverPosPayload.x, serverPosPayload.y);
            
            // 1. Remove states older than or equal to seq
            pendingStates.RemoveAll(s => s.seq <= seq);

            // 2. Check discrepancy with last processed sequence
            // For simplicity in this MVP, we assume the last predicted state matches the sequence
            // but we'll compare current position with server position after re-applying pending
            float dist = Vector2.Distance(rb.position, serverPos);
            if (dist > reconciliationThreshold) {
                Debug.Log($"[Reconciliation] Correcting player: Dist {dist}. Seq {seq}");
                rb.position = serverPos;

                // 3. Re-simulate pending inputs
                foreach (var state in pendingStates) {
                    rb.position += state.input.normalized * moveSpeed * 0.05f; // Constant server-step
                }
            }
        }

        [System.Serializable]
        private class PlayerUpdateData {
            public string tipo;
            public string userId;
            public Vector2Payload pos;
            public int seq;
        }

        [System.Serializable]
        private class InputMessage {
            public string tipo = "movimiento";
            public Vector2 input;
            public int seq;
        }

        public void NotifyShoot(Vector2 direction)
        {
            lastShootTime = Time.time;
            if (animator != null)
            {
                animator.SetFloat(MoveXHash, direction.x);
                animator.SetFloat(MoveYHash, direction.y);
            }
        }

        private void FixedUpdate()
        {
            if (!canMove || GetComponent<Antigravity.Network.NetworkPlayer>() != null) 
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 movement = Vector2.zero;
            // Get inputs
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) movement.y += 1f;
                if (Keyboard.current.sKey.isPressed) movement.y -= 1f;
                if (Keyboard.current.aKey.isPressed) movement.x -= 1f;
                if (Keyboard.current.dKey.isPressed) movement.x += 1f;
            }
            
            if (movement == Vector2.zero)
            {
                movement.x = Input.GetAxisRaw("Horizontal");
                movement.y = Input.GetAxisRaw("Vertical");
            }

            bool isMultiplayer = Antigravity.Auth.GameSession.CurrentGameId != "singleplayer";

            if (isMultiplayer) {
                // prediction
                currentSeq++;
                Vector2 velocity = movement.normalized * moveSpeed;
                rb.position += velocity * Time.fixedDeltaTime; 

                // Store for reconciliation
                pendingStates.Add(new PredictionState { seq = currentSeq, input = movement, pos = rb.position });

                // Send to server
                if (Antigravity.Shooting.NetworkManager.Instance != null && Antigravity.Shooting.NetworkManager.Instance.IsConnected) {
                    var msg = new InputMessage { input = movement, seq = currentSeq };
                    Antigravity.Shooting.NetworkManager.Instance.SendMessage(JsonUtility.ToJson(msg));
                }
            } else {
                // Standard Authoritative Movement (Local)
                rb.linearVelocity = movement.normalized * moveSpeed;
            }

            // ANIMACIÓN
            if (animator != null)
            {
                if (Time.time > lastShootTime + lookOverrideDuration && movement != Vector2.zero)
                {
                    animator.SetFloat(MoveXHash, movement.x);
                    animator.SetFloat(MoveYHash, movement.y);
                }
                animator.SetFloat(SpeedHash, rb.linearVelocity.magnitude);
            }
        }
    }
}

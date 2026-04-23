using UnityEngine;

namespace Antigravity.Network
{
    public class NetworkPlayer : MonoBehaviour
    {
        public string userId;
        public string username;

        private Vector3 targetPosition;
        private Vector2 lastVelocity;
        private Vector2 lastLookDirection;

        public float interpolationSpeed = 10f;

        private Animator animator;
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            targetPosition = transform.position;

            // PHYSICS ISOLATION:
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true; // Stay simulated but kinematic to allow manual movement
            }
        }

        public void UpdateState(Vector2Payload position, int seq)
        {
            Vector3 previousTarget = targetPosition;
            targetPosition = new Vector3(position.x, position.y, 0);
            
            // Calculate pseudo-velocity for animations
            Vector2 delta = (Vector2)targetPosition - (Vector2)previousTarget;
            
            if (animator != null)
            {
                if (delta.magnitude > 0.01f) {
                    animator.SetFloat(MoveXHash, delta.x);
                    animator.SetFloat(MoveYHash, delta.y);
                }
                animator.SetFloat(SpeedHash, delta.magnitude * 20f); // 20Hz normalize
            }

            // Flip sprite based on direction
            if (Mathf.Abs(delta.x) > 0.01f) {
                transform.localScale = new Vector3(delta.x > 0 ? 1 : -1, 1, 1);
            }
        }

        private void Update()
        {
            // Smooth interpolation to target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolationSpeed);
        }
    }
}

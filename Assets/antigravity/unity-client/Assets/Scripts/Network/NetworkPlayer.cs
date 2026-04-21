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
                rb.fullKinematicContacts = false;
                rb.simulated = true; // Stay simulated but kinematic to allow manual movement
            }
        }

        public void UpdateState(Vector2Payload position, Vector2Payload velocity, Vector2Payload looking)
        {
            targetPosition = new Vector3(position.x, position.y, 0);
            
            if (animator != null)
            {
                animator.SetFloat(MoveXHash, looking.x);
                animator.SetFloat(MoveYHash, looking.y);
                animator.SetFloat(SpeedHash, new Vector2(velocity.x, velocity.y).magnitude);
            }
        }

        private void Update()
        {
            // Smooth interpolation to target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolationSpeed);
        }
    }
}

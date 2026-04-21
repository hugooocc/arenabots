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
            if (!canMove) 
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 movement = Vector2.zero;

            // 1. New Input System (Moderno)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) movement.y += 1f;
                if (Keyboard.current.sKey.isPressed) movement.y -= 1f;
                if (Keyboard.current.aKey.isPressed) movement.x -= 1f;
                if (Keyboard.current.dKey.isPressed) movement.x += 1f;
            }
            
            // 2. Fallback: Input Manager (Clásico - por si el nuevo falla en su configuración)
            if (movement == Vector2.zero)
            {
                movement.x = Input.GetAxisRaw("Horizontal");
                movement.y = Input.GetAxisRaw("Vertical");
            }

            if (movement != Vector2.zero)
            {
                Debug.Log($"[PlayerMovement] Detectado Intento de Movimiento: {movement}");
            }

            // Normalizamos para no ir más rápido en diagonal y aplicamos velocidad
            Vector2 velocity = movement.normalized * moveSpeed;
            rb.linearVelocity = velocity;

            // ANIMACIÓN: Sincronización con el Animator (Blend Tree de 8 direcciones)
            if (animator != null)
            {
                // Solo actualizamos la dirección si no hemos disparado recientemente
                // Esto permite el strafing (caminar de espaldas/lado mientras se dispara)
                if (Time.time > lastShootTime + lookOverrideDuration)
                {
                    if (movement != Vector2.zero)
                    {
                        animator.SetFloat(MoveXHash, movement.x);
                        animator.SetFloat(MoveYHash, movement.y);
                    }
                }
                
                animator.SetFloat(SpeedHash, velocity.magnitude);
            }
            
            // LOG DE DIAGNÓSTICO AVANZADO PARA DEDUCIR LA ILUSIÓN
            if (movement != Vector2.zero) 
            {
                Camera cam = Camera.main;
                GameObject floor = GameObject.Find("ArenaFloor") ?? UnityEngine.Object.FindAnyObjectByType<Antigravity.Environment.FloorSetup>()?.gameObject;
                
                string camPos = cam != null ? cam.transform.position.ToString() : "N/A";
                string floorPos = floor != null ? floor.transform.position.ToString() : "N/A";
                string floorParent = (floor != null && floor.transform.parent != null) ? floor.transform.parent.name : "None";

                Debug.Log($"[Player] Pos: {transform.position} | [Camera] Pos: {camPos} | [Floor] Pos: {floorPos} (Parent: {floorParent})");
            }
        }
    }
}

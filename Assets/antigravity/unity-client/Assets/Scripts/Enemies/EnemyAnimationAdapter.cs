using UnityEngine;

namespace Antigravity.Enemies
{
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimationAdapter : MonoBehaviour
    {
        public EnemyController enemyController;
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            
            if (enemyController == null)
            {
                enemyController = GetComponentInParent<EnemyController>();
            }
        }

        private void Update()
        {
            if (enemyController == null || animator == null) return;

            // Suponemos que si se está moviendo a más de 0.1 de velocidad o si su RigidBody tiene velocidad
            // reproducimos la animación de correr.
            var rb = enemyController.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                bool isMoving = rb.linearVelocity.sqrMagnitude > 0.1f;
                animator.SetBool("IsMoving", isMoving);
            }
        }

        // Este método se llamará desde EnemyController o al revés.
        // Pero para ser más fáciles, si en EnemyController sobreescribimos Die() para no destruir el objeto al instante, 
        // aquí podemos lanzar el trigger.
    }
}

using UnityEngine;
using Antigravity.Player;

namespace Antigravity.CameraSystem
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        private Transform lastTarget;

        private void LateUpdate()
        {
            // Reintentar buscar el jugador si el actual es null o no es el correcto
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (target == null) return;

            // Si cambiamos de player (ej: al empezar), teletransportar cámara instantáneamente
            if (target != lastTarget)
            {
                Debug.Log($"[CameraFollow] Siguiendo a nuevo objetivo: {target.name} en {target.position}");
                transform.position = new Vector3(target.position.x, target.position.y, -10f);
                lastTarget = target;
            }

            // Fix visual glitch where camera loses Z-depth or drifts
            transform.position = new Vector3(target.position.x, target.position.y, -10f);
        }
    }
}

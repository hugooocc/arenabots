using UnityEngine;
using Antigravity.Player;
using Antigravity.Network;

namespace Antigravity.CameraSystem
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        private Transform lastTarget;

        private void LateUpdate()
        {
            // Reintentar buscar un objetivo VIVO si el actual es null o ha muerto
            if (target == null || !IsTargetAlive(target))
            {
                target = FindAnyAlivePlayer();
            }

            if (target == null) return;

            // Si cambiamos de target, teletransportar cámara instantáneamente
            if (target != lastTarget)
            {
                Debug.Log($"[CameraFollow] Cambiando a objetivo VIVO: {target.name}");
                transform.position = new Vector3(target.position.x, target.position.y, -10f);
                lastTarget = target;
            }

            transform.position = new Vector3(target.position.x, target.position.y, -10f);
        }

        private bool IsTargetAlive(Transform t)
        {
            // El local player usa PlayerHealth
            PlayerHealth ph = t.GetComponent<PlayerHealth>();
            if (ph != null) return ph.IsAlive;

            // Los aliados usan NetworkPlayer
            NetworkPlayer np = t.GetComponent<NetworkPlayer>();
            if (np != null) return np.IsAlive;

            return t.gameObject.activeInHierarchy;
        }

        private Transform FindAnyAlivePlayer()
        {
            // 1. Priorizar Local Player si está vivo
            PlayerHealth local = FindAnyObjectByType<PlayerHealth>();
            if (local != null && local.IsAlive) return local.transform;

            // 2. Si no, buscar cualquier NetworkPlayer (aliado) vivo
            NetworkPlayer[] remotes = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var remote in remotes)
            {
                if (remote.IsAlive) return remote.transform;
            }

            return null;
        }
    }
}

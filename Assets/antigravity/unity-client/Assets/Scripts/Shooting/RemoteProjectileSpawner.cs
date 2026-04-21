using UnityEngine;
using Antigravity.Network;
using System;

namespace Antigravity.Shooting
{

    public class RemoteProjectileSpawner : MonoBehaviour
    {
        public GameObject projectilePrefab;
        public float projectileSpeed = 10f;

        private void Start()
        {
            NetworkManager.Instance.OnMessageReceived += HandleMessage;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived -= HandleMessage;
            }
        }

        private void HandleMessage(string message)
        {
            try
            {
                RetransmissionPayload payload = JsonUtility.FromJson<RetransmissionPayload>(message);
                if (payload.tipo == "disparo_retransmision")
                {
                    // If it's not our own projectile (handled by prediction), spawn it
                    // Or we could always spawn it and let ShootController handle reconciliation
                    OnRetransmissionReceived(payload);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to parse message: " + e.Message);
            }
        }

        private void OnRetransmissionReceived(RetransmissionPayload payload)
        {
            // Simple check: if this is our own, we already spawned it
            if (payload.jugadorId == Antigravity.Auth.GameSession.UserId)
            {
                return;
            }

            Vector2 pos = new Vector2(payload.posicion.x, payload.posicion.y);
            Vector2 dir = new Vector2(payload.direccion.x, payload.direccion.y);

            GameObject go = Instantiate(projectilePrefab, pos, Quaternion.identity);
            Projectile projectile = go.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(payload.proyectilId, payload.jugadorId, dir, projectileSpeed, payload.timestamp);
            }
        }
    }
}

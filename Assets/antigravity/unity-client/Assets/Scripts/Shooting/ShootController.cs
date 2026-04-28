using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Antigravity.Network;

namespace Antigravity.Shooting
{
    [Serializable]
    public class DisparoPayload
    {
        public string tipo = "disparo";
        public string jugadorId;
        public string partidaId = "singleplayer"; 
        public Vector2Payload posicion;
        public Vector2Payload direccion;
        public long timestamp;
    }

    [Serializable]
    public class DisparoRetransmitidoPayload
    {
        public string tipo = "disparo_retransmitido";
        public string jugadorId;
        public Vector2Payload posicion;
        public Vector2Payload direccion;
        public float velocidad;
        public long timestamp;
    }

    public class ShootController : MonoBehaviour
    {
        public GameObject projectilePrefab;
        public float projectileSpeed = 10f;
        public bool isSinglePlayer = false;

        private Animator animator;
        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private Antigravity.Player.PlayerMovement playerMovement;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            playerMovement = GetComponentInParent<Antigravity.Player.PlayerMovement>() ?? GetComponent<Antigravity.Player.PlayerMovement>();
            if (!isSinglePlayer && NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived += HandleMessage;
            }
        }

        private void OnDestroy()
        {
            if (!isSinglePlayer && NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived -= HandleMessage;
            }
        }

        private void Update()
        {
            bool firePressed = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) firePressed = true;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) firePressed = true;

            if (firePressed)
            {
                Shoot();
            }
        }

        private void Shoot()
        {
            Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            Vector2 direction = (mousePos - (Vector2)transform.position).normalized;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string currentUserId = Antigravity.Auth.GameSession.UserId;

            // Client-side prediction: Spawn projectile immediately
            Projectile proj = SpawnProjectile(currentUserId, transform.position, direction, timestamp);
            
            if (!isSinglePlayer && NetworkManager.Instance != null && NetworkManager.Instance.isActiveAndEnabled)
            {
                // Generate and send payload
                SendDisparoEvent(transform.position, direction, timestamp);
            }

            // ANIMACIÓN
            if (playerMovement != null)
            {
                playerMovement.NotifyShoot(direction);
            }
            
            if (animator != null)
            {
                animator.SetTrigger(ShootHash);
            }
        }

        private Projectile SpawnProjectile(string ownerId, Vector2 position, Vector2 direction, long timestamp)
        {
            if (projectilePrefab == null)
            {
                Debug.LogError("[ShootController] Missing Projectile Prefab!");
                return null;
            }
            GameObject go = Instantiate(projectilePrefab, position, Quaternion.identity);
            Projectile projectile = go.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize("p_" + timestamp, ownerId, direction, projectileSpeed, timestamp);
            }
            return projectile;
        }

        private void SendDisparoEvent(Vector2 position, Vector2 direction, long timestamp)
        {
            DisparoPayload payload = new DisparoPayload
            {
                tipo = "disparo",
                jugadorId = Antigravity.Auth.GameSession.UserId,
                partidaId = Antigravity.Auth.GameSession.CurrentGameId ?? "singleplayer",
                posicion = new Vector2Payload(position),
                direccion = new Vector2Payload(direction),
                timestamp = timestamp
            };

            string json = JsonUtility.ToJson(payload);
            NetworkManager.Instance.SendMessage(json);
        }

        private void HandleMessage(string message)
        {
            if (message.Contains("\"tipo\":\"disparo_retransmitido\""))
            {
                try {
                    var data = JsonUtility.FromJson<DisparoRetransmitidoPayload>(message);
                    
                    // Si el disparo es nuestro, lo ignoramos (ya se predijo el cliente)
                    if (data.jugadorId != Antigravity.Auth.GameSession.UserId)
                    {
                        Vector2 serverPos = new Vector2(data.posicion.x, data.posicion.y);
                        Vector2 serverDir = new Vector2(data.direccion.x, data.direccion.y);
                        
                        // Spawn projectile using network data
                        SpawnProjectile(data.jugadorId, serverPos, serverDir, data.timestamp);
                    }
                } catch (Exception e) {
                    Debug.LogError("[ShootController] Error parsing remote shoot limit: " + e.Message);
                }
            }
        }
    }
}

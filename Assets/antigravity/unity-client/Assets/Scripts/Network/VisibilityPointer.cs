using UnityEngine;

namespace Antigravity.Network
{
    public class VisibilityPointer : MonoBehaviour
    {
        private LineRenderer lr;
        private Transform localPlayer;

        void Start()
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.startWidth = 0.5f;
            lr.endWidth = 0.5f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
            lr.sortingOrder = 999;
        }

        void Update()
        {
            if (localPlayer == null) {
                var local = Object.FindAnyObjectByType<Antigravity.Player.PlayerMovement>();
                if (local != null && local.GetComponent<NetworkPlayer>() == null) {
                    localPlayer = local.transform;
                }
            }

            if (localPlayer != null) {
                lr.SetPosition(0, localPlayer.position);
                lr.SetPosition(1, transform.position);
            }
            
            // Force visibility and correct scale/layer
            transform.localScale = new Vector3(1f, 1f, 1f);
            gameObject.layer = 0; // Default layer

            var renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach(var r in renderers) {
                r.enabled = true;
                r.color = Color.red;
                r.sortingOrder = 1000;
            }

            // Remove animator to prevent it from hiding the sprite or overriding color
            var anim = GetComponentInChildren<Animator>();
            if (anim != null) Destroy(anim);
        }
    }
}

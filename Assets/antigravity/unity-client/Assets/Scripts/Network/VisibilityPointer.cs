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

            // [BRUTE FORCE] Create a procedural white square so something is ALWAYS visible
            if (GetComponent<SpriteRenderer>() == null || GetComponent<SpriteRenderer>().sprite == null) {
                var sr = gameObject.AddComponent<SpriteRenderer>();
                Texture2D tex = new Texture2D(64, 64);
                for(int x=0; x<64; x++) for(int y=0; y<64; y++) tex.SetPixel(x,y, Color.white);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0,0,64,64), new Vector2(0.5f, 0.5f));
                sr.color = Color.red;
                sr.sortingOrder = 1001;
                Debug.Log("[VisibilityPointer] Creado sprite procedural rojo para " + gameObject.name);
            }

            var renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach(var r in renderers) {
                r.enabled = true;
                if (r.sprite != null) r.color = Color.red;
                r.sortingOrder = 1000;
            }

            // Remove animator to prevent it from hiding the sprite or overriding color
            var anim = GetComponentInChildren<Animator>();
            if (anim != null) Destroy(anim);
        }
    }
}

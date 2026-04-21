using UnityEngine;

namespace Antigravity.Environment
{
    [ExecuteInEditMode]
    public class FloorSetup : MonoBehaviour
    {
        [Header("Settings")]
        public Sprite floorSprite;
        public Vector2 arenaSize = new Vector2(30, 30);
        public int sortingOrder = -10;


        private void Start()
        {
            SetupFloor();
        }

        [ContextMenu("Setup Floor")]
        public void SetupFloor()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

            if (floorSprite != null)
            {
                sr.sprite = floorSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = arenaSize;
                sr.sortingOrder = sortingOrder;

                // FIX COLOR GRISÁCEO: Forzamos el material por defecto para que no se vea oscuro si no hay luces cerca
                if (sr.sharedMaterial == null || sr.sharedMaterial.name.Contains("Lit"))
                {
                    sr.material = new Material(Shader.Find("Sprites/Default"));
                }
            }

            // CREAR PAREDES INVISIBLES EN LOS BORDES (DESACTIVADO PARA MUNDO ABIERTO)
            EdgeCollider2D edge = GetComponent<EdgeCollider2D>();
            if (edge != null)
            {
                if (Application.isPlaying) Destroy(edge);
                else DestroyImmediate(edge);
            }
            
        }
    }
}

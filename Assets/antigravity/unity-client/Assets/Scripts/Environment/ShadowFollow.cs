using UnityEngine;

namespace Antigravity.Environment
{
    public class ShadowFollow : MonoBehaviour
    {
        [Header("Settings")]
        public Vector3 worldOffset = new Vector3(0, -0.2f, 0);

        void LateUpdate()
        {
            if (transform.parent != null)
            {
                // Posicionar exactamente bajo el padre en coordenadas de mundo
                transform.position = transform.parent.position + worldOffset;
                
                // Forzar rotación cero (siempre plana contra el suelo)
                transform.rotation = Quaternion.identity;
            }
        }
    }
}

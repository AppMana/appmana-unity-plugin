using UnityEngine;
using UnityEngine.EventSystems;

namespace AppManaPublic.Configuration
{
    public class RenderNonStreamingCamera : UIBehaviour
    {
        [SerializeField] private Camera m_Camera;

        protected override void Start()
        {
            base.Start();
            if (m_Camera == null)
            {
                Debug.LogWarning($"{nameof(RenderNonStreamingCamera)} should have camera set", this);
            }
        }

        private void LateUpdate()
        {
            if (!Application.isBatchMode || !m_Camera)
            {
                return;
            }

            m_Camera.Render();
        }
    }
}
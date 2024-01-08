using UnityEngine;
using UnityEngine.EventSystems;

namespace AppManaPublic.Configuration
{
    /// <summary>
    /// Renders cameras that are not meant for streaming to the end user.
    /// </summary>
    /// <para>
    /// By default, only the camera associated with a <see cref="RemotePlayableConfiguration"/> is rendered every frame.
    /// This script should be attached to cameras that still need to be rendered, such as cameras used for special
    /// in-game rendering effects.
    /// </para>
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
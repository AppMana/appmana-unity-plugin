using UnityEngine;
using UnityEngine.Scripting;

namespace AppManaPublic.Configuration
{
    /// <summary>
    /// This class is used by the runtime to add a remote playable configuration when none is specified in the scene.
    /// </summary>
    [Preserve, DefaultExecutionOrder(10000), AddComponentMenu("")]
    internal class InferredRemotePlayableConfiguration : RemotePlayableConfiguration
    {
        [SerializeField] private bool m_Initialized;

        protected override void AwakeImpl()
        {
            if (m_Initialized)
            {
                base.AwakeImpl();
            }
        }

        protected override void StartImpl()
        {
            if (m_Initialized)
            {
                base.StartImpl();
            }
        }

        protected override void OnEnableImpl()
        {
            if (m_Initialized)
            {
                base.OnEnableImpl();
            }
        }

        protected override void OnDisableImpl()
        {
            if (m_Initialized)
            {
                base.OnDisableImpl();
            }
        }

        public void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            m_StreamInEditMode = true;
            m_Initialized = true;
            AwakeImpl();
            OnEnableImpl();
            StartImpl();
        }
    }
}
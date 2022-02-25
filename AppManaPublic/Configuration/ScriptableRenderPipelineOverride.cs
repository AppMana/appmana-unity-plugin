using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace AppManaPublic.Configuration
{
    [ExecuteAlways]
    public class ScriptableRenderPipelineOverride : UIBehaviour
    {
        [SerializeField] private RenderPipelineAsset m_Asset;

        public RenderPipelineAsset asset => m_Asset;

        protected override void Awake()
        {
            base.Awake();
            SetDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        private void SetDirty()
        {
            if (GraphicsSettings.renderPipelineAsset != m_Asset)
            {
                GraphicsSettings.renderPipelineAsset = m_Asset;
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif
    }
}
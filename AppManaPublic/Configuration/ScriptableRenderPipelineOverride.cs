using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace AppManaPublic.Configuration
{
    /// <summary>
    /// Sets the pipeline in multi-pipeline projects.
    /// </summary>
    [ExecuteAlways, Obsolete]
    internal class ScriptableRenderPipelineOverride : UIBehaviour
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
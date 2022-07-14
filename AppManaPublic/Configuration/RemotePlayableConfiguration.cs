using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace AppManaPublic.Configuration
{
    [Preserve, DefaultExecutionOrder(10000)]
    public class RemotePlayableConfiguration : UIBehaviour
    {
        [Header("Setup")] [SerializeField, Tooltip("Set this to the camera to stream for this player")]
        private Camera m_Camera;

        [SerializeField, Tooltip("Set this to the audio listener for this player, or leave null to disable audio")]
        private AudioListener m_AudioListener;

        [SerializeField, Tooltip("Set this to canvas scalers for the player's canvas, used to adjust display DPI")]
        private CanvasScaler[] m_CanvasScalers = new CanvasScaler[0];

        [SerializeField, Tooltip("Specify a design scale for your UI - typically this is 1.0, 2.0 or 3.0")]
        private float m_BaseScale = 1f;

        [SerializeField, Tooltip("Called when this player connects to the experience")]
        private UnityEvent m_OnPlayerConnected;

        [SerializeField, Tooltip("Called when this player disconnects from the experience")]
        private UnityEvent m_OnPlayerDisconnected;

        [Header("Advanced")]
        [SerializeField, Tooltip("Set this to limit event system callbacks to objects in this hierarchy")]
        private Transform m_InputsOnlyAffectThisHierarchyOrAny;

        [SerializeField, Tooltip("Contact us for editor streaming support")]
        private bool m_EnableStreamingInEditor;

        /// <summary>
        /// Set to <c>true</c> when we're in editor and a player connected invocation was requested.
        /// </summary>
        private bool m_RequestedOnPlayerConnectedInvoke;

        private bool m_DidCallInStart = false;

        public UnityEvent onPlayerConnected => m_OnPlayerConnected;

        public UnityEvent onPlayerDisconnected => m_OnPlayerDisconnected;

        public float baseScale => m_BaseScale;

        public Camera camera1
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        public AudioListener audioListener
        {
            get => m_AudioListener;
            set => m_AudioListener = value;
        }

        public CanvasScaler[] canvasScalers => m_CanvasScalers;

        public Transform inputsOnlyAffectThisHierarchyOrAny => m_InputsOnlyAffectThisHierarchyOrAny;

        protected override void Start()
        {
            if (m_RequestedOnPlayerConnectedInvoke && !m_DidCallInStart)
            {
                m_RequestedOnPlayerConnectedInvoke = false;
                m_DidCallInStart = true;
                m_OnPlayerConnected.Invoke();
            }
        }

        protected override void OnEnable()
        {
            if (Application.isEditor && !m_EnableStreamingInEditor)
            {
                if (!m_DidCallInStart)
                {
                    // delay this until Start() has been called, to give time for all the other user scripts to have run
                    m_RequestedOnPlayerConnectedInvoke = true;
                }
                else
                {
                    m_OnPlayerConnected.Invoke();
                }

                return;
            }

            PluginBase.EnsurePlugins();
        }

        protected override void OnDisable()
        {
            if (Application.isEditor && !m_EnableStreamingInEditor)
            {
                m_OnPlayerDisconnected.Invoke();
                return;
            }

            base.OnDisable();
        }
    }
}
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
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

        [SerializeField] private InputActionAsset m_Actions;

        [SerializeField, Tooltip("Set this to canvas scalers for the player's canvas, used to adjust display DPI")]
        private CanvasScaler[] m_CanvasScalers = new CanvasScaler[0];

        [SerializeField, Tooltip("Called when this player connects to the experience")]
        private UnityEvent m_OnPlayerConnected;

        [SerializeField, Tooltip("Called when this player disconnects from the experience")]
        private UnityEvent m_OnPlayerDisconnected;

        [Header("Advanced")]
        [SerializeField, Tooltip("Set this to limit event system callbacks to objects in this hierarchy")]
        private Transform m_InputsOnlyAffectThisHierarchyOrAny;

        [SerializeField, Tooltip("Contact us for editor streaming support")]
        private bool m_StreamInEditMode;

        [Header("Obsolete")]
        [SerializeField, Tooltip("(Obsolete) Specify a design scale for your UI - typically this is 1.0, 2.0 or 3.0")]
        private float m_BaseScale = 1f;

        /// <summary>
        /// Set to <c>true</c> when we're in editor and a player connected invocation was requested.
        /// </summary>
        private bool m_RequestedOnPlayerConnectedInvoke;

        private bool m_DidCallInStart = false;

        public UnityEvent onPlayerConnected => m_OnPlayerConnected;

        public UnityEvent onPlayerDisconnected => m_OnPlayerDisconnected;
        private InputUser m_User;
        internal InputUser user => m_User;
        private int m_Index;

        internal int index => m_Index;

        private static int m_Counter = -1;

        public InputActionAsset actions => m_Actions;

        protected override void Awake()
        {
            m_Index = Interlocked.Increment(ref m_Counter);
            base.Awake();

            var count = FindObjectsOfType<RemotePlayableConfiguration>(true).Length;

            if (m_Actions == null)
            {
                if (count > 1)
                {
                    // if there is a PlayerInput, warn that using it is a misconfiguration
                    var playerInput = camera.GetComponentInChildren<PlayerInput>();
                    if (playerInput != null)
                    {
                        Debug.LogError(
                            $"In a multiplayer game, {nameof(RemotePlayableConfiguration)} replaces {nameof(PlayerInput)}. Remove the {nameof(PlayerInput)} attached to {playerInput.gameObject.name}",
                            playerInput);
                    }

                    Debug.LogWarning(
                        $"Set the Actions field on this object.",
                        this);
                }

                return;
            }

            // clone the actions
            var newActionsName = m_Actions.name;
            m_Actions = Instantiate(m_Actions);
            m_Actions.name = $"{newActionsName} for Player {m_User.id}";

            // associate with a user, multiple players
            if (count <= 1)
            {
                return;
            }

            m_User = InputUser.CreateUserWithoutPairedDevices();
            m_User.AssociateActionsWithUser(m_Actions);
            
            // Find the input modules associated with this user
            // todo: make this configurable
            foreach (var inputSystemUIModule in GetComponentsInChildren<InputSystemUIInputModule>(true))
            {
                // this automatically finds the corresponding actions, if they exist
                inputSystemUIModule.actionsAsset = m_Actions;
            }
            m_Actions.Enable();
        }

        internal void PerformPairingWithDevice(InputDevice device)
        {
            m_User = InputUser.PerformPairingWithDevice(device, m_User);
        }

        [Obsolete] public float baseScale => m_BaseScale;

        [Obsolete]
        public Camera camera1
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        public new Camera camera
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
        internal bool enableStreamingInEditor => m_StreamInEditMode;

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
            if (Application.isEditor && !m_StreamInEditMode)
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
            if (Application.isEditor && !m_StreamInEditMode)
            {
                m_OnPlayerDisconnected.Invoke();
                return;
            }

            base.OnDisable();
        }
    }
}
﻿using System;
using System.Threading;
using AppMana.UI.TMPro;
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
    /// <summary>
    /// Configures a streamable player in your game.
    /// </summary>
    /// <para>Connect the player's corresponding <c>Camera</c> (usually the main camera) to the Camera slot. This is the
    /// minimum amount of configuration needed for a working streamed player.</para>
    /// <para>If your game has UGUI-based UI, add the canvas scalers to this component in order to correctly set their
    /// scaling based on devices. You should use <see cref="CanvasScaler.ScaleMode.ConstantPixelSize"/> to emulate
    /// responsive web browser scaling.</para>
    /// <para>If you're not sure where to put this component, add it to the root of the hierarchy that represents a
    /// single player. For example, this component should be the parent of the hierarchy that contains the main camera
    /// and canvas UI for the player.</para>
    /// <para>Each player in a multiplayer game should have an active game object in the scene with this component
    /// attached to it. This means you should create a prefab with the player's camera and canvases with this component
    /// attached to the root of the prefab hierarchy, then create two or more references to it in your scene for
    /// multiplayer. You can visualize the different players by setting a distinct Display for each player's camera, and
    /// opening multiple game views visualizing each display. AppMana will automatically emulate the input for each
    /// player.</para>
    /// <para>AppMana uses Input System to make multiplayer work. You must drag and drop an Input Action Asset into this
    /// component to correctly configure inputs. You can use the one that ships with the plugin. Find it in the
    /// Packages/ directory in your project view. You cannot use the Input class. Internally, AppMana duplicates the
    /// asset for each player and bind-masks (filters) inputs for the player's corresponding devices to the actions on
    /// this component's input action asset's action maps. In other words, use the <see cref="actions"/> property to
    /// access the action to listen to by name. Unfortunately, you cannot use the generated code file, because Unity
    /// does not configure it correctly for any kind of multiplayer scenario (local multiplayer nor AppMana
    /// multiplayer).</para>
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
        private UnityEvent m_OnPlayerConnected = new();

        [SerializeField, Tooltip("Called when this player disconnects from the experience")]
        private UnityEvent m_OnPlayerDisconnected = new();

        [Header("Advanced")]
        [SerializeField, Tooltip("Set this to limit event system callbacks to objects in this hierarchy")]
        private Transform m_InputsOnlyAffectThisHierarchyOrAny;

        [SerializeField, Tooltip("Contact us for editor streaming support")]
        protected bool m_StreamInEditMode;

        [SerializeField, HideInInspector, Obsolete]
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

        internal static int counter = -1;

        public InputActionAsset actions
        {
            get => m_Actions;
            internal set => m_Actions = value;
        }

        protected sealed override void Awake()
        {
            base.Awake();
            AwakeImpl();
        }

        protected virtual void AwakeImpl()
        {
            m_Index = Interlocked.Increment(ref counter);

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

            foreach (var tmpInputFieldModule in GetComponentsInChildren<InputSystemTMPInputFieldModule>(true))
            {
                tmpInputFieldModule.actionsAsset = m_Actions;
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
            internal set => m_Camera = value;
        }

        public new Camera camera
        {
            get => m_Camera;
            internal set => m_Camera = value;
        }

        public AudioListener audioListener
        {
            get => m_AudioListener;
            internal set => m_AudioListener = value;
        }

        public CanvasScaler[] canvasScalers
        {
            get => m_CanvasScalers;
            internal set => m_CanvasScalers = value;
        }

        public Transform inputsOnlyAffectThisHierarchyOrAny => m_InputsOnlyAffectThisHierarchyOrAny;
        internal bool enableStreamingInEditor => m_StreamInEditMode;

        protected sealed override void Start()
        {
            StartImpl();
        }

        protected virtual void StartImpl()
        {
            if (m_RequestedOnPlayerConnectedInvoke && !m_DidCallInStart)
            {
                m_RequestedOnPlayerConnectedInvoke = false;
                m_DidCallInStart = true;
                m_OnPlayerConnected?.Invoke();
            }
        }

        protected sealed override void OnEnable()
        {
            OnEnableImpl();
        }

        protected virtual void OnEnableImpl()
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

        protected sealed override void OnDisable()
        {
            OnDisableImpl();
        }

        protected virtual void OnDisableImpl()
        {
            if (Application.isEditor && !m_StreamInEditMode)
            {
                m_OnPlayerDisconnected.Invoke();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AppMana.ComponentModel;
using AppMana.UI.TMPro;
using AppManaPublic.ComponentModel;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace AppManaPublic.Configuration
{
    /// <summary>
    /// Configures a streamable player in your game.
    /// </summary>
    /// <para>Connect the player's corresponding <c>Camera</c> (usually the main camera) to the Camera slot. This is the
    /// minimum amount of configuration needed for a working streamed player.</para>
    /// <para>Use <see cref="onPlayerConnected"/> to start your experience. <c>Start</c> will be called potentially long
    /// before the player actually connects. This is an important part of ensuring AppMana streams load instantly.</para>
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
    public class RemotePlayableConfiguration : UIBehaviour, IEvalInPage
    {
        [Header("Setup")] [SerializeField, Tooltip("Set this to the camera to stream for this player")]
        internal Camera m_Camera;

        [SerializeField, Tooltip("Set this to the audio listener for this player, or leave null to disable audio")]
        internal AudioListener m_AudioListener;

        [SerializeField] internal InputActionAsset m_Actions;

        [SerializeField, Tooltip("Set this to canvas scalers for the player's canvas, used to adjust display DPI")]
        internal CanvasScaler[] m_CanvasScalers = new CanvasScaler[0];

        [SerializeField, Tooltip("Called when this player connects to the experience")]
        internal UnityEvent m_OnPlayerConnected = new();

        [SerializeField, Tooltip("Called when this player disconnects from the experience")]
        internal UnityEvent m_OnPlayerDisconnected = new();

        [SerializeField, Tooltip("Contact us for editor streaming support")]
        internal bool m_StreamInEditMode;

        [SerializeField] internal bool m_EnablePlayerPrefs = true;
        [SerializeField] internal bool m_EnableUrlParameters;
        [SerializeField] internal bool m_EnableAugmentedReality;

        [FormerlySerializedAs("m_DefaultUrlParameters")] [SerializeField]
        internal StringTuple[] m_OfflineUrlParameters = new StringTuple[0];

        /// <summary>
        /// These are used for work-in-progress support of augmented reality features.
        /// </summary>
        [SerializeField] internal Vector4 m_RotationCoefficient = new(1, -1, 1, 1);
        [SerializeField] internal Vector3 m_PositionCoefficient = new(1, 1, 1);

        /// <summary>
        /// Set the URL parameters when this project is offline, for example when running in the editor.
        /// </summary>
        /// This can be used by your editor scripts.
        public IReadOnlyDictionary<string, string> offlineUrlParameters
        {
            get => m_OfflineUrlParameters.ToDictionary(tuple => tuple.key, tuple => tuple.value);
            set => m_OfflineUrlParameters = value.Select(kv => new StringTuple(kv.Key, kv.Value)).ToArray();
        }

        /// <summary>
        /// Raised when the player connects for the first time.
        /// </summary>
        public UnityEvent onPlayerConnected => m_OnPlayerConnected;

        /// <summary>
        /// Raised when this player disconnects.
        /// </summary>
        public UnityEvent onPlayerDisconnected => m_OnPlayerDisconnected;

        internal InputUser user { get; private set; }

        private int m_Index;

        internal int index => m_Index;

        internal static int counter = -1;
        private RemotePlayerPrefs m_PlayerPrefs;
        private UrlParameters m_UrlParameters;


        /// <summary>
        /// This player's instance of the input actions asset.
        /// </summary>
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

        internal virtual async UniTask OnPlayerConnected()
        {
            if (m_EnablePlayerPrefs)
            {
                await m_PlayerPrefs.LoadAsync();
            }

            if (m_EnableUrlParameters)
            {
                await m_UrlParameters.Update();
            }

            m_OnPlayerConnected?.Invoke();
        }

        protected virtual void AwakeImpl()
        {
            m_Index = Interlocked.Increment(ref counter);
            if (m_EnablePlayerPrefs)
            {
                m_PlayerPrefs = new RemotePlayerPrefs(this);
            }

            if (m_EnableUrlParameters)
            {
                m_UrlParameters = new UrlParameters(this, m_OfflineUrlParameters);
            }

            if (m_EnableAugmentedReality)
            {
                UniTask.Void(async () =>
                {
                    await UniTask.DelayFrame(1);
                    AppManaHostBase.instance.EnableAugmentedReality();
                });
            }

            var count = UnityUtilities.FindObjectsByType<RemotePlayableConfiguration>(true).Length;

            // if we're in the editor, simulate an on player connected event
            UniTask.Void(async () =>
            {
                var runningStandalone =
                    PluginBase.EnsurePlugins() == 0 && !Application.isBatchMode && !Application.isEditor;
                var runningInEditorAndNotStreaming = Application.isEditor && !m_StreamInEditMode;
                if (runningStandalone || runningInEditorAndNotStreaming)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(Application.isEditor ? 1f : 0f), DelayType.Realtime);
                    await OnPlayerConnected();
                }
            });

            // single player is done
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

            // associate with a user, multiple players
            if (!user.valid)
            {
                user = InputUser.CreateUserWithoutPairedDevices();
            }

            // duplicate the actions
            var originalName = m_Actions.name;
            m_Actions = Instantiate(m_Actions);
            m_Actions.name = $"{originalName} (Player {index})";
            user.AssociateActionsWithUser(m_Actions);
            // use MultiplayerInputActionReference wherever possible

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
            user = InputUser.PerformPairingWithDevice(device, user);
        }

        public new Camera camera
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        internal AudioListener audioListener
        {
            get => m_AudioListener;
            set => m_AudioListener = value;
        }

        /// <summary>
        /// Canvas scalers corresponding to this player.
        /// </summary>
        /// <para>In the next version, these will be determined automatically.</para>
        internal CanvasScaler[] canvasScalers
        {
            get => m_CanvasScalers;
            set => m_CanvasScalers = value;
        }

        internal bool enableStreamingInEditor => m_StreamInEditMode;
        public RemotePlayerPrefs playerPrefs => m_PlayerPrefs;
        public UrlParameters urlParameters => m_UrlParameters;

        protected sealed override void Start()
        {
            StartImpl();
        }

        protected virtual void StartImpl()
        {
        }

        protected sealed override void OnEnable()
        {
            OnEnableImpl();
        }

        protected virtual void OnEnableImpl()
        {
            // if we're running locally, associate the devices we find automatically
            // in the case this is built with the private plugin, the private plugin's system will associate the user
            // with all devices instead
            // because there may be builds with multiple players, eventually this all needs to be handled by
            // StreamedMultiplayer
            if (PluginBase.EnsurePlugins() == 0
                && !Application.isEditor
                && !Application.isBatchMode
                // only associate the devices with the player that's rendering to the primary backbuffer
                // i.e. the user we can actually see
                && camera?.targetDisplay == 0)
            {
                foreach (var device in InputSystem.devices)
                {
                    PerformPairingWithDevice(device);
                }
            }
        }

        protected sealed override void OnDisable()
        {
            OnDisableImpl();
        }

        protected virtual void OnDisableImpl()
        {
            if (Application.isEditor && !m_StreamInEditMode)
            {
                OnPlayerDisconnected().Forget();
            }
        }

        /// <summary>
        /// Evaluates the provided JavaScript code in the context of the user's page.
        /// </summary>
        /// <para>This supports await.</para>
        /// <para>Throws <see cref="PageEvaluationException"/> when a Javascript exception occurs on the remote page.</para>
        /// <param name="javascript">The code to execute. This will be awaited.</param>
        /// <param name="editorStubResponse">When running in editor, return this stub instead.</param>
        /// <param name="editorDelaySeconds">When running in editor, delay the reply by this amount of time.</param>
        /// <typeparam name="T">The expected type of the response. It should be JSON serializable. Use <c>JToken</c> to interpret as a general JSON value.</typeparam>
        /// <returns>The JSON response, deserialized.</returns>
        public async UniTask<T> EvalInPage<T>(
            string javascript,
            Func<T> editorStubResponse = default,
            float editorDelaySeconds = 0.2f)
        {
            if (!AppManaHostBase.instance || Application.isEditor && !m_StreamInEditMode)
            {
                Debug.Log($"Called {nameof(EvalInPage)}, returning stub response");
                if (editorDelaySeconds > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(editorDelaySeconds));
                }

                return editorStubResponse != null ? editorStubResponse() : default;
            }

            return (await AppManaHostBase.instance.EvalInPage(javascript, this, false)).ToObject<T>();
        }

        /// <summary>
        /// Evaluates the provided JavaScript code in the context of the user's page.
        /// </summary>
        /// <param name="javascript"></param>
        /// <param name="editorStub"></param>
        public void EvalInPage(string javascript, Action editorStub = default)
        {
            if (!AppManaHostBase.instance || Application.isEditor && !m_StreamInEditMode)
            {
                Debug.Log($"Called {nameof(EvalInPage)}");
                editorStub?.Invoke();
                return;
            }

            AppManaHostBase.instance.EvalInPage(javascript, this, true).Forget();
        }

#pragma warning disable CS1998
        public async UniTask OnPlayerDisconnected()
#pragma warning restore CS1998
        {
            m_OnPlayerDisconnected?.Invoke();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Clears the player prefs in the editor, thereby also clearing the player prefs in the facade remotePlayerPrefs
        /// </summary>
        [ContextMenu("Clear Player Prefs")]
        public void ClearPlayerPrefs()
        {
            RemotePlayerPrefs.EditorClearPlayerPrefs();
        }
#endif
    }
}
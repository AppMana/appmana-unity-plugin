using System;
using System.Linq;
using System.Reflection;
using AppMana.ComponentModel;
using AppManaPublic.Configuration;
using UniRx;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
using Observable = UniRx.Observable;

namespace AppMana.Multiplayer
{
    /// <summary>
    /// Enables streaming input in an AppMana-hosted Unity game.
    /// </summary>
    /// Use the singleton accessible from <see cref="instance"/> to make multiplayer-specific API calls against the
    /// AppMana backend.
    [DefaultExecutionOrder(-1000)]
    public class StreamedInputs : UIBehaviour, ILobby
    {
        public static StreamedInputs instance { get; private set; }
        private int m_DisplayIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Inject()
        {
            var players = UnityUtilities.FindObjectsByType<RemotePlayableConfiguration>(true);
            if (players.All(player => player.actions == null))
            {
                return;
            }

            if (UnityUtilities.FindFirstObjectByType<StreamedInputs>() != null)
            {
                return;
            }

            var gameObject = new GameObject($"({nameof(StreamedInputs)})");
            gameObject.AddComponent<StreamedInputs>();
        }

        protected override void Awake()
        {
            instance = this;

            var players = FixRaycasters();

#if UNITY_EDITOR
            // find the local mouse
            var localMouse = InputSystem.devices.OfType<Mouse>().FirstOrDefault();
            var localKeyboard = InputSystem.devices.OfType<Keyboard>().FirstOrDefault();

            // when using the simulator, a touchscreen device might be added
            var localTouchscreen = InputSystem.devices.OfType<Touchscreen>().FirstOrDefault();

            var localDevices = new InputDevice[] { localMouse, localKeyboard, localTouchscreen }
                .Where(device => device != null)
                .ToArray();

            // in single player, we can associate the editor controls directly
            if (players.Length == 1)
            {
                var player = players[0];
                if (player.enableStreamingInEditor)
                {
                    foreach (var device in localDevices)
                    {
                        InputSystem.DisableDevice(device);
                    }

                    Observable.OnceApplicationQuit()
                        .Subscribe(_ =>
                        {
                            foreach (var device in localDevices)
                            {
                                InputSystem.EnableDevice(localMouse);
                            }
                        })
                        .AddTo(this);
                }
                else
                {
                    foreach (var device in localDevices)
                    {
                        InputSystem.EnableDevice(device);
                        player.PerformPairingWithDevice(device);
                    }
                }

                return;
            }

            if (localTouchscreen != null)
            {
                Debug.LogWarning(
                    "The simulator view is open when running multiplayer. This creates a simulated touchscreen device and will not issue commands through the multiplayer input simulator. Switch all views from Simulator to Game view when using multiplayer simulation features.");
            }

            // create a fake mouse & keyboard device for each player
            var displayToMouseKeyboard = players
                // players that have streaming enabled in editor will be connected via an offer
                .Where(player => !player.enableStreamingInEditor)
                .Select(player =>
                {
                    var mouse = InputSystem.AddDevice<Mouse>($"EditorMousePlayer{player.user.id}");
                    player.PerformPairingWithDevice(mouse);
                    mouse.AddTo(this);

                    var keyboard = InputSystem.AddDevice<Keyboard>($"EditorKeyboardPlayer{player.user.id}");
                    player.PerformPairingWithDevice(keyboard);
                    keyboard.AddTo(this);
                    return (player, mouse, keyboard);
                })
                .ToDictionary(tuple => tuple.player.camera.targetDisplay, tuple => tuple);

            // unpair pre-existing (editor) devices
            foreach (var player in players)
            {
                var user = player.user;
                if (!user.valid)
                {
                    continue;
                }

                try
                {
                    foreach (var device in localDevices)
                    {
                        user.UnpairDevice(device);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                }
            }

            // get the display under the mouse
            var assembly = typeof(EditorWindow).Assembly;
#if UNITY_2021_1_OR_NEWER
            var targetType = "UnityEditor.PlayModeView";
#else
            var targetType = "UnityEditor.GameView";
#endif
            var type = assembly.GetType(targetType);
            var displayField = type.GetField("m_TargetDisplay", BindingFlags.NonPublic | BindingFlags.Instance);

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    var mouseOverWindow = EditorWindow.mouseOverWindow;

                    var displayId = -1;
                    if (mouseOverWindow != null && type.IsInstanceOfType(mouseOverWindow))
                    {
                        if (displayField == null)
                        {
                            return;
                        }

                        displayId = (int)displayField.GetValue(mouseOverWindow);
                    }

                    if (mouseOverWindow != null && displayId != m_DisplayIndex && displayId != -1)
                    {
                        mouseOverWindow.Focus();
                    }

                    m_DisplayIndex = displayId;
                })
                .AddTo(this);


            // listen to editor mouse and keyboard events, cancel them then repeat them onto the appropriate player
            // based on the display that the mouse or keyboard presented on
            // release buttons
            Observable.Select(InputSystem.onEvent
                        .ForDevice(localMouse),
                    inputEventPtr => (inputEventPtr, m_DisplayIndex))
                .DistinctUntilChanged(tuple => tuple.m_DisplayIndex)
                .Subscribe(tuple =>
                {
                    var (inputEventPtr, _) = tuple;
                    // release the buttons whenever the display index changes
                    unsafe
                    {
                        var stateEventPtr = StateEvent.From(inputEventPtr);
                        var mouseState = (MouseState*)stateEventPtr->state;
                        mouseState->buttons = 0;
                    }
                })
                .AddTo(this);

            InputSystem.onEvent
                .ForDevice(localMouse)
                .Subscribe(inputEventPtr =>
                {
                    if (m_DisplayIndex == -1)
                    {
                        // release the buttons for the other devices
                        return;
                    }

                    if (!displayToMouseKeyboard.ContainsKey(m_DisplayIndex))
                    {
                        return;
                    }

                    var targetDevice = displayToMouseKeyboard[m_DisplayIndex].mouse;

                    unsafe
                    {
                        var stateEventPtr = StateEvent.From(inputEventPtr);
                        var mouseState = stateEventPtr->GetState<MouseState>();
                        mouseState.displayIndex = (ushort)m_DisplayIndex;
                        InputSystem.QueueStateEvent(targetDevice, mouseState, inputEventPtr.time);
                    }

                    inputEventPtr.handled = true;
                })
                .AddTo(this);

            InputSystem.onEvent
                .ForDevice(localKeyboard)
                .Subscribe(inputEventPtr =>
                {
                    if (m_DisplayIndex == -1)
                    {
                        // release the buttons for the other devices
                        return;
                    }

                    if (!displayToMouseKeyboard.ContainsKey(m_DisplayIndex))
                    {
                        return;
                    }

                    var targetDevice = displayToMouseKeyboard[m_DisplayIndex].keyboard;

                    unsafe
                    {
                        if (inputEventPtr.type == KeyboardState.Format)
                        {
                            var stateEventPtr = StateEvent.From(inputEventPtr);
                            var keyboardState = stateEventPtr->GetState<KeyboardState>();
                            InputSystem.QueueStateEvent(targetDevice, keyboardState, inputEventPtr.time);
                        }
                        else if (inputEventPtr.type == TextEvent.Type)
                        {
                            var keysEventPtr = TextEvent.From(inputEventPtr);
                            InputSystem.QueueTextEvent(targetDevice, (char)keysEventPtr->character);
                        }
                    }

                    inputEventPtr.handled = true;
                });
#endif
        }

        internal static RemotePlayableConfiguration[] FixRaycasters()
        {
            var players = UnityUtilities.FindObjectsByType<RemotePlayableConfiguration>(true);

            // find the player inputs
            var playerGameObjects = string.Join(", ", players.Select(player => player.gameObject.name));
            Assert.AreEqual(players.Length,
                players.Select(player => player).Where(playerInput => playerInput != null).Distinct()
                    .Count(),
                $"Assign each {nameof(RemotePlayableConfiguration)} on {playerGameObjects} a distinct {nameof(PlayerInput)}");


            // fix the raycasters
            foreach (var player in players)
            {
                // 3d
                // this must always be on the camera
                var physicsRaycaster = player.camera.GetComponent<PhysicsRaycaster>();
                if (physicsRaycaster != null && physicsRaycaster is not PerUserPhysicsRaycaster)
                {
                    var replacement = physicsRaycaster.gameObject.AddComponent<PerUserPhysicsRaycaster>();
                    replacement.eventMask = physicsRaycaster.eventMask;
                    replacement.remotePlayableConfiguration = player;
                    replacement.maxRayIntersections = physicsRaycaster.maxRayIntersections;
                    Debug.LogFormat(players.Length > 1 ? LogType.Error : LogType.Log, LogOption.None, physicsRaycaster,
                        $"{nameof(PhysicsRaycaster)} on {physicsRaycaster.gameObject.name} was replaced by a {nameof(PerUserPhysicsRaycaster)}. Use the {nameof(PerUserPhysicsRaycaster)} instead. Select the object, switch your Inspector to Debug, then click the Slot Dial next to the Script slot on your {nameof(PhysicsRaycaster)} and select {nameof(PerUserPhysicsRaycaster)}. Then assign the {nameof(RemotePlayableConfiguration)} slot.");
                    Destroy(physicsRaycaster);
                }

                // 2d
                var physics2dRaycaster = player.camera.GetComponent<Physics2DRaycaster>();
                if (physics2dRaycaster != null && physics2dRaycaster is not PerUserPhysics2DRaycaster)
                {
                    var replacement = physics2dRaycaster.gameObject.AddComponent<PerUserPhysics2DRaycaster>();
                    replacement.eventMask = physics2dRaycaster.eventMask;
                    replacement.remotePlayableConfiguration = player;
                    replacement.maxRayIntersections = physics2dRaycaster.maxRayIntersections;
                    Debug.LogFormat(players.Length > 1 ? LogType.Error : LogType.Log, LogOption.None,
                        physics2dRaycaster,
                        $"{nameof(Physics2DRaycaster)} on {physics2dRaycaster.gameObject.name} was replaced by a {nameof(PerUserPhysics2DRaycaster)}. Use the {nameof(PerUserPhysics2DRaycaster)} instead. Select the object, switch your Inspector to Debug, then click the Slot Dial next to the Script slot on your {nameof(Physics2DRaycaster)} and select {nameof(PerUserPhysics2DRaycaster)}. Then assign the {nameof(RemotePlayableConfiguration)} slot.");
                    Destroy(physics2dRaycaster);
                }

                // canvas
                var canvasRaycasters = players.Length == 1
                    ? UnityUtilities.FindObjectsByType<GraphicRaycaster>()
                    : player.camera.GetComponentsInChildren<GraphicRaycaster>(true);
                foreach (var canvasRaycaster in canvasRaycasters)
                {
                    if (canvasRaycaster is PerUserGraphicRaycaster)
                    {
                        continue;
                    }

                    var replacement = canvasRaycaster.gameObject.AddComponent<PerUserGraphicRaycaster>();
                    replacement.blockingMask = canvasRaycaster.blockingMask;
                    replacement.blockingObjects = canvasRaycaster.blockingObjects;
                    replacement.ignoreReversedGraphics = canvasRaycaster.ignoreReversedGraphics;
                    replacement.remotePlayableConfiguration = player;
                    Debug.LogFormat(players.Length > 1 ? LogType.Error : LogType.Log, LogOption.None, canvasRaycaster,
                        $"{nameof(GraphicRaycaster)} on {canvasRaycaster.gameObject.name} was replaced by a {nameof(PerUserGraphicRaycaster)}. Use the {nameof(PerUserGraphicRaycaster)} instead. Select the object, switch your Inspector to Debug, then click the Slot Dial next to the Script slot on your {nameof(GraphicRaycaster)} and select {nameof(PerUserGraphicRaycaster)}. Then assign the {nameof(RemotePlayableConfiguration)} slot.");
                    Destroy(canvasRaycaster);
                }
            }

            return players;
        }

        /// <summary>
        /// Closes the lobby. No more players can join the game.
        /// </summary>
        public void CloseLobby()
        {
            if (!AppManaHostBase.instance)
            {
                Debug.Log($"Called {nameof(CloseLobby)}");
                return;
            }

            AppManaHostBase.instance.CloseLobby();
        }
    }
}
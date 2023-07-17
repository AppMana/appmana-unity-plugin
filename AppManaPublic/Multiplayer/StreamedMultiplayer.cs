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
    /// Enables streaming multiplayer in an AppMana-hosted Unity game.
    /// </summary>
    /// Use the singleton accessible from <see cref="instance"/> to make multiplayer-specific API calls against the
    /// AppMana backend.
    [DefaultExecutionOrder(-1000)]
    public class StreamedMultiplayer : UIBehaviour, ILobby
    {
        public static StreamedMultiplayer instance { get; private set; }
        private int m_DisplayIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Inject()
        {
            var players = UnityUtilities.FindObjectsByType<RemotePlayableConfiguration>(true);
            if (players.All(player => player.actions == null))
            {
                return;
            }

            if (FindAnyObjectByType<StreamedMultiplayer>() != null)
            {
                return;
            }

            var gameObject = new GameObject($"({nameof(StreamedMultiplayer)})");
            gameObject.AddComponent<StreamedMultiplayer>();
        }

        protected override void Awake()
        {
            instance = this;

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
                var physicsRaycaster = player.camera.GetComponent<PhysicsRaycaster>();
                if (physicsRaycaster != null && physicsRaycaster is not PerUserPhysicsRaycaster)
                {
                    var replacement = physicsRaycaster.gameObject.AddComponent<PerUserPhysicsRaycaster>();
                    replacement.eventMask = physicsRaycaster.eventMask;
                    replacement.remotePlayableConfiguration = player;
                    replacement.maxRayIntersections = physicsRaycaster.maxRayIntersections;
                    Debug.LogWarning(
                        $"{nameof(PhysicsRaycaster)} on {physicsRaycaster.gameObject.name} was replaced by a {nameof(PerUserPhysicsRaycaster)}. References to it, while rare, will be broken. Use the {nameof(PerUserPhysicsRaycaster)} instead");
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
                    Debug.LogWarning(
                        $"{nameof(Physics2DRaycaster)} on {physicsRaycaster.gameObject.name} was replaced by a {nameof(PerUserPhysics2DRaycaster)}. References to it, while rare, will be broken. Use the {nameof(PerUserPhysics2DRaycaster)} instead");
                    Destroy(physics2dRaycaster);
                }

                // canvas
                var canvasRaycasters = player.camera.GetComponentsInChildren<GraphicRaycaster>(true);
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
                    Debug.LogWarning(
                        $"{nameof(GraphicRaycaster)} on {canvasRaycaster.gameObject.name} was replaced by a {nameof(PerUserGraphicRaycaster)}. References to it, while rare, will be broken. Use the {nameof(PerUserGraphicRaycaster)} instead");
                    Destroy(canvasRaycaster);
                }
            }

#if UNITY_EDITOR
            // find the local mouse
            var localMouse = InputSystem.devices.OfType<Mouse>().FirstOrDefault();
            var localKeyboard = InputSystem.devices.OfType<Keyboard>().FirstOrDefault();
            // todo: enable touchscreen support

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
                    user.UnpairDevice(localMouse);
                    user.UnpairDevice(localKeyboard);
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
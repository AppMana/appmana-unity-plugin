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
    public class StreamedMultiplayer : UIBehaviour
    {
        private int m_DisplayIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Inject()
        {
            var players = FindObjectsOfType<RemotePlayableConfiguration>(true);
            if (players.Length <= 1)
            {
                return;
            }

            var gameObject = new GameObject($"({nameof(StreamedMultiplayer)})");
            gameObject.AddComponent<StreamedMultiplayer>();
        }

        protected override void Start()
        {
            base.Start();
            var players = FindObjectsOfType<RemotePlayableConfiguration>(true);

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
                if (physicsRaycaster != null)
                {
                    var replacement = physicsRaycaster.gameObject.AddComponent<PerUserPhysicsRaycaster>();
                    replacement.eventMask = physicsRaycaster.eventMask;
                    replacement.remotePlayableConfiguration = player;
                    replacement.maxRayIntersections = physicsRaycaster.maxRayIntersections;
                    Destroy(physicsRaycaster);
                }

                // 2d
                var physics2dRaycaster = player.camera.GetComponent<Physics2DRaycaster>();
                if (physics2dRaycaster != null)
                {
                    var replacement = physics2dRaycaster.gameObject.AddComponent<PerUserPhysics2DRaycaster>();
                    replacement.eventMask = physics2dRaycaster.eventMask;
                    replacement.remotePlayableConfiguration = player;
                    replacement.maxRayIntersections = physics2dRaycaster.maxRayIntersections;
                    Destroy(physics2dRaycaster);
                }

                // canvas
                var canvasRaycasters = player.camera.GetComponentsInChildren<GraphicRaycaster>(true);
                foreach (var canvasRaycaster in canvasRaycasters)
                {
                    var replacement = canvasRaycaster.gameObject.AddComponent<PerUserGraphicRaycaster>();
                    replacement.blockingMask = canvasRaycaster.blockingMask;
                    replacement.blockingObjects = canvasRaycaster.blockingObjects;
                    replacement.ignoreReversedGraphics = canvasRaycaster.ignoreReversedGraphics;
                    replacement.remotePlayableConfiguration = player;
                    Destroy(canvasRaycaster);
                }
            }

#if UNITY_EDITOR
            // find the editor mouse
            var editorMouse = InputSystem.devices.OfType<Mouse>().FirstOrDefault();
            var editorKeyboard = InputSystem.devices.OfType<Keyboard>().FirstOrDefault();
            Assert.IsNotNull(editorMouse, "editorMouse != null");

            // create a fake mouse device for each player
            var displayToMouse = players
                // players that have streaming enabled in editor will be connected via an offer
                .Where(player => !player.enableStreamingInEditor)
                .Select(player =>
                {
                    var mouse = InputSystem.AddDevice<Mouse>($"MouseEditorPlayer{player.user.id}");
                    player.PerformPairingWithDevice(mouse);
                    mouse.AddTo(this);
                    return (player, mouse);
                })
                .ToDictionary(tuple => tuple.player.camera.targetDisplay, tuple => tuple.mouse);

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
                    user.UnpairDevice(editorMouse);
                    user.UnpairDevice(editorKeyboard);
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


            // listen to editor mouse events, cancel them then repeat them onto the appropriate player based on the
            // display that the mouse presented on
            // release buttons
            Observable.Select(InputSystem.onEvent
                        .ForDevice(editorMouse),
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
                .ForDevice(editorMouse)
                .Subscribe(inputEventPtr =>
                {
                    if (m_DisplayIndex == -1)
                    {
                        // release the buttons for the other devices
                        return;
                    }

                    if (!displayToMouse.ContainsKey(m_DisplayIndex))
                    {
                        return;
                    }
                    
                    var targetDevice = displayToMouse[m_DisplayIndex];

                    unsafe
                    {
                        var stateEventPtr = StateEvent.From(inputEventPtr);
                        var mouseState = stateEventPtr->GetState<MouseState>();
                        InputSystem.QueueStateEvent(targetDevice, mouseState, inputEventPtr.time);
                    }

                    inputEventPtr.handled = true;
                })
                .AddTo(this);
#endif
        }
    }
}
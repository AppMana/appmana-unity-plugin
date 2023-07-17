using System;
using AppMana.InteractionToolkit;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace AppMana.Compatibility
{
    /// <summary>
    /// This class provides a familiar Input API in front of Input System that is compatible with AppMana streams.
    /// </summary>
    public static class Input
    {
        private static bool m_WarnedTouches;
        private static bool m_WarnedGetAxis;
        private static bool m_WarnedMousePosition;
        private static InputActions m_ActionAsset;
        public static Vector3 acceleration => Accelerometer.current?.acceleration.ReadValue() ?? Vector3.zero;

        public static bool anyKey => (Keyboard.current?.anyKey.isPressed ?? false) ||
                                     (Pointer.current?.press.isPressed ?? false);

        public static bool anyKeyDown => Keyboard.current?.anyKey.wasPressedThisFrame ?? false;

        public static bool compensateSensors
        {
            get => InputSystem.settings.compensateForScreenOrientation;
            set => InputSystem.settings.compensateForScreenOrientation = value;
        }

        public static Vector2 compositionCursorPos
        {
            set => Keyboard.current?.SetIMECursorPosition(value);
        }

        /// <summary>
        /// Returns the <see cref="Pointer"/>, which is the last nonzero touch or current mouse position.
        /// </summary>
        public static Vector2 mousePosition
        {
            get
            {
                if (!m_WarnedMousePosition)
                {
                    m_WarnedMousePosition = true;
                    Debug.LogWarning($"Think critically how you are using {nameof(mousePosition)}. You are almost" +
                                     $" always better served by using {nameof(EventSystem)} handlers like {nameof(IPointerMoveHandler)}." +
                                     $"{nameof(IPointerMoveHandler.OnPointerMove)} and reading the position from the " +
                                     $"{nameof(PointerEventData)} instance you receive. You can also get detailed information " +
                                     $"about the device by casting the argument to {nameof(ExtendedPointerEventData)}. " +
                                     $"This is the only approach that is compatible with multiplayer.");
                }

                InitializeActionAsset();
                return m_ActionAsset.UI.Point.ReadValue<Vector2>();
            }
        }

        public static bool multiTouchEnabled => true;

        public static bool simulateMouseWithTouches => true;

        public static ReadOnlyArray<Touch> touches
        {
            get
            {
                if (!m_WarnedTouches)
                {
                    m_WarnedTouches = true;
                    Debug.LogWarning($"Think critically how you are using {nameof(touches)}. {nameof(Pointer)} " +
                                     $"already abstracts away {nameof(Mouse)} and {nameof(Touchscreen)} device " +
                                     $"observations like the position of the mouse versus the position of the most " +
                                     $"recent touch. If you need multitouch behavior for the purposes of making " +
                                     $"multiple elements interactive at the same time, use {nameof(EventSystem)} " +
                                     $"callbacks by inheriting from {nameof(UIBehaviour)}, then calling this.{nameof(ObservableTriggerExtensions.OnDragAsObservable)} " +
                                     $"in your Start method. If you genuinely need multiple touches, see https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches.");
                }

                if (!EnhancedTouchSupport.enabled)
                {
                    EnhancedTouchSupport.Enable();
                }

                return Touch.activeTouches;
            }
        }

        public static bool touchPressureSupported => false;

        /// <summary>
        /// Touch is always enabled on AppMana. Design around <see cref="Pointer"/>, not touch or mouse devices.
        /// </summary>
        public static bool touchSupported => true;

        public static float GetAxis(string name)
        {
            InitializeActionAsset();

            var delta = m_ActionAsset.UI.Delta.ReadValue<Vector2>();
            switch (name)
            {
                case "Horizontal":
                case "Mouse X":
                    return delta.x;
                case "Vertical":
                case "Mouse Y":
                    return delta.y;
                case "Fire1":
                    return m_ActionAsset.UI.Click.WasPerformedThisFrame() ? 1f : 0f;
                case "Fire2":
                    return m_ActionAsset.UI.RightClick.WasPerformedThisFrame() ? 1f : 0f;
                case "Submit":
                    return m_ActionAsset.UI.Submit.WasPerformedThisFrame() ? 1f : 0f;
                case "Cancel":
                    return m_ActionAsset.UI.Cancel.WasPerformedThisFrame() ? 1f : 0f;
                case "Jump":
                    return Keyboard.current?.spaceKey.wasPressedThisFrame ?? false ? 1f : 0f;
            }

            return 0f;
        }

        public static bool GetButton(string name)
        {
            return GetAxis(name) >= 1f;
        }

        public static bool GetButtonDown(string name)
        {
            return name switch
            {
                "Fire1" => m_ActionAsset.UI.Click.WasPressedThisFrame(),
                "Fire2" => m_ActionAsset.UI.RightClick.WasPressedThisFrame(),
                "Submit" => m_ActionAsset.UI.Submit.WasPressedThisFrame(),
                "Cancel" => m_ActionAsset.UI.Cancel.WasPressedThisFrame(),
                "Jump" => Keyboard.current?.spaceKey.wasPressedThisFrame ?? false,
                _ => false
            };
        }

        public static bool GetButtonUp(string name)
        {
            return name switch
            {
                "Fire1" => m_ActionAsset.UI.Click.WasReleasedThisFrame(),
                "Fire2" => m_ActionAsset.UI.RightClick.WasReleasedThisFrame(),
                "Submit" => m_ActionAsset.UI.Submit.WasReleasedThisFrame(),
                "Cancel" => m_ActionAsset.UI.Cancel.WasReleasedThisFrame(),
                "Jump" => Keyboard.current?.spaceKey.wasPressedThisFrame ?? false,
                _ => false
            };
        }

        public static bool GetKey(string name)
        {
            return ((KeyControl)Keyboard.current?[name.ToLower()])?.isPressed ?? false;
        }

        public static bool GetKey(Key key)
        {
            return Keyboard.current?[key]?.isPressed ?? false;
        }

        private static Key Convert(KeyCode oldKeyCode)
        {
            var keyCodeName = oldKeyCode.ToString();

            if (Enum.TryParse(keyCodeName, out Key key))
            {
                return key;
            }

            throw new ArgumentException($"Invalid key name {keyCodeName}, use the {nameof(Key)} enum instead");
        }

        public static bool GetKey(KeyCode key)
        {
            return GetKey(Convert(key));
        }

        public static bool GetKeyDown(string name)
        {
            return ((KeyControl)Keyboard.current?[name.ToLower()])?.wasPressedThisFrame ?? false;
        }

        public static bool GetKeyDown(Key key)
        {
            return Keyboard.current?[key]?.wasPressedThisFrame ?? false;
        }

        public static bool GetKeyDown(KeyCode key)
        {
            return GetKeyDown(Convert(key));
        }

        public static bool GetKeyUp(string name)
        {
            return ((KeyControl)Keyboard.current?[name.ToLower()])?.wasReleasedThisFrame ?? false;
        }

        public static bool GetKeyUp(Key key)
        {
            return Keyboard.current?[key]?.wasReleasedThisFrame ?? false;
        }

        public static bool GetKeyUp(KeyCode key)
        {
            return GetKeyUp(Convert(key));
        }

        public static bool GetMouseButton(int index)
        {
            InitializeActionAsset();
            return index switch
            {
                0 => m_ActionAsset.UI.Click.IsPressed(),
                1 => m_ActionAsset.UI.RightClick.IsPressed(),
                2 => m_ActionAsset.UI.MiddleClick.IsPressed(),
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "invalid mouse index")
            };
        }

        public static bool GetMouseButtonDown(int index)
        {
            InitializeActionAsset();
            return index switch
            {
                0 => m_ActionAsset.UI.Click.WasPressedThisFrame(),
                1 => m_ActionAsset.UI.RightClick.WasPressedThisFrame(),
                2 => m_ActionAsset.UI.MiddleClick.WasPressedThisFrame(),
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "invalid mouse index")
            };
        }

        public static bool GetMouseButtonUp(int index)
        {
            InitializeActionAsset();
            return index switch
            {
                0 => m_ActionAsset.UI.Click.WasReleasedThisFrame(),
                1 => m_ActionAsset.UI.RightClick.WasReleasedThisFrame(),
                2 => m_ActionAsset.UI.MiddleClick.WasReleasedThisFrame(),
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "invalid mouse index")
            };
        }

        public static Touch GetTouch(int index) => Touch.activeTouches[index];

        public static void ResetInputAxes()
        {
        }

        private static void InitializeActionAsset()
        {
            if (m_ActionAsset == null)
            {
                m_ActionAsset = new InputActions();
                m_ActionAsset.Enable();
            }
        }
    }
}
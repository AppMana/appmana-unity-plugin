using System;
using System.Collections.Generic;
using System.Linq;
using AppMana.InteractionToolkit;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;

namespace AppMana.ComponentModel
{
    /// <summary>
    /// Helper classes for <c>InputSystem</c> devices.
    /// </summary>
    public static class DeviceExtensions
    {
        private class DisposableDevice : IDisposable
        {
            private InputDevice inputDevice { get; }

            public DisposableDevice(InputDevice inputDevice)
            {
                this.inputDevice = inputDevice;
            }

            public void Dispose()
            {
                InputSystem.RemoveDevice(inputDevice);
            }
        }

        /// <summary>
        /// When the target component is destroyed, the <c>device</c> is removed.
        /// </summary>
        /// <param name="device">input device</param>
        /// <param name="target">a component in your scene</param>
        /// <returns>the device</returns>
        public static InputDevice AddTo(this InputDevice device, Component target)
        {
            var disposableDevice = new DisposableDevice(device);
            disposableDevice.AddTo(target);
            return device;
        }

        /// <summary>
        /// When the target game object is destroyed, the <c>device</c> is removed.
        /// </summary>
        /// <param name="device">input device</param>
        /// <param name="target">game object in your scene</param>
        /// <returns>the device</returns>
        public static InputDevice AddTo(this InputDevice device, GameObject target)
        {
            var disposableDevice = new DisposableDevice(device);
            disposableDevice.AddTo(target);
            return device;
        }

        /// <summary>
        /// When the target disposable collection, the <c>device</c> is removed.
        /// </summary>
        /// <param name="device">input device</param>
        /// <param name="target">a disposable collection</param>
        /// <returns>the device</returns>
        /// <seealso cref="CompositeDisposable"/>
        public static InputDevice AddTo(this InputDevice device, ICollection<IDisposable> target)
        {
            var disposableDevice = new DisposableDevice(device);
            disposableDevice.AddTo(target);
            return device;
        }

        /// <summary>
        /// Observes a stream of text character inputs from the provided keyboard using <see cref="Keyboard.onTextInput"/>
        /// </summary>
        /// <param name="keyboard">the keyboard</param>
        /// <returns>an observable of characters</returns>
        public static IObservable<char> OnTextInputAsObservable(this Keyboard keyboard)
        {
            return Observable.FromEvent<char>(handler => keyboard.onTextInput += handler,
                handler => keyboard.onTextInput -= handler);
        }

        /// <summary>
        /// Observes whenever the action is performed
        /// </summary>
        /// Caveats: Click actions will be performed twice: Once with <c>ctx.ReadValueAsButton()</c> true (i.e. when the
        /// pointer is pressed) and again with <c>ctx.ReadValueAsButton()</c> false (i.e., when the pointer is
        /// released).
        /// <param name="action">the action</param>
        /// <returns>a stream of callback contexts</returns>
        /// <seealso cref="MultiplayerInputActionReference"/>
        public static IObservable<InputAction.CallbackContext> OnPerformedAsObservable(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                handler => action.performed += handler,
                handler => action.performed -= handler);
        }

        /// <summary>
        /// Get the user associated with the input action callback.
        /// </summary>
        /// Useful for retrieving the user from a performed callback.
        /// <param name="callbackContext"></param>
        /// <returns></returns>
        public static InputUser? User(this InputAction.CallbackContext callbackContext)
        {
            return InputUser.FindUserPairedToDevice(callbackContext.control.device);
        }

        /// <summary>
        /// Observes whenever an action is cancelled
        /// </summary>
        /// <param name="action">the action</param>
        /// <returns>a stream of callback contexts</returns>
        public static IObservable<InputAction.CallbackContext> OnCancelledAsObservable(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                handler => action.canceled += handler,
                handler => action.canceled -= handler);
        }

        /// <summary>
        /// Streams the values from the action whenever it is performed
        /// </summary>
        /// <param name="action">the action</param>
        /// <typeparam name="T">the type of its value (corresponds to its associated control)</typeparam>
        /// <returns>a stream of values whenever the action is performed</returns>
        public static IObservable<T> OnValueAsObservable<T>(this InputAction action) where T : struct
        {
            return action.OnPerformedAsObservable()
                .Select(ctx => ctx.ReadValue<T>());
        }

        /// <summary>
        /// Observes an action whose <see cref="InputAction.activeControl"/> is a <see cref="KeyControl"/> and emits
        /// the key pressed
        /// </summary>
        /// <param name="action">the action</param>
        /// <returns>a tuple of key codes and scan codes</returns>
        /// <exception cref="NotImplementedException">throws when the action's active control is not a <see cref="KeyControl"/></exception>
        public static IObservable<(Key keyCode, int scanCode)> OnKeyAsObservable(this InputAction action)
        {
            return action.OnPerformedAsObservable()
                .SelectMany(ctx =>
                {
                    if (ctx.action is not { activeControl: KeyControl activeControl })
                    {
                        return Observable.Throw(
                            new NotImplementedException("cannot observe key for not key control inputs"),
                            (Key.A, 0));
                    }


                    return Observable.Return((activeControl.keyCode, activeControl.scanCode));
                });
        }

        /// <summary>
        /// Creates an input action reference for the action with the specified name
        /// </summary>
        /// <param name="actionsAsset"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static InputActionReference FindReference(this InputActionAsset actionsAsset, string name)
        {
            if (name.StartsWith("m_"))
            {
                name = name.Substring("m_".Length);
            }

            return InputActionReference.Create(actionsAsset.actionMaps
                .SelectMany(map => map.actions)
                .FirstOrDefault(action =>
                    string.Equals(action.name, name, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
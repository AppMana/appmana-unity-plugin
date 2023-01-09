using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace AppMana.ComponentModel
{
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

        public static InputDevice AddTo(this InputDevice device, Component target)
        {
            var disposableDevice = new DisposableDevice(device);
            disposableDevice.AddTo(target);
            return device;
        }

        public static InputDevice AddTo(this InputDevice device, GameObject target)
        {
            var disposableDevice = new DisposableDevice(device);
            disposableDevice.AddTo(target);
            return device;
        }

        public static InputDevice AddTo(this InputDevice device, ICollection<IDisposable> target)
        {
            var disposableDevice = new DisposableDevice(device);
            disposableDevice.AddTo(target);
            return device;
        }

        public static IObservable<char> OnTextInputAsObservable(this Keyboard keyboard)
        {
            return Observable.FromEvent<char>(handler => keyboard.onTextInput += handler,
                handler => keyboard.onTextInput -= handler);
        }

        public static IObservable<InputAction.CallbackContext> OnPerformedAsObservable(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                handler => action.performed += handler,
                handler => action.performed -= handler);
        }

        public static IObservable<InputAction.CallbackContext> OnCancelledAsObservable(this InputAction action)
        {
            return Observable.FromEvent<InputAction.CallbackContext>(
                handler => action.canceled += handler,
                handler => action.canceled -= handler);
        }

        public static IObservable<T> OnValueAsObservable<T>(this InputAction action) where T : struct
        {
            return action.OnPerformedAsObservable()
                .Select(ctx => ctx.ReadValue<T>());
        }

        public static IObservable<(Key keyCode, int scanCode)> OnKeyAsObservable(this InputAction action)
        {
            return action.OnPerformedAsObservable()
                .SelectMany(ctx =>
                {
                    if (ctx.action is not {activeControl: KeyControl activeControl})
                    {
                        return Observable.Throw(
                            new NotImplementedException("cannot observe key for not key control inputs"),
                            (Key.A, 0));
                    }

                    
                    return Observable.Return((activeControl.keyCode, activeControl.scanCode));
                });
        }
    }
}
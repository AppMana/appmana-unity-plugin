using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

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
    }
}
using System;
using AppManaPublic.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AppMana.InteractionToolkit
{
    [Serializable]
    public class MultiplayerInputActionReference
    {
        [SerializeField] private InputActionReference m_InputActionReference;
        [SerializeField] private RemotePlayableConfiguration m_RemotePlayableConfiguration;

        private InputAction m_Action;

        public InputAction action =>
            m_Action ??= m_RemotePlayableConfiguration.actions.FindAction(m_InputActionReference.action.id);

        public static implicit operator InputAction(MultiplayerInputActionReference reference)
        {
            return reference?.action;
        }
    }
}
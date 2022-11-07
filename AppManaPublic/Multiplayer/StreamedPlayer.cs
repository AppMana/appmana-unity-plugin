using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace AppMana.Multiplayer
{
    [DefaultExecutionOrder(-10000)]
    public class StreamedPlayer : UIBehaviour
    {
        private InputUser m_User;
        [SerializeField] private InputActionAsset m_Actions;

        public InputUser user => m_User;

        protected override void Awake()
        {
            base.Awake();
            // clone the actions
            var newActionsName = m_Actions.name;
            m_Actions = Instantiate(m_Actions);
            m_User = InputUser.CreateUserWithoutPairedDevices();
            m_User.AssociateActionsWithUser(m_Actions);
            m_Actions.name = $"{newActionsName} for Player {m_User.id}";
        }

        public void PerformPairingWithDevice(InputDevice device)
        {
            m_User = InputUser.PerformPairingWithDevice(device, m_User);
        }
    }
}
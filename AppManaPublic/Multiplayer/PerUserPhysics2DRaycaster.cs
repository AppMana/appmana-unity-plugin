using System.Collections.Generic;
using AppManaPublic.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;

namespace AppMana.Multiplayer
{
    /// <summary>
    /// Filters raycasts. If the pointer does not belong to the user associated with this raycaster, return no raycast
    /// hits. Simplifies multiple players interacting with EventSystem.
    /// </summary>
    public class PerUserPhysics2DRaycaster : Physics2DRaycaster
    {
        [SerializeField] private RemotePlayableConfiguration m_RemotePlayableConfiguration;

        public RemotePlayableConfiguration remotePlayableConfiguration
        {
            get => m_RemotePlayableConfiguration;
            set => m_RemotePlayableConfiguration = value;
        }

        protected override void Awake()
        {
            m_RemotePlayableConfiguration ??= gameObject.GetComponentInParent<RemotePlayableConfiguration>() ??
                                              gameObject.GetComponentInChildren<RemotePlayableConfiguration>();
            if (m_RemotePlayableConfiguration == null)
            {
                Debug.LogError(
                    $"Assign a {nameof(RemotePlayableConfiguration)} to the {nameof(PerUserPhysics2DRaycaster)} on {gameObject.name}, because none was found.");
            }
            base.Awake();
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is ExtendedPointerEventData { device: not null } extendedPointerEventData)
            {
                var user = InputUser.FindUserPairedToDevice(extendedPointerEventData.device);
                if (m_RemotePlayableConfiguration?.user != user)
                {
                    return;
                }
            }

            base.Raycast(eventData, resultAppendList);
        }
    }
}
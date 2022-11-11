using System.Collections.Generic;
using AppManaPublic.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;

namespace AppMana.Multiplayer
{
    public class PerUserPhysics2DRaycaster : Physics2DRaycaster
    {
        [SerializeField] private RemotePlayableConfiguration m_RemotePlayableConfiguration;

        public RemotePlayableConfiguration remotePlayableConfiguration
        {
            get => m_RemotePlayableConfiguration;
            set => m_RemotePlayableConfiguration = value;
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is ExtendedPointerEventData extendedPointerEventData)
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
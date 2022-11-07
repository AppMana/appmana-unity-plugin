using System.Collections.Generic;
using AppManaPublic.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

namespace AppMana.Multiplayer
{
    public class PerUserGraphicRaycaster : GraphicRaycaster
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
                if (m_RemotePlayableConfiguration?.player?.user != user)
                {
                    return;
                }
            }

            base.Raycast(eventData, resultAppendList);
        }
    }
}
using System.Collections.Generic;
using System.Reflection;
using AppManaPublic.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

namespace AppMana.Multiplayer
{
    /// <summary>
    /// Filters raycasts. If the pointer does not belong to the user associated with this raycaster, return no raycast
    /// hits. Simplifies multiple players interacting with EventSystem.
    /// </summary>
    public class PerUserGraphicRaycaster : GraphicRaycaster
    {
        private static FieldInfo activeEditorGameViewTargetField { get; }

        static PerUserGraphicRaycaster()
        {
            if (Application.isEditor)
            {
                var displayType = typeof(Display);
                activeEditorGameViewTargetField = displayType.GetField("m_ActiveEditorGameViewTarget",
                    BindingFlags.Static | BindingFlags.NonPublic);
            }
            else
            {
                activeEditorGameViewTargetField = null;
            }
        }

        protected override void Awake()
        {
            m_RemotePlayableConfiguration ??= gameObject.GetComponentInParent<RemotePlayableConfiguration>() ??
                                              gameObject.GetComponentInChildren<RemotePlayableConfiguration>();
            if (m_RemotePlayableConfiguration == null)
            {
                Debug.LogError(
                    $"Assign a {nameof(RemotePlayableConfiguration)} to the {nameof(PerUserGraphicRaycaster)} on {gameObject.name}, because none was found.");
            }

            base.Awake();
        }


        [SerializeField] private RemotePlayableConfiguration m_RemotePlayableConfiguration;

        public RemotePlayableConfiguration remotePlayableConfiguration
        {
            get => m_RemotePlayableConfiguration;
            set => m_RemotePlayableConfiguration = value;
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

            if (eventCamera == null)
            {
                Debug.LogWarning(
                    $"Graphic raycaster on {gameObject.name} had a null event camera but a graphic raycaster");
            }
            else
            {
                if (Application.isEditor)
                {
                    activeEditorGameViewTargetField.SetValue(null, eventCamera.targetDisplay);
                }
                else
                {
                    // the camera's target display should always be zero, to pass the graphic raycaster issue here
                    eventCamera.targetDisplay = 0;
                }
            }

            base.Raycast(eventData, resultAppendList);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using AppManaPublic.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;

namespace AppMana.Multiplayer
{
    /// <summary>
    /// Filters raycasts. If the pointer does not belong to the user associated with this raycaster, return no raycast
    /// hits. Simplifies multiple players interacting with EventSystem.
    /// </summary>
    public class PerUserPhysicsRaycaster : PhysicsRaycaster
    {
        [Tooltip("Enable to improve raycast hits with concave mesh colliders"), SerializeField]
        private bool m_EnableMultipleHitsPerConcaveMeshCollider;

        [SerializeField] private RemotePlayableConfiguration m_RemotePlayableConfiguration;

        protected override void Awake()
        {
            base.Awake();

            m_RemotePlayableConfiguration ??= gameObject.GetComponentInParent<RemotePlayableConfiguration>() ??
                                              gameObject.GetComponentInChildren<RemotePlayableConfiguration>();
            if (m_RemotePlayableConfiguration == null)
            {
                Debug.LogError(
                    $"Assign a {nameof(RemotePlayableConfiguration)} to the {nameof(PerUserPhysicsRaycaster)} on {gameObject.name}, because none was found.");
            }

            if (m_EnableMultipleHitsPerConcaveMeshCollider)
            {
                SetDelegate("raycast3DAll", Delegate.CreateDelegate(
                    Type.GetType("UnityEngine.UI.ReflectionMethodsCache+RaycastAllCallback, UnityEngine.UI"),
                    typeof(PerUserPhysicsRaycaster).GetMethod(nameof(RaycastAll),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                SetDelegate("getRaycastNonAlloc", Delegate.CreateDelegate(
                    Type.GetType("UnityEngine.UI.ReflectionMethodsCache+GetRaycastNonAllocCallback, UnityEngine.UI"),
                    typeof(PerUserPhysicsRaycaster).GetMethod(nameof(RaycastNonAlloc),
                        BindingFlags.Static | BindingFlags.NonPublic)));
            }
        }

        private static void SetDelegate(string name, Delegate newMethod)
        {
            var type = Type.GetType("UnityEngine.UI.ReflectionMethodsCache, UnityEngine.UI");
            var singletonProperty = type.GetProperty("Singleton", BindingFlags.Public | BindingFlags.Static);
            var singletonInstance = singletonProperty.GetValue(null, null);

            var raycast3DAllField = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            raycast3DAllField.SetValue(singletonInstance, newMethod);
        }

        private static RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask)
        {
            var hits = new List<RaycastHit>();
            while (Physics.Raycast(ray, out var hit, maxDistance, layerMask) && maxDistance > 0f)
            {
                if (hits.Count > 1)
                {
                    hit.distance += hits[^2].distance;
                }

                hits.Add(hit);
                maxDistance -= hit.distance + Mathf.Epsilon;
                ray = new Ray(hit.point + ray.direction * Mathf.Epsilon, ray.direction);
            }

            return hits.ToArray();
        }

        private static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask)
        {
            var i = 0;
            while (Physics.Raycast(ray, out var hit, maxDistance, layerMask) && maxDistance > 0f && i < results.Length)
            {
                if (i > 1)
                {
                    hit.distance += results[i - 1].distance;
                }

                results[i] = hit;
                maxDistance -= hit.distance + Mathf.Epsilon;
                ray = new Ray(hit.point + ray.direction * Mathf.Epsilon, ray.direction);
                i++;
            }

            return i;
        }

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

            base.Raycast(eventData, resultAppendList);
        }
    }
}
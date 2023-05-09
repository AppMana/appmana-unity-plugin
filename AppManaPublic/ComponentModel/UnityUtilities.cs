using System.Runtime.CompilerServices;
using UnityEngine;

namespace AppMana.ComponentModel
{
    public static class UnityUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindFirstObjectByType<T>() where T : Component
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FindObjectsByType<T>(bool includeInactive = false) where T : Component
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.InstanceID);
#else
            return UnityEngine.Object.FindObjectsOfType<T>(includeInactive);
#endif
        }
    }
}
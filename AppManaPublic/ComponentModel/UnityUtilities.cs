using System.Runtime.CompilerServices;
using UnityEngine;

namespace AppMana.ComponentModel
{
    public static class UnityUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindFirstObjectByType<T>() where T : Component
        {
            return Object.FindAnyObjectByType<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FindObjectsByType<T>(bool includeInactive = false) where T : Component
        {
            return Object.FindObjectsByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.InstanceID);
        }
    }
}
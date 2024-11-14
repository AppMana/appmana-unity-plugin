using System;
using System.Runtime.CompilerServices;
using UniRx;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace AppMana.ComponentModel
{
    /// <summary>
    /// An internal class used to support multiple versions of Unity.
    /// </summary>
    internal static class UnityUtilities
    {
        /// <summary>
        /// A wrapper around the Unity API call for object finding.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindFirstObjectByType<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        /// <summary>
        /// A wrapper around the Unity API call for object finding.
        /// </summary>
        /// <param name="includeInactive"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FindObjectsByType<T>(bool includeInactive = false) where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(
                includeInactive ? UnityEngine.FindObjectsInactive.Include : UnityEngine.FindObjectsInactive.Exclude,
                UnityEngine.FindObjectsSortMode.InstanceID);
#else
            return UnityEngine.Object.FindObjectsOfType<T>(includeInactive);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindAnyObjectByType<T>() where T : UnityEngine.Object
        {
            return FindFirstObjectByType<T>();
        }


        /// <summary>
        /// An event that fires when a new scene is loaded.
        /// </summary>
        /// <returns>A observable stream of scene events.</returns>
        public static IObservable<(Scene, LoadSceneMode)> OnSceneLoadedAsObservable()
        {
            return Observable.FromEvent<UnityAction<Scene, LoadSceneMode>, (Scene, LoadSceneMode)>(
                h => (scene, mode) => h((scene, mode)),
                h => SceneManager.sceneLoaded += h,
                h => SceneManager.sceneLoaded -= h);
        }
    }
}
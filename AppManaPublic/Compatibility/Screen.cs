using AppMana.ComponentModel;
using AppManaPublic.Configuration;
using UnityEngine;

// ReSharper disable Unity.NoNullPropagation

namespace AppMana.Compatibility
{
    /// <summary>
    /// This class provides a familiar API for Screens.
    /// </summary>
    /// <para>AppMana streams do not have screens nor is the size easy to interpret. In DOM pixel units, query the
    /// canvas size instead. With a <c>PhysicalScaler</c>, a canvas will report the correct dimensions of the user's
    /// viewport. This class will report the size of the user's video stream.</para>
    /// <para>Some legacy effects depend on screens. Do not use them if possible.</para>
    public static class Screen
    {
        private static bool m_WasAccessed;
        private static Camera m_Camera;
        private static Vector2Int m_DefaultScreenSize = new Vector2Int(780, 1326);

        static Screen()
        {
            m_WasAccessed = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void ScreenCompatibility()
        {
            if (!m_WasAccessed)
            {
                return;
            }

            var configurations = UnityUtilities.FindObjectsByType<RemotePlayableConfiguration>();
            if (configurations.Length == 0)
            {
                Debug.LogError(
                    $"Add a {nameof(RemotePlayableConfiguration)} for valid usage of {nameof(ScreenCompatibility)}");
                m_Camera = UnityUtilities.FindObjectsByType<Camera>(true)[0];
                return;
            }

            if (configurations.Length > 1)
            {
                Debug.LogError($"{nameof(ScreenCompatibility)} will not give valid results for multiple players.");
            }

            m_Camera = configurations[0].camera;
        }

        public static int width =>
            m_Camera?.targetTexture?.width ?? m_Camera?.pixelWidth ?? (UnityEngine.Screen.width > 0
                ? UnityEngine.Screen.width
                : m_DefaultScreenSize[0]);

        public static int height =>
            m_Camera?.targetTexture?.height ?? m_Camera?.pixelHeight ?? (UnityEngine.Screen.height > 0
                ? UnityEngine.Screen.height
                : m_DefaultScreenSize[1]);
    }
}
using AppMana.ComponentModel;
using AppManaPublic.Configuration;
using UnityEngine;

// ReSharper disable Unity.NoNullPropagation

namespace AppMana.Compatibility
{
    /// <summary>
    /// This class provides a familiar API for Screens.
    /// </summary>
    /// <remarks>
    /// <para>AppMana streams do not have screens nor is the size easy to interpret. In DOM pixel units, query the
    /// canvas size instead. With a <c>PhysicalScaler</c>, a canvas will report the correct dimensions of the user's
    /// viewport. This class will report the size of the user's video stream.</para>
    /// <para>Some legacy effects depend on screens. Do not use them if possible.</para>
    /// </remarks>
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
                var cameras = UnityUtilities.FindObjectsByType<Camera>(true);
                m_Camera = cameras.Length != 0 ? cameras[0] : null;
                return;
            }

            m_Camera = configurations[0].camera;
        }

        internal static Camera camera
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        // The camera can be destroyed by a scene load; Unity's == sees that, while ?. does not.
        public static int width
        {
            get
            {
                if (m_Camera != null)
                {
                    var targetTexture = m_Camera.targetTexture;
                    return targetTexture != null ? targetTexture.width : m_Camera.pixelWidth;
                }

                return UnityEngine.Screen.width > 0 ? UnityEngine.Screen.width : m_DefaultScreenSize[0];
            }
        }

        public static int height
        {
            get
            {
                if (m_Camera != null)
                {
                    var targetTexture = m_Camera.targetTexture;
                    return targetTexture != null ? targetTexture.height : m_Camera.pixelHeight;
                }

                return UnityEngine.Screen.height > 0 ? UnityEngine.Screen.height : m_DefaultScreenSize[1];
            }
        }
    }
}
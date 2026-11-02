using UnityEngine;

namespace AppManaPublic.Configuration
{
    /// <summary>
    /// A utility which retrieves the main camera.
    /// </summary>
    public class Cameras
    {
        public static Camera guessedMainCamera
        {
            get
            {
                // Unity overloads == for destroyed objects; ?? would not see them as missing.
                var main = Camera.main;
                if (main != null)
                {
                    return main;
                }

                var active = Object.FindFirstObjectByType<Camera>();
                if (active != null)
                {
                    return active;
                }

                var inactive = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
                return inactive != null ? inactive : null;
            }
        }

        internal static Camera FirstAlive(params Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                if (camera != null)
                {
                    return camera;
                }
            }

            return null;
        }
    }
}
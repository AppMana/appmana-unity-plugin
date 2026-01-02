using UnityEngine;

namespace AppManaPublic.Configuration
{
    /// <summary>
    /// A utility which retrieves the main camera.
    /// </summary>
    public class Cameras
    {
        public static Camera guessedMainCamera => Camera.main
                                                  ?? Object.FindFirstObjectByType<Camera>()
                                                  ?? Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
    }
}
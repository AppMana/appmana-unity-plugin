using UnityEngine;

namespace AppManaPublic.Configuration
{
    public class Cameras
    {
        public static Camera guessedMainCamera => Camera.main
                                                  ?? Object.FindObjectOfType<Camera>()
                                                  ?? Object.FindObjectOfType<Camera>(true);
    }
}
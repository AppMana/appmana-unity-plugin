using AppMana.ComponentModel;
using AppMana.Multiplayer;
using UnityEngine;

namespace AppManaPublic.Configuration
{
    /// <summary>
    /// Resets static plugin state when entering play mode with domain reloading disabled.
    /// </summary>
    internal static class AppManaPublicStatics
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetForPlayMode()
        {
            RemotePlayableConfiguration.counter = -1;
            StreamedInputs.ResetForPlayMode();
            AppManaHostBase.instance = null;
        }
    }
}

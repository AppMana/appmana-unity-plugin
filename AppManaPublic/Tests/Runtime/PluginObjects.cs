using AppManaPublic.Configuration;
using UnityEngine;

namespace AppManaPublic.Tests.Runtime
{
    /// <summary>
    /// Keeps tests from starting the real plugin. Players call <see cref="PluginBase.EnsurePlugins"/>, which would
    /// instantiate the remote system and stream to a signaling server as soon as a camera exists.
    /// </summary>
    internal static class PluginObjects
    {
        internal sealed class Placeholder : PluginBase
        {
        }

        internal static void DestroyAll()
        {
            foreach (var plugin in Object.FindObjectsByType<PluginBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (plugin is not Placeholder)
                {
                    Object.DestroyImmediate(plugin.gameObject);
                }
            }

            foreach (var player in Object.FindObjectsByType<RemotePlayableConfiguration>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(player);
            }

            if (Object.FindFirstObjectByType<Placeholder>() == null)
            {
                var placeholder = new GameObject("(Test Plugin Placeholder)");
                placeholder.AddComponent<Placeholder>();
                Object.DontDestroyOnLoad(placeholder);
            }
        }
    }
}

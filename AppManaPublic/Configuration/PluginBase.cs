using UnityEngine;
using UnityEngine.EventSystems;

namespace AppManaPublic.Configuration
{
    internal class PluginBase : UIBehaviour
    {
        internal const string pluginResourceName = "(AppMana Plugin Prefab)";
        internal const string pluginResourcePath = "(AppMana Plugin Resources)";

        internal static void EnsurePlugins()
        {
            var existingPlugins = FindObjectsOfType<PluginBase>();
            if (existingPlugins.Length != 0)
            {
                return;
            }

            var pluginPrefabs = Resources.LoadAll<PluginBase>(PluginBase.pluginResourcePath);
            foreach (var pluginPrefab in pluginPrefabs)
            {
                var gameObject = Instantiate(pluginPrefab);
                gameObject.name = gameObject.name.Replace("(Clone)", "");
            }
        }
    }
}
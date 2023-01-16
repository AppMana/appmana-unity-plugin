using UnityEngine.EventSystems;

namespace AppMana.ComponentModel
{
    /// <summary>
    /// Internal class that allows the plugin to discover the host API implementation.
    /// </summary>
    internal abstract class AppManaHostBase : UIBehaviour
    {
        internal abstract void CloseLobby();
    }
}
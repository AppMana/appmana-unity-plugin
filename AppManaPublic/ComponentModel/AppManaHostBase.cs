using AppManaPublic.Configuration;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine.EventSystems;

namespace AppMana.ComponentModel
{
    /// <summary>
    /// Internal class that allows the plugin to discover the host API implementation.
    /// </summary>
    internal abstract class AppManaHostBase : UIBehaviour
    {
        internal static AppManaHostBase instance { get; set; }
        internal abstract void CloseLobby();

        internal abstract UniTask<JToken> EvalInPage(
            string javascript, 
            RemotePlayableConfiguration player = null,
            bool ignoreResult = false);
    }
}
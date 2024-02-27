using System.Linq;
using AppMana.ComponentModel;
using AppManaPublic.Configuration;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// Executes JavaScript in the browser hosting the stream.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Execute"/> to run the code in the browser. Specify the code using the <c>javascript</c>
    /// property.
    /// </remarks>
    public class ExecuteScript : MonoBehaviour
    {
        [SerializeField, Tooltip("The code to execute in your browser"), Multiline(3)]
        private string m_Javascript;

        /// <summary>
        /// The code to execute in your browser
        /// </summary>
        [Preserve]
        public virtual string script => m_Javascript;

        /// <summary>
        /// Immediately run the code in the browser. Does not block.
        /// </summary>
        [Preserve]
        [ContextMenu("Execute")]
        public virtual void Execute()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var playableConfigurations = UnityUtilities.FindObjectsByType<RemotePlayableConfiguration>();
            if (playableConfigurations.Length > 1)
            {
                // multiple players: run the code in the correct context
                var (playableConfiguration, _) = playableConfigurations
                    .SelectMany(playableConfiguration => playableConfiguration.GetComponentsInChildren<ExecuteScript>()
                        .Select(executeScript => (playableConfiguration, executeScript)))
                    .FirstOrDefault(tuple => tuple.executeScript == this);

                playableConfigurations[0] = playableConfiguration;
            }

            if (playableConfigurations[0] != null)
            {
                UniTask.Void(async () =>
                {
                    var result = await playableConfigurations[0].EvalInPage<object>(script);
                    Debug.Log(result);
                });
            }
            else
            {
                Debug.LogError(
                    $"Trying to execute code in a multiplayer game, and this script is not the child of a {nameof(RemotePlayableConfiguration)}");
            }
        }
    }
}
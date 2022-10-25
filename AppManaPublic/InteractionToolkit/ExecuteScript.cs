using System;
using UniRx;
using UnityEngine;
using UnityEngine.Scripting;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// Executes JavaScript in the browser hosting the stream.
    /// </summary>
    /// Call <see cref="Execute"/> to run the code in the browser. Specify the code using the <c>javascript</c>
    /// property.
    public class ExecuteScript : MonoBehaviour
    {
        [SerializeField, Tooltip("The code to execute in your browser"), Multiline(3)]
        private string m_Javascript;

        internal Subject<string> m_Subject = new Subject<string>();

        /// <summary>
        /// The code to execute in your browser
        /// </summary>
        [Preserve]
        public virtual string script => m_Javascript;

        /// <summary>
        /// Immediately run the code in the browser. Does not block.
        /// </summary>
        [Preserve]
        public virtual void Execute()
        {
            m_Subject.OnNext(script);
        }
    }
}
using System;
using UniRx;
using UnityEngine;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// Executes JavaScript in the browser hosting the stream.
    /// </summary>
    /// Call <see cref="Execute"/> to run the code in the browser. Specify the code using the <c>javascript</c>
    /// property.
    public class ExecuteScript : MonoBehaviour
    {
        [SerializeField, Tooltip("The code to execute in your browser")] private string m_Javascript;
        internal Subject<string> m_Subject = new Subject<string>();
        
        /// <summary>
        /// The code to execute in your browser
        /// </summary>
        public string script
        {
            get => m_Javascript;
            set => m_Javascript = value;
        }

        /// <summary>
        /// Immediately run the code in the browser. Does not block.
        /// </summary>
        public void Execute()
        {
            m_Subject.OnNext(m_Javascript);
        }
    }
}
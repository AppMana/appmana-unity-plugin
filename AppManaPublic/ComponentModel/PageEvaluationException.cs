using System;
using AppManaPublic.Configuration;

namespace AppMana.ComponentModel
{
    /// <summary>
    /// Thrown when an exception occurs on the Javascript side of an <see cref="RemotePlayableConfiguration.EvalInPage{T}"/> call.
    /// </summary>
    public class PageEvaluationException : Exception
    {
        /// <summary>
        /// Creates a new instance of this exception.
        /// </summary>
        /// <param name="message"></param>
        internal PageEvaluationException(string message) : base(message)
        {
        }
    }
}
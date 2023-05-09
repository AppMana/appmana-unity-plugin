using System;
using AppMana.ComponentModel;
using Cysharp.Threading.Tasks;

namespace AppManaPublic.Configuration
{
    public interface IEvalInPage
    {
        /// <summary>
        /// Evaluates the provided JavaScript code in the context of the user's page.
        /// </summary>
        /// <para>This supports await.</para>
        /// <para>Throws <see cref="PageEvaluationException"/> when a Javascript exception occurs on the remote page.</para>
        /// <param name="javascript">The code to execute. This will be awaited.</param>
        /// <param name="editorStubResponse">When running in editor, return this stub instead.</param>
        /// <param name="editorDelaySeconds">When running in editor, delay the reply by this amount of time.</param>
        /// <typeparam name="T">The expected type of the response. It should be JSON serializable. Use <c>JToken</c> to interpret as a general JSON value.</typeparam>
        /// <returns>The JSON response, deserialized.</returns>
        UniTask<T> EvalInPage<T>(
            string javascript,
            Func<T> editorStubResponse = default,
            float editorDelaySeconds = 0.2f);

        /// <summary>
        /// Evaluates the provided JavaScript code in the context of the user's page.
        /// </summary>
        /// <param name="javascript"></param>
        /// <param name="editorStub"></param>
        void EvalInPage(string javascript, Action editorStub = default);
    }
}
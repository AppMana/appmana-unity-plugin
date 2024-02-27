using System.Collections.Generic;
using System.Linq;
using AppManaPublic.Configuration;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppManaPublic.ComponentModel
{
    /// <summary>
    /// The URL parameters for the visitor.
    /// </summary>
    /// <remarks>
    /// <para>Set the default parameters when editing offline using the
    /// <see cref="RemotePlayableConfiguration.urlParameters"/> field in the inspector.</para>
    /// </remarks>
    /// <see cref="As{T}"/>
    public class UrlParameters
    {
        private readonly IEvalInPage m_EvalInPage;
        private readonly StringTuple[] m_DefaultUrlParameters;
        private JToken m_CachedUrlParams;

        internal UrlParameters(IEvalInPage evalInPage, StringTuple[] defaultUrlParameters)
        {
            m_EvalInPage = evalInPage;
            m_DefaultUrlParameters = defaultUrlParameters;
        }

        internal async UniTask Update()
        {
            m_CachedUrlParams = await m_EvalInPage.EvalInPage<JToken>(@"
const getQueryParams = () => Object.fromEntries(new URLSearchParams(window.location.search));
return getQueryParams();
", () =>
            {
                return JObject.FromObject(
                    m_DefaultUrlParameters.ToDictionary(tuple => tuple.key, tuple => tuple.value));
            });
        }

        /// <summary>
        /// Read the URL parameters as an object.
        /// </summary>
        /// <remarks>
        /// <para>Use this to read your parameters into a strongly typed object. For example, suppose you want to
        /// configure a timeout and label using URL parameters <c>?timeout=55&label=Hello%20world</c>. First, define a \
        /// class:</para>
        /// <code>
        /// public class MyUrlParameters {
        ///   public string label;
        ///   public float timeout;
        /// }
        /// </code>
        /// <para>Then, use this method:</para>
        /// <example>
        /// <![CDATA[
        /// var remotePlayableConfiguration = GetComponent<RemotePlayableConfiguration>();
        /// remotePlayableConfiguration.onPlayerConnected.AddListener(() => {
        ///   var myParameters = remotePlayableConfiguration.urlParameters.As<MyUrlParameters>();
        ///   Debug.Log(myParameters.label);
        ///   Debug.Log(myParameters.timeout);
        /// });
        /// ]]>
        /// </example>
        /// <para>This will throw a <see cref="JsonException"/> if the parameters cannot be converted.</para>
        /// </remarks>
        /// <typeparam name="T">A strongly typed model for your URL parameters</typeparam>
        /// <returns>The object</returns>
        public T As<T>()
        {
            return m_CachedUrlParams == null ? default : m_CachedUrlParams.ToObject<T>();
        }


        /// <summary>
        /// Reads the URL parameters as a dictionary.
        /// </summary>
        /// <returns>A string-string dictionary.</returns>
        public IReadOnlyDictionary<string, string> AsDictionary()
        {
            return As<IReadOnlyDictionary<string, string>>();
        }
    }
}
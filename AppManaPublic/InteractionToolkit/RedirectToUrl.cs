using UnityEngine;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// When <see cref="ExecuteScript.Execute"/>
    /// </summary>
    public class RedirectToUrl : ExecuteScript
    {
        [SerializeField] private string m_BrowserHref = "";
        [SerializeField] private bool m_AppendCurrentSearchParams = true;
        [SerializeField] private string m_AppendAnchor = "";

        public override string script
        {
            get
            {
                var browserHref = m_BrowserHref ?? "";
                var javascript = $"window.location.href = \"{browserHref}\"";
                if (m_AppendCurrentSearchParams)
                {
                    // does the browser href contain query params? if so, append the pre-existing query params by
                    // correctly replacing the ?blah=1 with &blah=1
                    if (browserHref.Contains("?") && browserHref.Contains("="))
                    {
                        javascript += "+window.location.search.replace(/^\\?/g,\"&\")";
                    }
                    else
                    {
                        // the href that the user specified does not contain a query param
                        javascript += "+window.location.search";
                    }
                }

                // if the user appends an anchor, add it now
                if (!string.IsNullOrEmpty(m_AppendAnchor))
                {
                    var trimmed = m_AppendAnchor.TrimStart('#');
                    javascript += $"+\"#{trimmed}\"";
                }

                return javascript;
            }
        }

        public override void Execute()
        {
            if (Application.isEditor)
            {
                Debug.Log($"[{nameof(AppManaPublic)}] Redirecting to {m_BrowserHref} by executing \"{script}\"...");
            }
            else
            {
                base.Execute();
            }
        }
    }
}
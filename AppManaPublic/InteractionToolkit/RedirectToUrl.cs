using UnityEngine;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// When <see cref="ExecuteScript.Execute"/>
    /// </summary>
    public partial class RedirectToUrl : ExecuteScript
    {
        [SerializeField] protected string m_BrowserHref = "";
        [SerializeField] protected bool m_AppendCurrentSearchParams = true;
        [SerializeField] protected string m_AppendAnchor = "";

        public virtual string browserHref
        {
            get => m_BrowserHref;
            set => m_BrowserHref = value;
        }

        public virtual bool appendCurrentSearchParams
        {
            get => m_AppendCurrentSearchParams;
            set => m_AppendCurrentSearchParams = value;
        }

        public virtual string appendAnchor
        {
            get => m_AppendAnchor;
            set => m_AppendAnchor = value;
        }

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

            base.Execute();
        }
    }
}
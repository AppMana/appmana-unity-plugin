using UnityEngine;
using UnityEngine.EventSystems;

namespace AppMana.InteractionToolkit.Pointers
{
    /// <summary>
    /// Add to a <c>RectTransform</c> to make full-screen gestures when nothing else is in its foreground.
    /// </summary>
    public class Document : UIBehaviour
    {
        protected override void Awake()
        {
            var rectTransform = GetComponent<RectTransform>();
            
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

namespace AppMana.XR
{
    internal class XRScaled : UIBehaviour
    {
        public float scale
        {
            set => transform.localScale = value * Vector3.one;
        }
    }
}
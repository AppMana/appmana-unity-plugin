using UnityEngine;
using UnityEngine.EventSystems;

namespace AppMana.InteractionToolkit
{
    public class ThisDragsRotates3D : UIBehaviour
    {
        [SerializeField] private Transform m_TargetTransform;
        [SerializeField] private Transform m_OptionalPivotPoint;
    }
}
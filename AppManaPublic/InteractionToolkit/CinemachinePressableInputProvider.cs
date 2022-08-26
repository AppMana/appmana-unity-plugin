#if CINEMACHINE
using Cinemachine;
using UnityEngine.EventSystems;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AppMana.InteractionToolkit
{
    public partial class CinemachinePressableInputProvider :
#if CINEMACHINE
        CinemachineInputProvider
#else
        MonoBehaviour
#endif
    {
#pragma warning disable CS0414
        [SerializeField] private float m_ScrollWheelMultiplier = 0.1f;
#pragma warning restore
        [SerializeField,
         Tooltip(
             "Set this to a UI/Click action, and users will need to press a pointer button / touch in order to orbit.")]
        private InputActionReference m_EnableWhenPressed;


#if CINEMACHINE
        private float[] m_Values = new float[3];

        protected virtual void Start()
        {
            var pressed = false;

            if (m_EnableWhenPressed != null)
            {
                m_EnableWhenPressed.action.performed += ctx => { pressed = ctx.ReadValueAsButton(); };
                m_EnableWhenPressed.action.canceled += _ => pressed = false;
            }

            for (var i = 0; i <= 2; i++)
            {
                var axis = i;
                var action = ResolveForPlayer(i, i == 2 ? ZAxis : XYAxis);

                // no action bound
                if (action == null)
                {
                    continue;
                }

                action.performed += ctx =>
                {
                    // mouse buttons and other pointers must be pressed in order to cause
                    // an orbit
                    if (!ActionPredicate(axis, ctx, pressed))
                    {
                        m_Values[axis] = 0f;
                        return;
                    }

                    var value = ctx.ReadValue<Vector2>();

                    m_Values[axis] = axis switch
                    {
                        0 => value.x,
                        1 => value.y,
                        2 => value.y * m_ScrollWheelMultiplier,
                        _ => 0f
                    };
                };

                action.canceled += _ => { m_Values[axis] = 0f; };
            }
        }

        public virtual bool ActionPredicate(int axis, InputAction.CallbackContext ctx, bool pressed)
        {
            if (!enabled)
            {
                return false;
            }
            
            if (m_EnableWhenPressed != null && !pressed && axis < 2)
            {
                return false;
            }

            return true;
        }

        public override float GetAxisValue(int axis)
        {
            return m_Values[axis];
        }
#endif
    }
}
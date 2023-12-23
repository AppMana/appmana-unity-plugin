#if CINEMACHINE
using Cinemachine;
#endif
using AppMana.ComponentModel;
using AppManaPublic.Configuration;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// Implements a Cinemachine input provider for Input System that also takes into consideration when a <see cref="Pointer"/> is pressed.
    /// </summary>
    /// <para>This is used to implement a draggable or orbiting camera.</para>
    public partial class CinemachinePressableInputProvider :
#if CINEMACHINE
        CinemachineInputProvider,
#else
        MonoBehaviour,
#endif
        IHasInputActionReferences
    {
#pragma warning disable CS0414
        [SerializeField] protected float m_ScrollWheelMultiplier = 0.1f;
#pragma warning restore
        [SerializeField,
         Tooltip(
             "Set this to a UI/Click action, and users will need to press a pointer button / touch in order to orbit.")]
        protected InputActionReference m_EnableWhenPressed;

        [SerializeField] private RemotePlayableConfiguration m_RemotePlayableConfiguration;

        public virtual float scrollWheelMultiplier
        {
            get => m_ScrollWheelMultiplier;
            set => m_ScrollWheelMultiplier = value;
        }

        public virtual InputActionReference enableWhenPressed
        {
            get => m_EnableWhenPressed;
            set => m_EnableWhenPressed = value;
        }

#if CINEMACHINE
        public virtual float[] values
        {
            get => m_Values;
            set => m_Values = value;
        }

        public virtual int playerIndex
        {
            set => PlayerIndex = value;
        }

        public virtual bool autoEnableInputs
        {
            set => AutoEnableInputs = value;
        }

        public virtual InputActionReference xyAxis
        {
            set => XYAxis = value;
        }

        public virtual InputActionReference zAxis
        {
            set => ZAxis = value;
        }

        private float[] m_Values = new float[3];

        protected virtual void Start()
        {
            var pressed = false;

            var enableWhenPressedAction = enableWhenPressed.action;
            if (m_RemotePlayableConfiguration)
            {
                enableWhenPressedAction = m_RemotePlayableConfiguration.actions.FindAction(enableWhenPressedAction.id);
            }

            if (enableWhenPressed)
            {
                enableWhenPressedAction.OnPerformedAsObservable()
                    .Subscribe(ctx => { pressed = ctx.ReadValueAsButton(); })
                    .AddTo(this);

                enableWhenPressedAction.OnCancelledAsObservable()
                    .Subscribe(_ => pressed = false)
                    .AddTo(this);
            }

            var axes = new InputAction[] { XYAxis, XYAxis, ZAxis };

            for (var i = 0; i <= 2; i++)
            {
                var axis = i;
                var action = axes[axis];

                // no action bound
                if (action == null)
                {
                    continue;
                }

                action.OnPerformedAsObservable()
                    .Subscribe(ctx =>
                    {
                        // mouse buttons and other pointers must be pressed in order to cause
                        // an orbit
                        if (!ActionPredicate(axis, ctx, pressed))
                        {
                            values[axis] = 0f;
                            return;
                        }

                        var value = ctx.ReadValue<Vector2>();

                        values[axis] = axis switch
                        {
                            0 => value.x,
                            1 => value.y,
                            2 => value.y * scrollWheelMultiplier,
                            _ => 0f
                        };
                    })
                    .AddTo(this);

                action.OnCancelledAsObservable()
                    .Subscribe(_ => { m_Values[axis] = 0f; })
                    .AddTo(this);
            }
        }

        public virtual bool ActionPredicate(int axis, InputAction.CallbackContext ctx, bool pressed)
        {
            if (!enabled)
            {
                return false;
            }

            if (enableWhenPressed != null && !pressed && axis < 2)
            {
                return false;
            }

            return true;
        }

        public override float GetAxisValue(int axis)
        {
            return values[axis];
        }
#endif
        public IHasInputActionReferences.InputActionReferenceProperty[] inputActionReferenceProperties => new[]
        {
            new IHasInputActionReferences.InputActionReferenceProperty(() => m_EnableWhenPressed,
                value => m_EnableWhenPressed = value),
            new(() => XYAxis, value => XYAxis = value),
            new(() => ZAxis, value => ZAxis = value)
        };
    }
}
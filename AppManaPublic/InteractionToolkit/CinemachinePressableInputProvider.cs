#if (CINEMACHINE_3 || APPMANA_CINEMACHINE_3) && !CINEMACHINE_2 && !APPMANA_CINEMACHINE_2
#define APPMANA_USE_CINEMACHINE_3
using Unity.Cinemachine;
#elif CINEMACHINE || CINEMACHINE_2 || APPMANA_CINEMACHINE_2
#define APPMANA_USE_CINEMACHINE_2
using Cinemachine;
#endif
using System.Collections.Generic;
using AppMana.ComponentModel;
using AppManaPublic.Configuration;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// Implements a Cinemachine input provider for Input System that also takes into consideration when a <see cref="Pointer"/> is pressed.
    /// Supports both Cinemachine 2.x and Cinemachine 3.x.
    /// </summary>
    /// <remarks>
    /// <para>This is used to implement a draggable or orbiting camera.</para>
    /// </remarks>
#if APPMANA_USE_CINEMACHINE_3
#pragma warning disable CS0618
#endif
    public partial class CinemachinePressableInputProvider :
#if (APPMANA_USE_CINEMACHINE_2 || (APPMANA_USE_CINEMACHINE_3 && !CINEMACHINE_NO_CM2_SUPPORT)) && ENABLE_INPUT_SYSTEM
        CinemachineInputProvider,
        IHasInputActionReferences
#elif APPMANA_USE_CINEMACHINE_3 && ENABLE_INPUT_SYSTEM
        MonoBehaviour,
        IHasInputActionReferences
#else
        MonoBehaviour
#endif
    {
#pragma warning disable CS0414
        [SerializeField] protected float m_ScrollWheelMultiplier = 0.1f;
        [SerializeField] protected float m_HorizontalGain = 0.5f;
        [SerializeField] protected float m_VerticalGain = -0.375f;
#pragma warning restore CS0414
        [SerializeField,
         Tooltip(
             "Set this to a UI/Click action, and users will need to press a pointer button / touch in order to orbit.")]
        protected InputActionReference m_EnableWhenPressed;

        [SerializeField] private RemotePlayableConfiguration m_RemotePlayableConfiguration;

#if APPMANA_USE_CINEMACHINE_3 && CINEMACHINE_NO_CM2_SUPPORT && ENABLE_INPUT_SYSTEM
        [Tooltip("Leave this at -1 for single-player games. For multi-player games, set this to be the player index, and the actions will be read from that player's controls")]
        public int PlayerIndex = -1;

        [Tooltip("If set, Input Actions will be auto-enabled at start")]
        public bool AutoEnableInputs = true;

        [Tooltip("Vector2 action for XY movement")]
        public InputActionReference XYAxis;

        [Tooltip("Float action for Z movement")]
        public InputActionReference ZAxis;
#endif

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

        public RemotePlayableConfiguration remotePlayableConfiguration
        {
            get => m_RemotePlayableConfiguration;
            set => m_RemotePlayableConfiguration = value;
        }

#if (APPMANA_USE_CINEMACHINE_2 || APPMANA_USE_CINEMACHINE_3) && ENABLE_INPUT_SYSTEM
        public virtual float[] values
        {
            get => m_Values;
            set => m_Values = value;
        }

        public virtual int playerIndex
        {
            get => PlayerIndex;
            set => PlayerIndex = value;
        }

        public virtual bool autoEnableInputs
        {
            get => AutoEnableInputs;
            set => AutoEnableInputs = value;
        }

        public virtual InputActionReference xyAxis
        {
            get => XYAxis;
            set => XYAxis = value;
        }

        public virtual InputActionReference zAxis
        {
            get => ZAxis;
            set => ZAxis = value;
        }

        private float[] m_Values = new float[3];
        private readonly int[] m_LastPerformedFrame = { -1, -1, -1 };
        private readonly InputAction[] m_ResolvedAxes = new InputAction[3];
        private InputAction m_ResolvedEnableWhenPressedAction;
        private CompositeDisposable m_Subscriptions;
        private bool m_Initialized;
        private bool m_Pressed;
        private int m_LastPressFrame = -1;

#if APPMANA_USE_CINEMACHINE_3
        private readonly List<IInputAxisOwner.AxisDescriptor> m_Cm3Axes = new();
        private bool m_HasCm3InputAxisController;
#endif

        protected virtual void Awake()
        {
            EnsureInitialized();
        }

        protected virtual void OnEnable()
        {
            EnsureInitialized();
            RefreshCinemachineTargets();
        }

        protected virtual void Start()
        {
            EnsureInitialized();
            RefreshCinemachineTargets();
        }

#if !APPMANA_USE_CINEMACHINE_3 || !CINEMACHINE_NO_CM2_SUPPORT
        protected override void OnDisable()
        {
            base.OnDisable();
#else
        protected virtual void OnDisable()
        {
#endif
            if (m_Values != null)
            {
                for (var i = 0; i < m_Values.Length; i++)
                {
                    m_Values[i] = 0f;
                    m_LastPerformedFrame[i] = -1;
                }
            }
        }

        protected virtual void OnDestroy()
        {
            m_Subscriptions?.Dispose();
            m_Subscriptions = null;
            m_Initialized = false;
        }

        public void ReinitializeInputActions()
        {
            m_Subscriptions?.Dispose();
            m_Subscriptions = null;
            m_Initialized = false;
            EnsureInitialized();
            RefreshCinemachineTargets();
        }

        protected virtual InputAction ResolveInputAction(InputActionReference actionReference)
        {
            if (actionReference == null || actionReference.action == null)
            {
                return null;
            }

            var action = actionReference.action;
            if (m_RemotePlayableConfiguration == null)
            {
                m_RemotePlayableConfiguration = GetComponentInParent<RemotePlayableConfiguration>();
            }

            if (m_RemotePlayableConfiguration != null && m_RemotePlayableConfiguration.actions != null)
            {
                var mapped = m_RemotePlayableConfiguration.actions.FindAction(action.id) ??
                             m_RemotePlayableConfiguration.actions.FindAction(action.name);
                if (mapped != null)
                {
                    action = mapped;
                }
            }

            if (AutoEnableInputs && !action.enabled)
            {
                action.Enable();
            }

            return action;
        }

        protected virtual void EnsureInitialized()
        {
            if (m_Initialized)
            {
                if (AutoEnableInputs)
                {
                    if (m_ResolvedEnableWhenPressedAction != null && !m_ResolvedEnableWhenPressedAction.enabled)
                    {
                        m_ResolvedEnableWhenPressedAction.Enable();
                    }

                    for (var i = 0; i < m_ResolvedAxes.Length; i++)
                    {
                        if (m_ResolvedAxes[i] != null && !m_ResolvedAxes[i].enabled)
                        {
                            m_ResolvedAxes[i].Enable();
                        }
                    }
                }

                return;
            }

            m_Initialized = true;
            m_Subscriptions?.Dispose();
            m_Subscriptions = new CompositeDisposable();

            m_Pressed = !enableWhenPressed;
            m_ResolvedEnableWhenPressedAction = ResolveInputAction(enableWhenPressed);

            if (m_ResolvedEnableWhenPressedAction != null)
            {
                m_ResolvedEnableWhenPressedAction.OnPerformedAsObservable()
                    .Subscribe(ctx =>
                    {
                        m_Pressed = ctx.ReadValueAsButton();
                        m_LastPressFrame = Time.frameCount;
                        if (!m_Pressed && m_Values != null && m_Values.Length >= 2)
                        {
                            m_Values[0] = 0f;
                            m_Values[1] = 0f;
                        }
                    })
                    .AddTo(m_Subscriptions);

                m_ResolvedEnableWhenPressedAction.OnCancelledAsObservable()
                    .Subscribe(_ =>
                    {
                        m_Pressed = false;
                        m_LastPressFrame = Time.frameCount;
                        if (m_Values != null && m_Values.Length >= 2)
                        {
                            m_Values[0] = 0f;
                            m_Values[1] = 0f;
                        }
                    })
                    .AddTo(m_Subscriptions);
            }

            var xyAction = ResolveInputAction(XYAxis);
            var zAction = ResolveInputAction(ZAxis);
            m_ResolvedAxes[0] = xyAction;
            m_ResolvedAxes[1] = xyAction;
            m_ResolvedAxes[2] = zAction;

            for (var i = 0; i <= 2; i++)
            {
                var axis = i;
                var action = m_ResolvedAxes[axis];

                if (action == null)
                {
                    continue;
                }

                action.OnPerformedAsObservable()
                    .Subscribe(ctx =>
                    {
                        var pressedNow = IsPressed();
                        if (!ActionPredicate(axis, ctx, pressedNow))
                        {
                            m_Values[axis] = 0f;
                            return;
                        }

                        m_LastPerformedFrame[axis] = Time.frameCount;
                        m_Values[axis] = ExtractAxisValueFromContext(axis, ctx);
                    })
                    .AddTo(m_Subscriptions);

                action.OnCancelledAsObservable()
                    .Subscribe(_ =>
                    {
                        m_Values[axis] = 0f;
                        m_LastPerformedFrame[axis] = Time.frameCount;
                    })
                    .AddTo(m_Subscriptions);
            }
        }

        protected virtual float ExtractAxisValueFromContext(int axis, InputAction.CallbackContext ctx)
        {
            if (ctx.valueType == typeof(float))
            {
                var floatVal = ctx.ReadValue<float>();
                return axis == 2 ? floatVal * scrollWheelMultiplier : floatVal;
            }

            var value = ctx.ReadValue<Vector2>();
            return axis switch
            {
                0 => value.x,
                1 => value.y,
                2 => value.y * scrollWheelMultiplier,
                _ => 0f
            };
        }

        protected virtual float ReadLiveAxisValue(int axis, InputAction action)
        {
            if (action == null || !action.enabled)
            {
                return 0f;
            }

            if (action.activeControl != null && action.activeControl.valueType == typeof(float))
            {
                var floatVal = action.ReadValue<float>();
                return axis == 2 ? floatVal * scrollWheelMultiplier : floatVal;
            }

            var value = action.ReadValue<Vector2>();
            return axis switch
            {
                0 => value.x,
                1 => value.y,
                2 => value.y * scrollWheelMultiplier,
                _ => 0f
            };
        }

        public virtual bool IsPressed()
        {
            if (enableWhenPressed == null)
            {
                return true;
            }

            if (m_ResolvedEnableWhenPressedAction != null && m_ResolvedEnableWhenPressedAction.enabled)
            {
                if (m_ResolvedEnableWhenPressedAction.IsPressed())
                {
                    return true;
                }

                if (m_LastPressFrame >= 0 && Time.frameCount > m_LastPressFrame &&
                    m_ResolvedEnableWhenPressedAction.activeControl != null)
                {
                    m_Pressed = false;
                    return false;
                }
            }

            return m_Pressed;
        }

        protected virtual void RefreshCinemachineTargets()
        {
#if !CINEMACHINE_NO_CM2_SUPPORT
            foreach (var pov in GetComponentsInChildren<CinemachinePOV>(true))
            {
                pov.m_HorizontalAxis.SetInputAxisProvider(0, this);
                pov.m_VerticalAxis.SetInputAxisProvider(1, this);
            }

            foreach (var orbital in GetComponentsInChildren<CinemachineOrbitalTransposer>(true))
            {
                orbital.m_XAxis.SetInputAxisProvider(0, this);
            }

            foreach (var freeLook in GetComponentsInChildren<CinemachineFreeLook>(true))
            {
                freeLook.m_XAxis.SetInputAxisProvider(0, this);
                freeLook.m_YAxis.SetInputAxisProvider(1, this);
            }
#endif

#if APPMANA_USE_CINEMACHINE_3
            m_Cm3Axes.Clear();
            var owners = GetComponentsInChildren<IInputAxisOwner>(true);
            foreach (var owner in owners)
            {
                owner.GetInputAxes(m_Cm3Axes);
            }

            if (TryGetComponent<CinemachineInputAxisController>(out var axisController))
            {
                m_HasCm3InputAxisController = true;
                axisController.SynchronizeControllers();
                axisController.ReadControlValueOverride =
                    (InputAction action, IInputAxisOwner.AxisDescriptor.Hints hint, Object context,
                        CinemachineInputAxisController.Reader.ControlValueReader defaultReader) =>
                    {
                        var axisIndex = hint == IInputAxisOwner.AxisDescriptor.Hints.X ? 0 : 1;
                        if (!enabled || (axisIndex < 2 && enableWhenPressed != null && !IsPressed()))
                        {
                            return 0f;
                        }

                        return defaultReader != null
                            ? defaultReader(action, hint, context, null)
                            : GetAxisValue(axisIndex);
                    };
            }
            else
            {
                m_HasCm3InputAxisController = false;
            }
#endif
        }

#if APPMANA_USE_CINEMACHINE_3
        protected virtual void Update()
        {
            if (!enabled || m_HasCm3InputAxisController || m_Cm3Axes.Count == 0)
            {
                return;
            }

            for (var i = 0; i < m_Cm3Axes.Count; i++)
            {
                var desc = m_Cm3Axes[i];
                if (desc.DrivenAxis == null)
                {
                    continue;
                }

                ref var drivenAxis = ref desc.DrivenAxis();
                float delta;
                if (desc.Hint == IInputAxisOwner.AxisDescriptor.Hints.X)
                {
                    delta = GetAxisValue(0) * m_HorizontalGain;
                }
                else if (i >= 2)
                {
                    delta = GetAxisValue(2);
                }
                else
                {
                    delta = GetAxisValue(1) * m_VerticalGain;
                }

                if (!Mathf.Approximately(delta, 0f))
                {
                    drivenAxis.Value = drivenAxis.ClampValue(drivenAxis.Value + delta);
                }
            }
        }
#endif

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

#if !APPMANA_USE_CINEMACHINE_3 || !CINEMACHINE_NO_CM2_SUPPORT
        public override float GetAxisValue(int axis)
#else
        public virtual float GetAxisValue(int axis)
#endif
        {
            if (m_Values == null || axis < 0 || axis >= m_Values.Length)
            {
                return 0f;
            }

            if (!enabled)
            {
                return 0f;
            }

            if (axis < 2 && enableWhenPressed != null && !IsPressed())
            {
                m_Values[axis] = 0f;
                return 0f;
            }

            var resolvedAction = axis < m_ResolvedAxes.Length ? m_ResolvedAxes[axis] : null;
            if (resolvedAction != null && resolvedAction.enabled)
            {
                if (m_LastPerformedFrame[axis] >= 0 && Time.frameCount > m_LastPerformedFrame[axis])
                {
                    var liveValue = ReadLiveAxisValue(axis, resolvedAction);
                    if (Mathf.Approximately(liveValue, 0f))
                    {
                        m_Values[axis] = 0f;
                    }
                    else
                    {
                        m_Values[axis] = liveValue;
                    }
                }
                else if (Mathf.Approximately(m_Values[axis], 0f) && resolvedAction.activeControl != null)
                {
                    var liveValue = ReadLiveAxisValue(axis, resolvedAction);
                    if (!Mathf.Approximately(liveValue, 0f))
                    {
                        m_Values[axis] = liveValue;
                        m_LastPerformedFrame[axis] = Time.frameCount;
                    }
                }
            }

            return m_Values[axis];
        }
#endif
#if ENABLE_INPUT_SYSTEM && (APPMANA_USE_CINEMACHINE_2 || APPMANA_USE_CINEMACHINE_3)
        public IHasInputActionReferences.InputActionReferenceProperty[] inputActionReferenceProperties => new[]
        {
            new IHasInputActionReferences.InputActionReferenceProperty(() => m_EnableWhenPressed,
                value => m_EnableWhenPressed = value),
            new(() => XYAxis, value => XYAxis = value),
            new(() => ZAxis, value => ZAxis = value)
        };
#endif
    }
#if APPMANA_USE_CINEMACHINE_3
#pragma warning restore CS0618
#endif
}
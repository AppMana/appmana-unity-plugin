using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.Scripting;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// Attach to an object that should be able to have its position moved around in space.
    /// </summary>
    public class ThisDraggable3D : UIBehaviour
    {
        [Header("Target Object")] [SerializeField]
        private Positionable m_Target;

        [SerializeField] private RaycastConstraint m_RaycastOnObject = new RaycastConstraint();

        [Header("Movement Constraints"), SerializeField]
        private RaycastConstraint m_RaycastConstraint = RaycastConstraint.xzPlane;

        private BoolReactiveProperty m_IsDragging = new();

        [Preserve] public IReadOnlyReactiveProperty<bool> isDragging => m_IsDragging;

        protected override void Start()
        {
            base.Start();
            if (!EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
            }

            if (!m_Target.valid)
            {
                var rigidbody = GetComponent<Rigidbody>();
                var transform = GetComponent<Transform>();
                if (rigidbody)
                {
                    m_Target.rigidbody = rigidbody;
                }
                else
                {
                    m_Target.transform = transform;
                }
            }

            Assert.IsTrue(m_Target.valid, "Configure the target to set how the object should be moved");
            Assert.IsTrue(m_RaycastConstraint.hasConstraint, "Choose where to constrain the motion to.");

            // drag the object along the surface of the specified constraints

            var pointer = (Vector2Control)null;
            var buttons = (ButtonControl[])null;
            var camera = (Camera)null;
            var positionContext = (Positionable.IPositionContext)null;
            // todo: support something other than new input system
            this.OnBeginDragAsObservable().Subscribe(pointerEventData =>
            {
                camera = pointerEventData.enterEventCamera ?? pointerEventData.pressEventCamera ?? Camera.main;
                if (pointerEventData is ExtendedPointerEventData
                    {
                        control: Vector2Control draggingOnPointer
                    } extendedPointerEventData)
                {
                    pointer = draggingOnPointer;
                    buttons = draggingOnPointer.parent.children.OfType<ButtonControl>().ToArray();
                    positionContext = m_Target.Position(m_RaycastOnObject.canRaycast
                        ? m_RaycastOnObject.GetWorldPositionConstrained(pointer.ReadValue(), camera: camera)
                        : null);
                    positionContext.AddTo(this);
                    m_IsDragging.Value = true;
                    return;
                }

                pointer = null;
                camera = null;
                positionContext?.OnCompleted();
                positionContext?.Dispose();
                m_IsDragging.Value = false;
                positionContext = null;
            }).AddTo(this);

            this.OnDragAsObservable().Subscribe().AddTo(this);
            this.OnEndDragAsObservable().Subscribe().AddTo(this);

            Observable.EveryUpdate()
                .Where(_ => !IsDestroyed() && pointer != null)
                .Subscribe(_ =>
                {
                    if (buttons.All(button => !button.isPressed))
                    {
                        pointer = null;
                        buttons = null;
                        camera = null;
                        positionContext?.OnCompleted();
                        positionContext?.Dispose();
                        positionContext = null;
                        m_IsDragging.Value = false;
                        return;
                    }

                    // project the screen space drag vector onto the manifold of the collider surface
                    var pointerPosition = pointer.ReadValue();
                    var currentPosition = positionContext.position;
                    var desiredPosition =
                        m_RaycastConstraint.GetWorldPositionConstrained(pointerPosition, currentPosition, camera);
                    if (desiredPosition != null)
                    {
                        positionContext.OnNext(desiredPosition.Value);
                    }

                    m_IsDragging.Value = true;
                })
                .AddTo(this);
        }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            m_RaycastConstraint.OnDrawGizmos();
        }
#endif
    }
}
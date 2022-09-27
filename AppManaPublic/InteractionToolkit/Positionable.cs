using System;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace AppMana.InteractionToolkit
{
    [Serializable]
    internal class Positionable
    {
        [Header("Transform Movement")] [SerializeField]
        private Transform m_Transform;

        [Header("Rigidbody Movement")] [SerializeField]
        private Rigidbody m_Rigidbody;

        [SerializeField] private bool m_UseKinematic;
        [SerializeField] private float m_Force = 600;
        [SerializeField] private float m_Damping = 6;

        private IPositionContext m_CurrentContext;

        public Transform transform
        {
            get => m_Transform;
            set => m_Transform = value;
        }

        public Rigidbody rigidbody
        {
            get => m_Rigidbody;
            set => m_Rigidbody = value;
        }

        public bool valid => rigidbody != null || transform != null;

        public IPositionContext Position(Vector3? attachmentPosition = null)
        {
            m_CurrentContext?.OnCompleted();
            Assert.IsTrue(valid,
                "Set a transform or rigidbody on your Positionable");
            if (m_Transform)
            {
                m_CurrentContext = new TransformPositionContext(m_Transform, attachmentPosition);
            }
            else if (m_Rigidbody)
            {
                m_CurrentContext = new RigidbodyPositionContext(m_Rigidbody, m_UseKinematic, m_Damping, m_Force,
                    attachmentPosition);
            }

            return m_CurrentContext;
        }

        public interface IPositionContext : IObserver<Vector3>, IDisposable
        {
            Vector3 position { get; }
        }

        internal sealed class TransformPositionContext : IPositionContext
        {
            public TransformPositionContext(Transform target, Vector3? attachmentPosition = null)
            {
                this.target = target;
                this.attachmentPosition = attachmentPosition;
                if (attachmentPosition != null)
                {
                    m_Offset = target.position - attachmentPosition.Value;
                }
            }

            public Transform target { get; set; }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(Vector3 value)
            {
                target.position = value + m_Offset;
            }

            public Vector3 position => target.position;
            public Vector3? attachmentPosition { get; set; }

            private Vector3 m_Offset;

            public void Dispose()
            {
            }
        }

        internal sealed class RigidbodyPositionContext : IPositionContext
        {
            public Rigidbody rigidbody { get; }
            public bool useKinematic { get; }
            public float force { get; set; } = 600;
            public float damping { get; set; } = 6;
            public Vector3? attachmentPosition { get; set; }

            private Subject<Vector3> m_Positions = new Subject<Vector3>();
            private readonly CompositeDisposable m_Loops = new CompositeDisposable();

            public RigidbodyPositionContext(Rigidbody rigidbody, bool useKinematic, float damping, float force,
                Vector3? attachmentPosition)
            {
                this.rigidbody = rigidbody;
                this.useKinematic = useKinematic;
                this.damping = damping;
                this.force = force;
                this.attachmentPosition = attachmentPosition;

                if (useKinematic)
                {
                    rigidbody.isKinematic = true;
                    // just set the position
                    m_Positions
                        .Finally(() => { rigidbody.isKinematic = false; })
                        .Subscribe(position => { rigidbody.position = position; })
                        .AddTo(m_Loops);
                }
                else
                {
                    // setting the position by creating a joint and moving it
                    var joint = AttachJoint(rigidbody, attachmentPosition ?? rigidbody.position);
                    m_Positions
                        .Finally(() => { Object.Destroy(joint.gameObject); })
                        .Subscribe(position =>
                        {
                            if (joint != null)
                            {
                                joint.position = position;
                            }
                        })
                        .AddTo(m_Loops);
                }
            }

            public void OnCompleted()
            {
                m_Positions.Dispose();
                m_Loops.Dispose();
            }

            public void OnError(Exception error)
            {
                m_Positions.OnError(error);
                m_Loops.Dispose();
            }

            public void OnNext(Vector3 value)
            {
                m_Positions.OnNext(value);
            }

            public Transform AttachJoint(Rigidbody rb, Vector3 attachmentPosition)
            {
                var go = new GameObject("(Attachment Point)");
                // go.hideFlags = HideFlags.HideInHierarchy;
                go.transform.position = attachmentPosition;

                var newRb = go.AddComponent<Rigidbody>();
                newRb.isKinematic = true;

                var joint = go.AddComponent<ConfigurableJoint>();
                joint.connectedBody = rb;
                joint.configuredInWorldSpace = true;
                joint.xDrive = NewJointDrive(force, damping);
                joint.yDrive = NewJointDrive(force, damping);
                joint.zDrive = NewJointDrive(force, damping);
                joint.slerpDrive = NewJointDrive(force, damping);
                joint.rotationDriveMode = RotationDriveMode.Slerp;

                return go.transform;
            }

            public static JointDrive NewJointDrive(float force, float damping)
            {
                var drive = new JointDrive
                {
                    positionSpring = force,
                    positionDamper = damping,
                    maximumForce = Mathf.Infinity
                };
                return drive;
            }

            public Vector3 position => rigidbody.position;

            public void Dispose()
            {
                m_Positions?.Dispose();
                m_Loops?.Dispose();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace AppMana.InteractionToolkit
{
    public class ThisResetsQuickly : UIBehaviour
    {
        [FormerlySerializedAs("m_Targets")] [SerializeField]
        private GameObject[] m_OnCollisionWith = new GameObject[0];

        [SerializeField] private Camera m_WhenBehindCamera;
        [SerializeField] private float m_ResetDistance = 1000f;
        [SerializeField] private float m_Delay;

        protected override void Start()
        {
            base.Start();
            var rigidBody = GetComponent<Rigidbody>();
            var position = transform.position;
            var rotation = transform.rotation;
#if UNITY_6000_0_OR_NEWER
            var initialVelocity = rigidBody?.linearVelocity ?? Vector3.zero;
#else
            var initialVelocity = rigidBody?.velocity ?? Vector3.zero;
#endif
            var set = new HashSet<GameObject>(m_OnCollisionWith.Select(c => c.gameObject));

            var observables = new List<IObservable<Unit>>();
            if (m_OnCollisionWith?.Length > 0)
            {
                observables.Add(this.OnCollisionEnterAsObservable()
                    .Where(collision => set.Contains(collision.gameObject))
                    .AsUnitObservable());
            }

            if (m_WhenBehindCamera)
            {
                observables.Add(Observable.EveryUpdate()
                    .Select(_ =>
                    {
                        var viewportPoint = m_WhenBehindCamera.WorldToViewportPoint(transform.position);
                        return (viewportPoint.x < 0 || viewportPoint.y < 0 || viewportPoint.x > 1 ||
                                viewportPoint.y > 1 || viewportPoint.z < 0 || viewportPoint.z > m_ResetDistance);
                    })
                    .DistinctUntilChanged()
                    .Where(x => x)
                    .AsUnitObservable()
                );
            }

            Observable.Merge(observables)
                .Throttle(TimeSpan.FromSeconds(m_Delay))
                .Subscribe(_ =>
                {
                    transform.position = position;
                    transform.rotation = rotation;
                    if (rigidBody != null)
                    {
                        rigidBody.isKinematic = true;
#if UNITY_6000_0_OR_NEWER
                        rigidBody.linearVelocity = initialVelocity;
#else
                        rigidBody.velocity = initialVelocity;
#endif
                        rigidBody.isKinematic = false;    
                    }
                })
                .AddTo(this);
        }
    }
}
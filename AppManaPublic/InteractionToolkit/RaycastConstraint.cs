using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace AppMana.InteractionToolkit
{
    [Serializable]
    public struct RaycastConstraint : ISerializationCallbackReceiver
    {
        internal const int defaultMaxRaycastHits = 20;
        internal const float defaultMaxDistanceForAllRays = 1000f;
        [SerializeField] private Camera m_Camera;

        // planes
        [Header("Local Plane Option")] [SerializeField]
        private bool m_ConstrainToLocalPlane;

        [Header("World Plane Option")] [SerializeField]
        private bool m_ConstrainToWorldPlane;

        [Header("Camera Plane Option")] [SerializeField]
        private bool m_ConstrainToCameraPlane;

        [Header("Common Plane Settings")] [SerializeField]
        private Vector3 m_PlaneVector;

        [SerializeField] private Vector3 m_PlanePosition;
        [SerializeField] private Transform m_LocalTransform;

        // colliders
        [Header("Colliders Option")] [SerializeField]
        private bool m_ConstrainOnColliderSurfaces;

        [SerializeField] private Collider[] m_Colliders;
        private ISet<Collider> m_CollidersSet;

        [Header("Physics Layer Option")] [SerializeField]
        private bool m_ConstrainToCollidersByLayerMask;

        [SerializeField] private LayerMask m_LayerMask;

        [Header("Common Physics/Collider Settings")] [SerializeField]
        private float m_MaxDistanceForAllRays;

        [Range(0, 255)] [SerializeField] private int m_MaxRaycastHits;
        private RaycastHit[] m_RaycastHits1;
        private RaycastHit[] m_RaycastHits2;

        public Plane plane
        {
            get
            {
                Assert.IsTrue(m_ConstrainToLocalPlane || m_ConstrainToWorldPlane || m_ConstrainToCameraPlane,
                    "Must specify a plane constraint");
                Assert.AreNotEqual(m_PlaneVector, Vector3.zero, "Plane vector must be nonzero");

                if (m_ConstrainToLocalPlane)
                {
                    Assert.IsNotNull(m_LocalTransform, "Must use a local transform when retrieving the plane");

                    return new Plane(m_LocalTransform.TransformVector(m_PlaneVector),
                        m_LocalTransform.TransformPoint(m_PlanePosition));
                }

                if (m_ConstrainToWorldPlane)
                {
                    return new Plane(m_PlaneVector,
                        m_PlanePosition);
                }

                if (m_ConstrainToCameraPlane)
                {
                    return new Plane(m_Camera.transform.TransformVector(m_PlaneVector),
                        m_LocalTransform.TransformPoint(m_PlanePosition));
                }

                throw new UnityException("never reached");
            }
        }

        public bool hasConstraint => m_ConstrainToLocalPlane || m_ConstrainToWorldPlane || m_ConstrainToCameraPlane ||
                                     m_ConstrainOnColliderSurfaces || m_ConstrainToCollidersByLayerMask;

        public bool canRaycast => hasConstraint && m_Camera != null;

        public Vector3? GetWorldPositionConstrained(Vector2 position, Vector3? nearWorldPosition = null)
        {
            if (m_Camera == null)
            {
                m_Camera = Camera.current ?? Camera.main;
            }

            Assert.IsNotNull(m_Camera, "Set a camera on the constraint to retrieve world positions");
            var ray = m_Camera.ScreenPointToRay(position);
            // todo: choose a logical current position
            var currentPosition = nearWorldPosition ?? m_Camera.ScreenToWorldPoint(new Vector3(position.x, position.y,
                (m_Camera.farClipPlane + m_Camera.nearClipPlane) / 2f));

            var planePosition = new Vector3[0];
            if (m_ConstrainToLocalPlane || m_ConstrainToWorldPlane || m_ConstrainToCameraPlane)
            {
                if (plane.Raycast(ray, out var distance))
                {
                    planePosition = new[] {ray.GetPoint(distance)};
                }
            }

            var size1 = 0;
            var size2 = 0;
            if (m_ConstrainToCollidersByLayerMask || m_ConstrainOnColliderSurfaces)
            {
                if (m_ConstrainToCollidersByLayerMask)
                {
                    size1 = Physics.RaycastNonAlloc(ray, m_RaycastHits1, m_MaxDistanceForAllRays, m_LayerMask);
                }

                if (m_ConstrainOnColliderSurfaces)
                {
                    size2 = Physics.RaycastNonAlloc(ray, m_RaycastHits2, m_MaxDistanceForAllRays);
                }
            }

            var finalColliderSet = TryComputeFinalColliderSet();
            // when there are multiple ray hitting points, choose the one closest to the provided position (typically
            // the transform's current position when moving something) as though this is a 2-manifold
            return planePosition.Concat(m_RaycastHits1
                    .Take(size1)
                    .Concat(m_RaycastHits2.Take(size2).Where(hit => finalColliderSet.Contains(hit.collider)))
                    .Select(hit => hit.point))
                .OrderBy(hit => (hit - currentPosition).sqrMagnitude)
                .Select(hit => (Vector3?) hit)
                .FirstOrDefault();
        }

        private ISet<Collider> TryComputeFinalColliderSet()
        {
            if (m_Colliders != null && (m_CollidersSet == null || m_CollidersSet.Count != m_Colliders.Length))
            {
                m_CollidersSet = new HashSet<Collider>(m_Colliders);
            }
            else if (m_Colliders == null)
            {
                m_CollidersSet = new HashSet<Collider>();
            }

            return m_CollidersSet;
        }

        private RaycastConstraint(bool withRaycastHitsAllocated) : this()
        {
            if (withRaycastHitsAllocated)
            {
                m_MaxDistanceForAllRays = defaultMaxDistanceForAllRays;
                m_MaxRaycastHits = defaultMaxRaycastHits;
                TryAllocateRaycastHits();
            }
        }

        private bool TryAllocateRaycastHits()
        {
            Assert.IsTrue(m_MaxRaycastHits >= 0, "Set a non-negative maximum number of raycast hits");
            var allocateHits1 = m_RaycastHits1?.Length != m_MaxRaycastHits;
            if (allocateHits1)
            {
                m_RaycastHits1 = new RaycastHit[m_MaxRaycastHits];
            }

            var allocateHits2 = m_RaycastHits2?.Length != m_MaxRaycastHits;
            if (allocateHits2)
            {
                m_RaycastHits2 = new RaycastHit[m_MaxRaycastHits];
            }

            return allocateHits1 || allocateHits2;
        }

        public static RaycastConstraint xzPlane => new RaycastConstraint()
        {
            m_ConstrainToWorldPlane = true,
            m_PlaneVector = Vector3.up,
        };

        public static RaycastConstraint XZLocalPlane(Transform forTransform) => new RaycastConstraint()
        {
            m_ConstrainToLocalPlane = true,
            m_PlaneVector = Vector3.up
        };

        public void OnBeforeSerialize()
        {
            if (m_ConstrainOnColliderSurfaces || m_ConstrainToCollidersByLayerMask)
            {
                if (m_MaxRaycastHits == 0)
                {
                    m_MaxRaycastHits = defaultMaxRaycastHits;
                }

                if (m_MaxDistanceForAllRays == 0)
                {
                    m_MaxDistanceForAllRays = defaultMaxDistanceForAllRays;
                }
            }
        }

        public void OnAfterDeserialize()
        {
            TryAllocateRaycastHits();
            TryComputeFinalColliderSet();
        }

#if UNITY_EDITOR
        internal void OnDrawGizmos()
        {
            if (m_ConstrainToLocalPlane && m_LocalTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(m_LocalTransform.position,
                    m_LocalTransform.TransformPoint(Vector3.Cross(Vector3.right, m_PlaneVector)));
                Gizmos.color = Color.red;
                Gizmos.DrawLine(m_LocalTransform.position,
                    m_LocalTransform.TransformPoint(Vector3.Cross(Vector3.forward, m_PlaneVector)));
            }

            if (m_ConstrainToWorldPlane)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(m_PlanePosition,
                    m_PlanePosition + Vector3.Cross(Vector3.right, m_PlaneVector));
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(m_PlanePosition,
                    m_PlanePosition + Vector3.Cross(Vector3.forward, m_PlaneVector));
            }
        }
#endif
    }
}
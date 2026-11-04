using AppMana.ComponentModel;
using AppMana.Multiplayer;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// Configures a Cinemachine pressable input provider to be enabled when the component this script is attached to
    /// is dragged.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The purpose of this script is to enable dragging on a large background collider to move a camera, much like
    /// dragging in the background of many apps and games moves the camera.</para>
    /// </remarks>
    public partial class CinemachineEnableWhenDragged : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private CinemachinePressableInputProvider m_InputProvider;

        public CinemachinePressableInputProvider inputProvider
        {
            get => m_InputProvider;
            set => m_InputProvider = value;
        }

        protected override void Start()
        {
            base.Start();

            ConfigureRaycasterAndCamera();

            m_InputProvider = m_InputProvider
                ? m_InputProvider
                : UnityUtilities.FindFirstObjectByType<CinemachinePressableInputProvider>();
            if (m_InputProvider == null)
            {
                return;
            }

#if (CINEMACHINE || CINEMACHINE_2 || APPMANA_CINEMACHINE_2 || CINEMACHINE_3 || APPMANA_CINEMACHINE_3) && ENABLE_INPUT_SYSTEM
            m_InputProvider.ReinitializeInputActions();
#endif
            m_InputProvider.enabled = false;

            this.OnBeginDragAsObservable()
                .Subscribe(_ => SetInputProviderEnabled(true))
                .AddTo(this);
            this.OnDragAsObservable()
                .Subscribe(_ => SetInputProviderEnabled(true))
                .AddTo(this);
            this.OnEndDragAsObservable()
                .Subscribe(_ => SetInputProviderEnabled(false))
                .AddTo(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            SetInputProviderEnabled(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            SetInputProviderEnabled(true);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            SetInputProviderEnabled(false);
        }

        private void SetInputProviderEnabled(bool isEnabled)
        {
            if (m_InputProvider != null)
            {
                m_InputProvider.enabled = isEnabled;
            }
        }

        private void ConfigureRaycasterAndCamera()
        {
            if (TryGetComponent<CanvasRenderer>(out var canvasRenderer))
            {
                canvasRenderer.cullTransparentMesh = false;
                canvasRenderer.cull = false;
            }

            var raycaster = GetComponentInParent<GraphicRaycaster>();
            if (raycaster == null)
            {
                return;
            }

            var cam = raycaster.eventCamera;
            if (cam == null)
            {
                return;
            }

            if (raycaster.blockingObjects is GraphicRaycaster.BlockingObjects.ThreeD or GraphicRaycaster.BlockingObjects.All &&
                (int)raycaster.blockingMask == ~0)
            {
                if (cam.TryGetComponent<PhysicsRaycaster>(out var physicsRaycaster))
                {
                    raycaster.blockingMask = physicsRaycaster.eventMask;
                }
                else
                {
                    raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
                }
            }

            if ((cam.cullingMask & (1 << gameObject.layer)) == 0 && raycaster is not PerUserGraphicRaycaster)
            {
                if (TryGetComponent<Image>(out var image) && image.sprite == null && image.color.a > 0f)
                {
                    var c = image.color;
                    image.color = new Color(c.r, c.g, c.b, 0f);
                }

                cam.cullingMask |= 1 << gameObject.layer;
            }
        }
    }
}
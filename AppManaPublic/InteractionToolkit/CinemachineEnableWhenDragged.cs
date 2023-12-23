using AppMana.ComponentModel;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AppMana.InteractionToolkit
{
    /// <summary>
    /// Configures a Cinemachine pressable input provider to be enabled when the component this script is attached to
    /// is dragged.
    /// </summary>
    /// <para>
    /// The purpose of this script is to enable dragging on a large background collider to move a camera, much like
    /// dragging in the background of many apps and games moves the camera.</para>
    public partial class CinemachineEnableWhenDragged : UIBehaviour
    {
        [SerializeField] private CinemachinePressableInputProvider m_InputProvider;

        protected override void Start()
        {
            base.Start();

            m_InputProvider = m_InputProvider
                ? m_InputProvider
                : UnityUtilities.FindFirstObjectByType<CinemachinePressableInputProvider>();
            if (m_InputProvider == null)
            {
                return;
            }

            m_InputProvider.enabled = false;

            this.OnBeginDragAsObservable()
                .Subscribe(_ => m_InputProvider.enabled = true)
                .AddTo(this);
            this.OnDragAsObservable()
                .Subscribe()
                .AddTo(this);
            this.OnEndDragAsObservable()
                .Subscribe(_ => m_InputProvider.enabled = false)
                .AddTo(this);
        }
    }
}
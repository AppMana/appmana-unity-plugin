using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AppMana.InteractionToolkit
{
    public partial class CinemachineEnableWhenDragged : UIBehaviour
    {
        [SerializeField] private CinemachinePressableInputProvider m_InputProvider;

        protected override void Start()
        {
            base.Start();

            m_InputProvider = m_InputProvider ? m_InputProvider : FindObjectOfType<CinemachinePressableInputProvider>();
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
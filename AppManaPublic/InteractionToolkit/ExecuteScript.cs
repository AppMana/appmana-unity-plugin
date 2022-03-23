using System;
using UniRx;
using UnityEngine;

namespace AppMana.InteractionToolkit
{
    public class ExecuteScript : MonoBehaviour
    {
        [SerializeField] private string m_Javascript;
        internal Subject<string> m_Subject = new Subject<string>();

        public string script
        {
            get => m_Javascript;
            set => m_Javascript = value;
        }

        public void Execute()
        {
            m_Subject.OnNext(m_Javascript);
        }
    }
}
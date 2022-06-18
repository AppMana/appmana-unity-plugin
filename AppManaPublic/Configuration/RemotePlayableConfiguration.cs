﻿using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AppManaPublic.Configuration
{
    internal class RemotePlayableConfiguration : UIBehaviour
    {
        [Tooltip("The camera to stream for this player")] [SerializeField]
        private Camera m_Camera;

        [SerializeField] private AudioListener m_AudioListener;

        [Tooltip(
            "When set, EventSystem inputs corresponding to this player will only affect objects in this hierarchy")]
        [SerializeField]
        private Transform m_InputsOnlyAffectThisHierarchyOrAny;

        public UnityEvent onPlayerConnected => m_OnPlayerConnected;

        public UnityEvent onPlayerDisconnected => m_OnPlayerDisconnected;

        [SerializeField] private UnityEvent m_OnPlayerConnected;
        [SerializeField] private UnityEvent m_OnPlayerDisconnected;

        [SerializeField] private CanvasScaler[] m_CanvasScalers = new CanvasScaler[0];
        [SerializeField] private float m_BaseScale = 1f;

        public float baseScale => m_BaseScale;

        public Camera camera1
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        public AudioListener audioListener
        {
            get => m_AudioListener;
            set => m_AudioListener = value;
        }

        public CanvasScaler[] canvasScalers => m_CanvasScalers;

        public Transform inputsOnlyAffectThisHierarchyOrAny => m_InputsOnlyAffectThisHierarchyOrAny;

        protected override void Start()
        {
            base.Start();

            PluginBase.EnsurePlugins();
        }
    }
}
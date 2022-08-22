using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AppMana.InteractionToolkit
{
    public partial class ToggleLight : UIBehaviour
    {
        [SerializeField] private Light[] m_Lights = Array.Empty<Light>();

        [SerializeField]
        private ToggleLightMaterialTuple[] m_MaterialConfiguration = Array.Empty<ToggleLightMaterialTuple>();

        [SerializeField] private GameObject[] m_OnSet;
        [SerializeField] private GameObject[] m_OffSet;
        protected override void Start()
        {
            base.Start();
            
            this.OnBeginDragAsObservable()
                .Subscribe()
                .AddTo(this);

            this.OnPointerClickAsObservable()
                .Subscribe(pointer =>
                {
                    if (pointer.dragging)
                    {
                        return;
                    }
                    
                    foreach (var light in m_Lights)
                    {
                        var targetOn = !light.enabled;
                        light.enabled = targetOn;
                        foreach (var materialConfig in m_MaterialConfiguration)
                        {
                            foreach (var mesh in materialConfig.lightMeshes)
                            {
                                mesh.material = targetOn ? materialConfig.litMaterial : materialConfig.dimmedMaterial;
                            }
                        }

                        foreach (var obj in targetOn ? m_OnSet : m_OffSet)
                        {
                            obj.SetActive(true);
                        }
                    }
                })
                .AddTo(this);
        }
    }
}
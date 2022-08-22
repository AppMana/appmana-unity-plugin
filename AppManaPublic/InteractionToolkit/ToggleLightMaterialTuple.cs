using System;
using UnityEngine;

namespace AppMana.InteractionToolkit
{
    [Serializable]
    public struct ToggleLightMaterialTuple
    {
        [SerializeField] private MeshRenderer[] m_LightMeshes;
        [SerializeField] private Material m_DimmedMaterial;
        [SerializeField] private Material m_LitMaterial;

        public MeshRenderer[] lightMeshes => m_LightMeshes;

        public Material dimmedMaterial => m_DimmedMaterial;

        public Material litMaterial => m_LitMaterial;
    }
}
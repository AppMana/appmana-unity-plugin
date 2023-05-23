using System;
using UnityEngine;

namespace AppManaPublic.ComponentModel
{
    [Serializable]
    public struct StringTuple
    {
        [SerializeField] private string m_Key;
        [SerializeField] private string m_Value;

        internal StringTuple(string key, string value)
        {
            m_Key = key;
            m_Value = value;
        }

        public string key => m_Key;

        public string value => m_Value;
    }
}
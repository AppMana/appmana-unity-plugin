using System;
using UnityEngine;

namespace AppManaPublic.ComponentModel
{
    /// <summary>
    /// String tuples used to model URL parameters.
    /// </summary>
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

        /// <summary>
        /// The key
        /// </summary>
        public string key => m_Key;

        /// <summary>
        /// The value
        /// </summary>
        public string value => m_Value;
    }
}
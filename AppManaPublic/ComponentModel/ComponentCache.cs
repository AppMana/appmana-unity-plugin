using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AppMana.ComponentModel
{
    /// <summary>
    /// Caches values keyed by component
    /// </summary>
    public class ComponentCache
    {
        private ConditionalWeakTable<Component, object> m_Cache = new();

        /// <summary>
        /// Gets a value keyed by component
        /// </summary>
        /// <param name="key"></param>
        /// <param name="computeIfAbsent"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ComputeIfAbsent<T>(Component key, Func<Component, T> computeIfAbsent)
        {
            if (m_Cache.TryGetValue(key, out var value))
            {
                return (T) value;
            }

            value = computeIfAbsent(key);
            m_Cache.Add(key, value);
            return (T) value;
        }
    }
}
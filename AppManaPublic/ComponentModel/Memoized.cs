using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AppMana.ComponentModel
{
    public static class Memoized
    {
        private static ConditionalWeakTable<Component, object> m_Cache = new ConditionalWeakTable<Component, object>();

        public static T ComputeIfAbsent<T>(Component key, Func<Component, T> computeIfAbsent)
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
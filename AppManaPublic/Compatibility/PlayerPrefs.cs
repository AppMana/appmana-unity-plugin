using AppMana.ComponentModel;
using AppManaPublic.Configuration;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AppMana.Compatibility
{
    /// <summary>
    /// This class provides compatibility with the <see cref="UnityEngine.PlayerPrefs"/> class.
    /// </summary>
    /// <remarks>
    /// <para>This differs from the default <c>PlayerPrefs</c> in two ways: This class saves after every preference
    /// change. Each type (<c>string</c>, <c>int</c> and <c>float</c>) has a distinct key mapping.</para>
    /// </remarks>
    public static class PlayerPrefs
    {
        private static RemotePlayerPrefs m_PlayerPrefs;

        public static void Save()
        {
            defaultRemotePlayerPrefs.Save().Forget();
        }

        public static void DeleteAll()
        {
            defaultRemotePlayerPrefs.DeleteAll();
        }

        public static void DeleteKey(string key)
        {
            defaultRemotePlayerPrefs.DeleteKey(key);
        }

        public static bool HasKey(string key)
        {
            return defaultRemotePlayerPrefs.HasKey(key);
        }

        public static string GetString(string key, string defaultValue = "")
        {
            return defaultRemotePlayerPrefs.GetString(key, defaultValue);
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            return defaultRemotePlayerPrefs.GetInt(key, defaultValue);
        }

        public static float GetFloat(string key, float defaultValue = 0)
        {
            return defaultRemotePlayerPrefs.GetFloat(key, defaultValue);
        }

        public static void SetString(string key, string value)
        {
            defaultRemotePlayerPrefs.SetString(key, value);
        }

        public static void SetInt(string key, int value)
        {
            defaultRemotePlayerPrefs.SetInt(key, value);
        }

        public static void SetFloat(string key, float value)
        {
            defaultRemotePlayerPrefs.SetFloat(key, value);
        }

        public static RemotePlayerPrefs defaultRemotePlayerPrefs
        {
            get
            {
                if (m_PlayerPrefs != null)
                {
                    return m_PlayerPrefs;
                }

                var playerConfigurations = UnityUtilities.FindObjectsByType<RemotePlayableConfiguration>();
                if (playerConfigurations.Length > 1)
                {
                    throw new UnityException($"You should not use {nameof(PlayerPrefs)} in a multiplayer context.");
                }

                if (playerConfigurations.Length == 0)
                {
                    throw new UnityException($"Create a {nameof(RemotePlayableConfiguration)} first.");
                }

                m_PlayerPrefs = playerConfigurations[0].playerPrefs;
                return m_PlayerPrefs;
            }
        }
    }
}
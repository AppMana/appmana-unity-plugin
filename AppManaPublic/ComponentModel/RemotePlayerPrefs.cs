﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AppManaPublic.Configuration;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

namespace AppMana.ComponentModel
{
    /// <summary>
    /// This class wraps PlayerPrefs and stores values in their page's local storage.
    /// </summary>
    public class RemotePlayerPrefs : IDisposable
    {
        private readonly IEvalInPage m_EvalInPage;
        private State m_State = new();

        internal RemotePlayerPrefs(IEvalInPage evalInPage)
        {
            m_EvalInPage = evalInPage;
        }


        protected const string localStorageKey = "appmanaPlayerPrefs";

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        public void Dispose()
        {
        }

        internal void Load()
        {
            LoadAsync().Forget();
        }

        internal async UniTask LoadAsync()
        {
            m_State = await m_EvalInPage.EvalInPage($@"
const data = localStorage.getItem(""{localStorageKey}"");
try {{
    return JSON.parse(data);
}} catch (exc) {{
    return null;
}}
"
                , () =>
                {
                    var index = ((RemotePlayableConfiguration)m_EvalInPage).index;
                    return JsonConvert.DeserializeObject<State>(
                        UnityEngine.PlayerPrefs.GetString($"{localStorageKey}{index}",
                            "null")) ?? new State();
                }) ?? new State();
            m_State.firstLoadComplete = true;
        }

        /// <summary>
        /// Saves the player preferences.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Unity API requires users to explicitly save changes to player preferences.
        /// </para>
        /// </remarks>
        public async UniTask Save()
        {
            var json = JsonConvert.SerializeObject(m_State);
            json = json.Replace("`", "\\`");
            await m_EvalInPage.EvalInPage(
                @$"
const json = `{json}`;
localStorage.setItem(""{localStorageKey}"", json);
return true;
", () =>
                {
                    var index = ((RemotePlayableConfiguration)m_EvalInPage).index;
                    UnityEngine.PlayerPrefs.SetString($"{localStorageKey}{index}", json);
                    UnityEngine.PlayerPrefs.Save();
                    return true;
                });
        }

        internal void Validate()
        {
            Assert.IsTrue(m_State?.firstLoadComplete ?? false, @$"
{nameof(RemotePlayerPrefs)} can only be accessed after {nameof(RemotePlayableConfiguration)}.{nameof(RemotePlayableConfiguration.onPlayerConnected)}
 is called. Add your script's code to the {nameof(RemotePlayableConfiguration.onPlayerConnected)} field in the {nameof(RemotePlayableConfiguration)}'s inspector.");
        }

        public void DeleteAll()
        {
            Validate();

            foreach (var dict in new[] { (IDictionary)m_State.strings, m_State.ints, m_State.floats })
            {
                dict.Clear();
            }
        }

        /// <summary>
        /// Deletes the specified preference key.
        /// </summary>
        /// <param name="key">the key</param>
        public void DeleteKey(string key)
        {
            Validate();

            foreach (var dict in new[] { (IDictionary)m_State.strings, m_State.ints, m_State.floats })
            {
                dict.Remove(key);
            }
        }

        /// <summary>
        /// Checks if a key has been saved into the player's player preferences.
        /// </summary>
        /// <param name="key">the key</param>
        /// <returns><c>true</c> if the key has been set</returns>
        public bool HasKey(string key)
        {
            Validate();

            return new[] { (IDictionary)m_State.strings, m_State.ints, m_State.floats }.Any(dict =>
                dict.Contains(key));
        }

        public string GetString(string key, string defaultValue = "")
        {
            Validate();

            return m_State.strings.TryGetValue(key, out var result) ? result : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            Validate();

            return m_State.ints.TryGetValue(key, out var result) ? result : defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0)
        {
            Validate();

            return m_State.floats.TryGetValue(key, out var result) ? result : defaultValue;
        }

        public void SetString(string key, string value)
        {
            Validate();

            m_State.strings[key] = value;
        }

        public void SetInt(string key, int value)
        {
            Validate();

            m_State.ints[key] = value;
        }

        public void SetFloat(string key, float value)
        {
            Validate();

            m_State.floats[key] = value;
        }

        internal class State
        {
            public bool firstLoadComplete;
            public Dictionary<string, float> floats = new();
            public Dictionary<string, int> ints = new();
            public Dictionary<string, string> strings = new();
        }

#if UNITY_EDITOR
        public static void EditorClearPlayerPrefs()
        {
            for (var i = -1; i < 32; i++)
            {
                var key = $"{localStorageKey}{i}";
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }

            PlayerPrefs.Save();
        }
#endif
    }
}
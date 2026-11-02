#if TMP
using System.Collections;
using System.Text.RegularExpressions;
using AppMana.InteractionToolkit;
using AppMana.UI.TMPro;
using AppManaPublic.Configuration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace AppManaPublic.Tests.Runtime
{
    public class TMPInputFieldModuleTests
    {
        private GameObject m_Player;

        [SetUp]
        public void WithoutPluginObjects()
        {
            PluginObjects.DestroyAll();
        }

        [UnityTearDown]
        public IEnumerator DestroyPlayer()
        {
            if (m_Player != null)
            {
                Object.Destroy(m_Player);
            }

            PluginObjects.DestroyAll();
            yield return null;
        }

        private InputSystemTMPInputFieldModule StartPlayer(InputActionAsset actions)
        {
            m_Player = new GameObject("Player");
            m_Player.SetActive(false);
            var module = m_Player.AddComponent<InputSystemTMPInputFieldModule>();
            var configuration = m_Player.AddComponent<RemotePlayableConfiguration>();
            configuration.m_EnableMultipleSceneMitigations = false;
            configuration.actions = actions;
            m_Player.SetActive(true);
            return module;
        }

        [UnityTest]
        public IEnumerator ActionsWithoutTheTextInputActionsKeepTheModulesOwnAndReportTheMissingMap()
        {
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            actions.name = "InputBindings";
            actions.AddActionMap("Gameplay").AddAction("Flip", binding: "<Keyboard>/leftShift");

            LogAssert.Expect(LogType.Error, new Regex("InputBindings on Player has no \"TextInput\" action map"));
            var module = StartPlayer(actions);
            yield return null;

            Assert.That(module.actionsAsset.FindActionMap("TextInput"), Is.Not.Null, "the module keeps its own text input actions");
            Assert.That(module.actionsAsset.name, Does.Not.StartWith("InputBindings"));
        }

        [UnityTest]
        public IEnumerator ActionsWithTheTextInputActionsDriveTheModuleForThisPlayer()
        {
            var actions = new InputActions().asset;
            actions.name = "Player Actions";

            var module = StartPlayer(actions);
            yield return null;

            Assert.That(module.actionsAsset, Is.Not.EqualTo(actions), "the player uses its own copy of the actions");
            Assert.That(module.actionsAsset.name, Does.StartWith("Player Actions (Player"));
        }
    }
}
#endif

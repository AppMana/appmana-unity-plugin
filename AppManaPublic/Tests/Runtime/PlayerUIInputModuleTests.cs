using System.Collections;
using System.Text.RegularExpressions;
using AppManaPublic.Configuration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace AppManaPublic.Tests.Runtime
{
    public class PlayerUIInputModuleTests
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

        private InputSystemUIInputModule StartPlayer(InputActionAsset actions)
        {
            // The module is enabled before the configuration wakes, the order a scene load may use.
            m_Player = new GameObject("Player");
            m_Player.SetActive(false);
            m_Player.AddComponent<EventSystem>();
            var module = m_Player.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();

            var configuration = m_Player.AddComponent<RemotePlayableConfiguration>();
            configuration.m_EnableMultipleSceneMitigations = false;
            configuration.actions = actions;
            m_Player.SetActive(true);
            return module;
        }

        [UnityTest]
        public IEnumerator ActionsWithoutAUIMapKeepTheModulesPointerActionsAndReportTheMissingMap()
        {
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            actions.name = "InputBindings";
            actions.AddActionMap("Gameplay").AddAction("Flip", binding: "<Keyboard>/leftShift");

            LogAssert.Expect(LogType.Error, new Regex("InputBindings on Player has no \"UI\" action map"));
            var module = StartPlayer(actions);
            yield return null;

            Assert.That(module.actionsAsset.FindActionMap("UI"), Is.Not.Null, "the module keeps its own UI actions");
            Assert.IsTrue(module.point != null && module.point.action != null, "pointer position keeps working");
            Assert.IsTrue(module.leftClick != null && module.leftClick.action != null, "clicks keep working");
        }

        [UnityTest]
        public IEnumerator ActionsWithAUIMapDriveTheModuleForThisPlayer()
        {
            var actions = new DefaultInputActions().asset;
            actions.name = "Player Actions";

            var module = StartPlayer(actions);
            yield return null;

            Assert.That(module.actionsAsset, Is.Not.EqualTo(actions), "the player uses its own copy of the actions");
            Assert.That(module.actionsAsset.name, Does.StartWith("Player Actions (Player"));
            Assert.That(module.point.action.actionMap.asset, Is.EqualTo(module.actionsAsset));
        }

    }
}

using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using AppMana.ComponentModel;
using AppMana.Multiplayer;
using AppManaPublic.Configuration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AppManaPublic.Tests.Runtime
{
    public class RemotePlayableConfigurationTests
    {
        [SetUp]
        public void WithoutPluginObjects()
        {
            PluginObjects.DestroyAll();
        }

        [UnityTearDown]
        public IEnumerator DestroyPlayersAndPlugins()
        {
            foreach (var behaviour in Object.FindObjectsByType<RemotePlayableConfiguration>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.Destroy(behaviour.gameObject);
            }

            PluginObjects.DestroyAll();

            yield return null;
        }

        private static RemotePlayableConfiguration Inactive(string name)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            return root.AddComponent<RemotePlayableConfiguration>();
        }

        [UnityTest]
        public IEnumerator AugmentedRealityWithoutAHostDoesNotThrow()
        {
            var configuration = Inactive("Player");
            configuration.camera = new GameObject("Camera").AddComponent<Camera>();
            configuration.m_EnableAugmentedReality = true;

            configuration.gameObject.SetActive(true);
            yield return null;
            yield return null;

            Assert.That(AppManaHostBase.instance == null);
        }

        [UnityTest]
        public IEnumerator PlayersWithoutCamerasOrActionsDoNotThrow()
        {
            var first = Inactive("Player 1");
            var second = Inactive("Player 2");

            LogAssert.Expect(LogType.Warning, new Regex("Set the Actions field"));
            LogAssert.Expect(LogType.Warning, new Regex("Set the Actions field"));
            first.gameObject.SetActive(true);
            second.gameObject.SetActive(true);
            yield return null;

            Assert.That(first.camera, Is.Null);
        }

        [UnityTest]
        public IEnumerator MultipleSceneSetupWithoutACameraDoesNotThrow([Values(true, false)] bool mitigations)
        {
            var configuration = Inactive("Player");
            configuration.m_EnableMultipleSceneMitigations = mitigations;

            configuration.ConfigureMultipleScenes();
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnmitigatedMultipleScenesReportTheStreamedCameraBeingDestroyed()
        {
            var configuration = Inactive("Player");
            configuration.m_EnableMultipleSceneMitigations = false;
            configuration.camera = new GameObject("Camera").AddComponent<Camera>();
            configuration.ConfigureMultipleScenes();

            LogAssert.Expect(LogType.Error, new Regex("The camera was destroyed while streaming"));
            Object.Destroy(configuration.camera.gameObject);
            yield return null;
        }

        [Test]
        public void LocalDevicesPairOnlyWithTheVisiblePlayerOfAStandalonePlayer()
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            try
            {
                Assert.That(RemotePlayableConfiguration.ShouldPairLocalDevices(0, false, false, camera), Is.True);
                Assert.That(RemotePlayableConfiguration.ShouldPairLocalDevices(1, false, false, camera), Is.False);
                Assert.That(RemotePlayableConfiguration.ShouldPairLocalDevices(0, true, false, camera), Is.False);
                Assert.That(RemotePlayableConfiguration.ShouldPairLocalDevices(0, false, true, camera), Is.False);
                Assert.That(RemotePlayableConfiguration.ShouldPairLocalDevices(0, false, false, null), Is.False);
                camera.targetDisplay = 1;
                Assert.That(RemotePlayableConfiguration.ShouldPairLocalDevices(0, false, false, camera), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator DestroyedCameraNeverPairsLocalDevices()
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            Object.Destroy(camera.gameObject);
            yield return null;

            Assert.That(RemotePlayableConfiguration.ShouldPairLocalDevices(0, false, false, camera), Is.False);
        }

        [Test]
        public void PlayerIndicesRestartWhenPlayModeStartsWithoutADomainReload()
        {
            RemotePlayableConfiguration.counter = 7;
            AppManaPublicStatics.ResetForPlayMode();
            Assert.That(RemotePlayableConfiguration.counter, Is.EqualTo(-1));
            Assert.That(StreamedInputs.instance, Is.Null);
            Assert.That(AppManaHostBase.instance, Is.Null);
        }
    }
}

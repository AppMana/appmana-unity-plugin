using System;
using System.Linq;
using NUnit.Framework;

namespace AppManaPublic.Editor.Tests
{
    public class TextInputCompatibilityTests
    {
        [Test]
        public void TextMeshProIntegrationIsPresentWhenTextMeshProIsInstalled()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (!assemblies.Any(a => a.GetName().Name == "Unity.TextMeshPro"))
                Assert.Ignore("TextMeshPro is optional in projects without text input.");

            var runtime = assemblies.Single(a => a.GetName().Name == "AppManaPublic");
            Assert.That(runtime.GetType("AppMana.UI.TMPro.TMP_InputSystemInputField"), Is.Not.Null,
                "Enable TMP for both standalone TextMeshPro and Unity 6's UGUI 2 package.");
            var editor = assemblies.Single(a => a.GetName().Name == "AppManaPublic.Editor");
            Assert.That(editor.GetTypes().Any(t => t.Name == "TMP_InputSystemInputFieldEditor"), Is.True,
                "The text-input inspector must use the same package detection as the runtime.");
        }
    }
}

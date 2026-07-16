using System.IO;
using NUnit.Framework;

namespace AppManaPublic.Editor.Tests
{
    public class WebGLTests
    {
        [Test]
        public void AcceptsExplicitOutputBasenameThatDiffersFromProductName()
        {
            var buildDirectory = CreateBuildDirectory();
            try
            {
                File.WriteAllText(Path.Combine(buildDirectory, "Dinky.data.br"), "data");
                File.WriteAllText(Path.Combine(buildDirectory, "Dinky.framework.js.br"), "framework");
                File.WriteAllText(Path.Combine(buildDirectory, "Dinky.wasm.br"), "wasm");

                Assert.That(WebGL.FindInvalidArtifacts(buildDirectory), Is.Empty);
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(buildDirectory), true);
            }
        }

        [Test]
        public void RejectsMissingReleaseArtifact()
        {
            var buildDirectory = CreateBuildDirectory();
            try
            {
                File.WriteAllText(Path.Combine(buildDirectory, "Dinky.data.br"), "data");
                File.WriteAllText(Path.Combine(buildDirectory, "Dinky.wasm.br"), "wasm");

                Assert.That(WebGL.FindInvalidArtifacts(buildDirectory), Is.EqualTo(new[] { "framework.js.br" }));
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(buildDirectory), true);
            }
        }

        private static string CreateBuildDirectory()
        {
            var root = Path.Combine(Path.GetTempPath(), $"appmana-webgl-{System.Guid.NewGuid():N}");
            var buildDirectory = Path.Combine(root, "Build");
            Directory.CreateDirectory(buildDirectory);
            return buildDirectory;
        }
    }
}

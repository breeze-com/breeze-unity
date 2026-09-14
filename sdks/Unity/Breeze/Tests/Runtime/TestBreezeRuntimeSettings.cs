using NUnit.Framework;

namespace BreezeSdk.Runtime.Tests
{
    /// <summary>
    /// Tests that pin the editor-tooling path constants in BreezeRuntimeSettings.
    /// These values are used by the Breeze Setup editor window to locate and create
    /// the settings asset; an accidental rename would break the initialization flow
    /// without any compiler error.
    /// </summary>
    public class TestBreezeRuntimeSettings
    {
        // ─── Editor path constants ──────────────────────────────────────────

        [Test]
        public void AssetDir_IsExpectedPath()
        {
            Assert.AreEqual("Assets/Breeze/Resources", BreezeRuntimeSettings.AssetDir);
        }

        [Test]
        public void AssetPath_IsExpectedPath()
        {
            Assert.AreEqual(
                "Assets/Breeze/Resources/BreezeRuntimeSettings.asset",
                BreezeRuntimeSettings.AssetPath);
        }

        [Test]
        public void AssetPath_StartsWithAssetDir()
        {
            // AssetPath must be under AssetDir so Resources.Load can find the asset.
            Assert.IsTrue(BreezeRuntimeSettings.AssetPath.StartsWith(BreezeRuntimeSettings.AssetDir));
        }
    }
}

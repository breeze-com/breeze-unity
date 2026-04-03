using System;
using NUnit.Framework;

namespace BreezeSdk.Runtime.Tests
{
    /// <summary>
    /// Tests for Breeze singleton lifecycle (Initialize/Uninitialize).
    /// Note: These tests require Unity runtime (GameObject, etc.).
    /// They should run in Unity Test Framework's Play Mode.
    /// </summary>
    public class TestBreezeSingleton
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure clean state before each test
            Breeze.Uninitialize();
        }

        [TearDown]
        public void TearDown()
        {
            Breeze.Uninitialize();
        }

        // ─── Initialize ─────────────────────────────────────────────────────

        [Test]
        public void Initialize_WithValidConfig_CreatesInstance()
        {
            var config = new BreezeConfiguration { AppScheme = "testgame" };
            Breeze.Initialize(config);
            Assert.IsNotNull(Breeze.Instance);
        }

        [Test]
        public void Initialize_SetsInstanceOnce()
        {
            var config = new BreezeConfiguration { AppScheme = "testgame" };
            Breeze.Initialize(config);
            var first = Breeze.Instance;
            Breeze.Initialize(config); // second call should be no-op
            Assert.AreSame(first, Breeze.Instance);
        }

        [Test]
        public void Initialize_NullConfig_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Breeze.Initialize(null));
        }

        [Test]
        public void Initialize_NullAppScheme_ThrowsArgumentException()
        {
            var config = new BreezeConfiguration { AppScheme = null };
            Assert.Throws<ArgumentException>(() => Breeze.Initialize(config));
        }

        [Test]
        public void Initialize_EmptyAppScheme_ThrowsArgumentException()
        {
            var config = new BreezeConfiguration { AppScheme = "" };
            Assert.Throws<ArgumentException>(() => Breeze.Initialize(config));
        }

        [Test]
        public void Initialize_WithEnvironmentDevelopment_Succeeds()
        {
            var config = new BreezeConfiguration
            {
                AppScheme = "testgame",
                Environment = BreezeEnvironment.Development
            };
            Breeze.Initialize(config);
            Assert.IsNotNull(Breeze.Instance);
        }

        [Test]
        public void Initialize_WithEnvironmentProduction_Succeeds()
        {
            var config = new BreezeConfiguration
            {
                AppScheme = "testgame",
                Environment = BreezeEnvironment.Production
            };
            Breeze.Initialize(config);
            Assert.IsNotNull(Breeze.Instance);
        }

        // ─── Uninitialize ───────────────────────────────────────────────────

        [Test]
        public void Uninitialize_ClearsInstance()
        {
            var config = new BreezeConfiguration { AppScheme = "testgame" };
            Breeze.Initialize(config);
            Assert.IsNotNull(Breeze.Instance);
            Breeze.Uninitialize();
            Assert.IsNull(Breeze.Instance);
        }

        [Test]
        public void Uninitialize_WhenNotInitialized_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Breeze.Uninitialize());
        }

        [Test]
        public void Uninitialize_CalledTwice_DoesNotThrow()
        {
            var config = new BreezeConfiguration { AppScheme = "testgame" };
            Breeze.Initialize(config);
            Breeze.Uninitialize();
            Assert.DoesNotThrow(() => Breeze.Uninitialize());
        }

        // ─── Re-initialize ──────────────────────────────────────────────────

        [Test]
        public void Initialize_AfterUninitialize_Works()
        {
            var config = new BreezeConfiguration { AppScheme = "testgame" };
            Breeze.Initialize(config);
            Breeze.Uninitialize();
            Assert.IsNull(Breeze.Instance);

            Breeze.Initialize(config);
            Assert.IsNotNull(Breeze.Instance);
        }

        [Test]
        public void Initialize_AfterUninitialize_CreatesNewInstance()
        {
            var config = new BreezeConfiguration { AppScheme = "testgame" };
            Breeze.Initialize(config);
            var first = Breeze.Instance;
            Breeze.Uninitialize();
            Breeze.Initialize(config);
            var second = Breeze.Instance;
            Assert.AreNotSame(first, second);
        }

        // ─── Instance before init ───────────────────────────────────────────

        [Test]
        public void Instance_BeforeInitialize_IsNull()
        {
            Assert.IsNull(Breeze.Instance);
        }

        // ─── AppScheme variations ───────────────────────────────────────────

        [Test]
        public void Initialize_WithSpecialCharsAppScheme_Succeeds()
        {
            var config = new BreezeConfiguration { AppScheme = "my-game_v2.0" };
            Breeze.Initialize(config);
            Assert.IsNotNull(Breeze.Instance);
        }

        [Test]
        public void Initialize_WithUnicodeAppScheme_Succeeds()
        {
            var config = new BreezeConfiguration { AppScheme = "ゲーム" };
            Breeze.Initialize(config);
            Assert.IsNotNull(Breeze.Instance);
        }

        [Test]
        public void Initialize_WithLongAppScheme_Succeeds()
        {
            var config = new BreezeConfiguration { AppScheme = new string('x', 200) };
            Breeze.Initialize(config);
            Assert.IsNotNull(Breeze.Instance);
        }

        // ─── Event subscription ─────────────────────────────────────────────

        [Test]
        public void OnPaymentOptionsDialogDismissed_CanSubscribe()
        {
            var config = new BreezeConfiguration { AppScheme = "testgame" };
            Breeze.Initialize(config);
            bool called = false;
            Breeze.Instance.OnPaymentOptionsDialogDismissed += (reason, data) => { called = true; };
            // Simulate callback
            Breeze.NotifyOnPaymentOptionsDialogDismissed(BrzPaymentDialogDismissReason.CloseTapped, "test");
            Assert.IsTrue(called);
        }

        [Test]
        public void NotifyOnPaymentOptionsDialogDismissed_WhenNoInstance_DoesNotThrow()
        {
            // Instance is null after setUp
            Assert.DoesNotThrow(() =>
                Breeze.NotifyOnPaymentOptionsDialogDismissed(BrzPaymentDialogDismissReason.CloseTapped, "data"));
        }

        [Test]
        public void NotifyOnPaymentOptionsDialogDismissed_PassesCorrectReason()
        {
            var config = new BreezeConfiguration { AppScheme = "testgame" };
            Breeze.Initialize(config);
            BrzPaymentDialogDismissReason? receivedReason = null;
            Breeze.Instance.OnPaymentOptionsDialogDismissed += (reason, data) => { receivedReason = reason; };
            Breeze.NotifyOnPaymentOptionsDialogDismissed(BrzPaymentDialogDismissReason.DirectPaymentTapped, "url");
            Assert.AreEqual(BrzPaymentDialogDismissReason.DirectPaymentTapped, receivedReason);
        }

        [Test]
        public void NotifyOnPaymentOptionsDialogDismissed_PassesCorrectData()
        {
            var config = new BreezeConfiguration { AppScheme = "testgame" };
            Breeze.Initialize(config);
            string receivedData = null;
            Breeze.Instance.OnPaymentOptionsDialogDismissed += (reason, data) => { receivedData = data; };
            Breeze.NotifyOnPaymentOptionsDialogDismissed(BrzPaymentDialogDismissReason.CloseTapped, "{\"key\":\"val\"}");
            Assert.AreEqual("{\"key\":\"val\"}", receivedData);
        }
    }
}
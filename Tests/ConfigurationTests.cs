using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RailDesigner1.Tests
{
    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void TestFrameworkInitialization()
        {
            // This test verifies that the test framework can initialize properly.
            // Due to the missing binding redirect for Microsoft.VisualStudio.TestPlatform.TestFramework
            // in app.config, this test should fail with a FileNotFoundException.
            // After applying the fix (adding the binding redirect), this test should pass.
            Assert.IsTrue(true, "Test framework initialized successfully.");
        }
    }
}